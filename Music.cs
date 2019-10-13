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
    class botMusic
    {
        private static Lazy<botMusic> lazy = new Lazy<botMusic>(() => new botMusic());

        private ConcurrentDictionary<ulong, IAudioClient> voiceConnections;
        private ConcurrentDictionary<ulong, SocketVoiceChannel> voiceChannels;

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
                await message.Channel.SendMessageAsync("I'm not in a voice channel");
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
                await message.Channel.SendMessageAsync("I'm not in a voice channel");
                return;
            }

            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream("/home/spartan364/LockAndLoad.mp3"))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = conn.CreatePCMStream(AudioApplication.Mixed))
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

    }
}