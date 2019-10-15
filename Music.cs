using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord.Audio;
using Discord.WebSocket;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace csharp
{
    class botMusic : IDisposable
    {
        private static Lazy<botMusic> lazy = new Lazy<botMusic>(() => new botMusic());

        private ConcurrentDictionary<ulong, IAudioClient> voiceConnections;

        private ConcurrentDictionary<ulong, ConcurrentQueue<string>> playQueuePerserver;
        private ConcurrentDictionary<ulong, SocketVoiceChannel> voiceChannels;

         private ConcurrentDictionary<ulong, Task> serverDispatchers;

        public static botMusic instance
        {
            get
            {
                return lazy.Value;
            }
        }
        private botMusic()
        {
            voiceConnections = new ConcurrentDictionary<ulong, IAudioClient>();
            voiceChannels = new ConcurrentDictionary<ulong, SocketVoiceChannel>();
            playQueuePerserver = new ConcurrentDictionary<ulong, ConcurrentQueue<string>>();
            serverDispatchers = new ConcurrentDictionary<ulong, Task>();
            
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "/" + "Music"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + "Music");
            }

        }

        public async Task LeaveAll()
        {

            foreach (var conn in voiceConnections)
            {
                await conn.Value.StopAsync();
                conn.Value.Dispose();
                voiceConnections.Remove(conn.Key, out _);
            }

            foreach (var channel in voiceChannels)
            {
                await channel.Value.DisconnectAsync();
                voiceChannels.Remove(channel.Key, out _);
            }
        }

        public async Task JoinVoiceChannel(SocketMessage message, string[] args)
        {

            if (!(message.Author is SocketGuildUser messageAuthor))
            {
                await message.Channel.SendMessageAsync("This command only works in a server");
                return;
            }
            if (messageAuthor.VoiceChannel == null)
            {
                await message.Channel.SendMessageAsync("You must be in a voice channel first");
                return;
            }
            try
            {
                var conn = await messageAuthor.VoiceChannel.ConnectAsync();
                if (voiceConnections.ContainsKey(messageAuthor.Guild.Id))
                {
                    voiceConnections.Remove(messageAuthor.Guild.Id, out _);
                    voiceChannels.Remove(messageAuthor.Guild.Id, out _);
                }
                voiceConnections.TryAdd(messageAuthor.Guild.Id, conn);
                voiceChannels.TryAdd(messageAuthor.Guild.Id, messageAuthor.VoiceChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public async Task LeaveVoiceChannel(SocketMessage message, string[] args)
        {

            if (!(message.Author is SocketGuildUser messageAuthor))
            {
                await message.Channel.SendMessageAsync("This command only works in a server");
                return;
            }

            if (!voiceConnections.TryGetValue(messageAuthor.Guild.Id, out var conn))
            {
                await message.Channel.SendMessageAsync("I'm already not in a voice channel");
                return;
            }

            voiceChannels.Remove(messageAuthor.Guild.Id, out var channel);
            await channel.DisconnectAsync();
            voiceConnections.Remove(messageAuthor.Guild.Id, out _);
            conn.Dispose();
        }


        public async Task PlayMusic(SocketMessage message, string[] args)
        {

            if (!(message.Author is SocketGuildUser messageAuthor))
            {
                await message.Channel.SendMessageAsync("This command only works in a server");
                return;
            }

            if (!voiceConnections.TryGetValue(messageAuthor.Guild.Id, out var conn))
            {
                //await message.Channel.SendMessageAsync("I'm not in a voice channel, joining...");
                await this.JoinVoiceChannel(message, args);
                if (!voiceConnections.ContainsKey(messageAuthor.Guild.Id))
                {
                    return;
                }
            }

            if (!Uri.TryCreate(args[1], UriKind.Absolute, out _))
            {
                await message.Channel.SendMessageAsync("That is not a valid youtube URL");
                return;
            }
            bool startDispatcher = playQueuePerserver.ContainsKey(messageAuthor.Guild.Id);
            await message.Channel.SendMessageAsync("adding to queue...");
            var playQueue = playQueuePerserver.GetOrAdd(messageAuthor.Guild.Id, new ConcurrentQueue<string>());
            playQueue.Enqueue(args[1]);
            if(serverDispatchers.TryGetValue(messageAuthor.Guild.Id, out var dispatcher))
            {
                if(dispatcher.IsCompleted) serverDispatchers.Remove(messageAuthor.Guild.Id, out _);
                else return;
            }
            var toAddDispatcher = BeginDispatcher(messageAuthor.Guild.Id);
            serverDispatchers.TryAdd(messageAuthor.Guild.Id, toAddDispatcher);

        }

        private Task BeginDispatcher(ulong guildID)
        {
            return Task.Run(async () =>
            {
                if (!voiceConnections.TryGetValue(guildID, out var conn))
                {
                    await Program.Log(new Discord.LogMessage(Discord.LogSeverity.Error, "music", "no voice connection"));
                    return;
                }
                playQueuePerserver.TryGetValue(guildID, out var queue);
                Process ffmpeg;
                Stream output;
                AudioOutStream discord;
                Console.WriteLine(queue.Count);
                while (!queue.IsEmpty)
                {
                    queue.TryDequeue(out var toDownload);
                    Console.WriteLine("Downloading " + toDownload);
                    DownloadVideo(toDownload, guildID.ToString()).WaitForExit();

                    using (ffmpeg = CreateStream(Directory.GetCurrentDirectory() + "/" + "Music/" + guildID.ToString() + ".m4a"))
                    using (output = ffmpeg.StandardOutput.BaseStream)
                    using (discord = conn.CreatePCMStream(AudioApplication.Mixed))
                    {
                        
                        try { await output.CopyToAsync(discord); }
                        catch (Exception e)
                        {
#if !DEBUG
                    await message.Channel.SendMessageAsync(e.Message);
#else
                            await Program.Log(new Discord.LogMessage(Discord.LogSeverity.Info, "music", e.Message, e));
#endif
                        }
                        finally { await discord.FlushAsync(); }
                    }
                }
            });
        }

        private Process DownloadVideo(string url, string fname)
        {

            fname = Directory.GetCurrentDirectory() + "/" + "Music/" + fname;
            if(File.Exists(fname))
            {
                File.Delete(fname);
            }
            return Process.Start(new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"{url} -x -o {fname}.m4a",
                UseShellExecute = true,
                RedirectStandardOutput = false,
            });
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                }
                LeaveAll().Wait();
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~botMusic()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}