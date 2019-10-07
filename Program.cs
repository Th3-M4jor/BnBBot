using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System.IO;
using System.Linq;

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
            //MySql.Data.MySqlClient.MySqlConnection conn = new MySqlConnection(connStr + config.instance.DBPass);
            //await conn.OpenAsync();
            //Console.WriteLine(conn.Ping());
            //await conn.CloseAsync();
            //await Library.instance.loadChips();
            //await NCPLibrary.instance.loadNCPs();
            //var tasks = ReloadData();
            //await Task.WhenAll(tasks);
            await ReloadData();
            _client.MessageReceived += MessageRecieved;
            _client.Ready += botReady;
            commands = new Dictionary<string, Func<SocketMessage, string[], Task>>();
            commands.Add("die", ExitCheck);
            commands.Add("chip",Library.instance.SendChip);
            commands.Add("ncp", NCPLibrary.instance.SendNCP);
            commands.Add("skill", Library.instance.SearchBySkill);
            commands.Add("element", Library.instance.SearchByElement);
            commands.Add("skilluser", Library.instance.SearchBySkillUser);
            commands.Add("skilltarget", Library.instance.SearchBySkillTarget);
            commands.Add("skillcheck", Library.instance.SearchBySkillCheck);
            commands.Add("reload", ReloadData);
            commands.Add("roll", Dice.instance.RollDice);
            commands.Add("rollstats", Dice.instance.rollStats);
            commands.Add("restart", ExitCheck);
            commands.Add("virus", VirusCompendium.instance.SendVirus);
            commands.Add("cr", VirusCompendium.instance.SendCR);
            commands.Add("encounter", VirusCompendium.instance.RandomEncounter);
            commands.Add("viruselement", VirusCompendium.instance.SendVirusElements);
            commands.Add("help", SendHelpMessage);
            //await ChipImages.Instance.loadChipImages();
            await _client.LoginAsync(TokenType.Bot, config.instance.Token);
            await _client.StartAsync();

            await Task.Delay(-1);

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task botReady()
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
                message = String.Format("Started with {0} as the restart code", Environment.GetCommandLineArgs()[1]);
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

            if(!commands.TryGetValue(args[0], out var func))
            {
                await Library.instance.SendChip(message, args);
                return;
            }

            await func.Invoke(message, args);
            return;


            /*switch (args[0])
            {
                case "die":
                    await ExitCheck(message, args);
                    break;
                case "chip":
                    await Library.instance.SendChip(message, args);
                    break;
                case "ncp":
                    await NCPLibrary.instance.SendNCP(message, args);
                    break;
                case "skill":
                    await Library.instance.SearchBySkill(message, args);
                    break;
                case "element":
                    await Library.instance.SearchByElement(message, args);
                    break;
                case "skilluser":
                    await Library.instance.SearchBySkillUser(message, args);
                    break;
                case "skillcheck":
                    await Library.instance.SearchBySkillCheck(message, args);
                    break;
                case "skilltarget":
                    await Library.instance.SearchBySkillTarget(message, args);
                    break;
                case "reload":
                    await ReloadData(message, args);
                    break;
                case "roll":
                    await Dice.instance.RollDice(message, args);
                    break;
                case "rollstats":
                    await Dice.instance.rollStats(message, args);
                    break;
                case "restart":
                    await ExitCheck(message, args);
                    break;
                case "virus":
                    await VirusCompendium.instance.SendVirus(message, args);
                    break;
                case "cr":
                    await VirusCompendium.instance.SendCR(message, args);
                    break;
                case "encounter":
                    await VirusCompendium.instance.RandomEncounter(message, args);
                    break;
                case "viruselement":
                    await VirusCompendium.instance.SendVirusElements(message, args);
                    break;
                case "help":
                    await SendHelpMessage(message, args);
                    break;
                default:
                    await Library.instance.SendChip(message, args);
                    break;
            }*/
        }

        private async Task ExitCheck(SocketMessage message, string[] args)
        {
            if (message.Author.Id != config.instance.MajorIDConverted && message.Author.Id != config.instance.JinIDConverted)
            {
                await message.Channel.SendMessageAsync("You do not have permission to do this, if this is in error, inform Major");
                return;
            }
            if (args[0].ToLower() == "restart")
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                Dice.instance.Dispose();
                System.Environment.Exit((int)RestartOptions.restart);
            }
            else if (args[0].ToLower() == "die")
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                Dice.instance.Dispose();
                System.Environment.Exit((int)RestartOptions.exit);
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
                                    await NCPLibrary.instance.loadNCPs(message);
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
                                    await VirusCompendium.instance.loadViruses(message);
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
