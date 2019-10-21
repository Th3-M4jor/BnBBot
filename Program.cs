using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Discord;
using Discord.WebSocket;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace csharp
{
    enum RestartOptions
    {
        exit = 88,
        restart = 42
    }

    public class Program
    {
        private static DiscordSocketClient _client;

        public static MySqlConnection conn;
        private const string connStr = "server=spartan364.hopto.org;user=Cougartalk;database=BnBData;port=3306;password=";

        private static bool sentStartupMessage = false;

        private Dictionary<string, Func<SocketMessage, string[], Task>> commands;

        public string HelpText { get; private set; }

        public static void Main(string[] args)
        => new Program().MainAsync(args).GetAwaiter().GetResult();
        public async Task MainAsync(string[] args)
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            //conn = new MySqlConnection(connStr + config.instance.DBPass);
            //await conn.OpenAsync();
            //Console.WriteLine(conn.Ping());
            //await conn.CloseAsync();
            //await Library.instance.loadChips();
            //await NCPLibrary.instance.loadNCPs();
            //var tasks = ReloadData();
            //await Task.WhenAll(tasks);
            await ReloadData();
            _client.MessageReceived += MessageRecieved;
            _client.Ready += BotReady;
            commands = new Dictionary<string, Func<SocketMessage, string[], Task>>
            {
                { "die", ExitCheck },
                { "chip", Library.instance.SendChip },
                { "ncp", NCPLibrary.instance.SendNCP },
                { "skill", Library.instance.SearchBySkill },
                { "element", Library.instance.SearchByElement },
                { "skilluser", Library.instance.SearchBySkillUser },
                { "skilltarget", Library.instance.SearchBySkillTarget },
                { "skillcheck", Library.instance.SearchBySkillCheck },
                { "reload", ReloadData },
                { "roll", Dice.instance.RollDice },
                { "rollstats", Dice.instance.RollStats },
                { "restart", ExitCheck },
                { "virus", VirusCompendium.instance.SendVirus },
                { "cr", VirusCompendium.instance.SendCR },
                { "encounter", VirusCompendium.instance.RandomEncounter },
                { "viruselement", VirusCompendium.instance.SendVirusElements },
                { "help", SendHelpMessage }
                //{ "join", BotMusic.Instance.JoinVoiceChannel },
                //{ "leave", BotMusic.Instance.LeaveVoiceChannel },
                //{ "play", BotMusic.Instance.PlayMusic }
            };
            //await ChipImages.Instance.loadChipImages();
            await _client.LoginAsync(TokenType.Bot, config.instance.Token);
            await _client.StartAsync();

            await Task.Delay(-1);

        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task BotReady()
        {
            await _client.SetGameAsync("%help for a list of commands");
            if (sentStartupMessage)
            {
                return;
            }
            this.HelpText = await File.ReadAllTextAsync("./help.txt");
            var major = _client.GetUser(config.instance.MajorIDConverted);
            if (major == null)
            {
                Console.WriteLine("could not fetch Major");
                return;
            }
            string message;
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                message = "Started with no commandLine args";
            }
            else
            {
                message = string.Format("Started with {0} as the restart code", Environment.GetCommandLineArgs()[1]);
            }
            await major.SendMessageAsync(message);
            sentStartupMessage = true;
        }

        private async Task MessageRecieved(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            if (!message.Content.Trim().StartsWith(config.instance.Prefix))
            {
                return;
            }

            var args = message.Content.Split(" ");
            args[0] = args[0].Substring(config.instance.Prefix.Length).ToLower(); //remove starting command prefix
            //Console.WriteLine(args[0]);
            if (args[0] == string.Empty)
            {
                return;
            }

            if (!commands.TryGetValue(args[0], out var func))
            {
                await Library.instance.SendChip(message, args);
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await func.Invoke(message, args);
                }
                catch (Exception e)
                {
#if DEBUG
                    await message.Channel.SendMessageAsync(e.Message);
                    Console.WriteLine(e.Message);
#else               
                    await Log(new Discord.LogMessage(LogSeverity.Error, "command", e.Message, e));
#endif
                }
            });
        }

        private async Task ExitCheck(SocketMessage message, string[] args)
        {
            if (message.Author.Id != config.instance.MajorIDConverted && message.Author.Id != config.instance.JinIDConverted)
            {
                await message.Channel.SendMessageAsync("You do not have permission to do this, if this is in error, inform Major");
                return;
            }

            BotMusic.Instance.Dispose();
            Dice.instance.Dispose();
            conn?.Dispose();


            if (args[0].ToLower() == "restart")
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                Environment.Exit((int)RestartOptions.restart);
            }
            else
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                Environment.Exit((int)RestartOptions.exit);
            }
        }

        private async Task SendHelpMessage(SocketMessage message, string[] args = null) =>
            await message.Author.SendMessageAsync(this.HelpText);

        private Task ReloadData(SocketMessage message = null, string[] args = null)
        {
            if (message != null && message.Author.Id != config.instance.JinIDConverted
                && message.Author.Id != config.instance.MajorIDConverted)
            {
                message.Author.SendMessageAsync("You do not have permission to do this, if you think this is in error, inform Major.");
                return null;
            }
            Task[] toReturn = new Task[3];
            toReturn[0] = Task.Run(async () =>
                            {
                                try
                                {
                                    await Library.instance.LoadChips(message);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            });
            toReturn[1] = Task.Run(async () =>
                            {
                                try
                                {
                                    await NCPLibrary.instance.LoadNCPs(message);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            });
            toReturn[2] = Task.Run(async () =>
                            {
                                try
                                {
                                    await VirusCompendium.instance.LoadViruses(message);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            });

            return Task.WhenAll(toReturn);
        }

    }
}
