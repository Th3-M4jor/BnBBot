using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

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

        private static bool sentStartupMessage = false;

        public string helpText { get; private set; }

        public static void Main(string[] args)
        => new Program().MainAsync(args).GetAwaiter().GetResult();
        public async Task MainAsync(string[] args)
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            //await Library.instance.loadChips();
            //await NCPLibrary.instance.loadNCPs();
            var tasks = reloadData();
            await Task.WhenAll(tasks);
            _client.MessageReceived += MessageRecieved;
            _client.Ready += botReady;
            
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
            this.helpText = await File.ReadAllTextAsync("./help.txt");
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
            switch (args[0])
            {
                case "die":
                    await exitCheck(message, RestartOptions.exit);
                    break;
                case "chip":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.sendChip(message, args[1]);
                    break;
                case "ncp":
                    {
                        if (args.Length < 2)
                        {
                            await message.Channel.SendMessageAsync("You must specify an argument");
                            break;
                        }
                        args = args.Skip(1).Take(args.Length - 1).ToArray();
                        string toGet = string.Join(" ", args);
                        await NCPLibrary.instance.sendNCP(message, toGet);
                        break;
                    }
                case "skill":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.searchBySkill(message, args[1]);
                    break;
                case "element":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.searchByElement(message, args[1]);
                    break;
                case "skilluser":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.searchBySkillUser(message, args[1]);
                    break;
                case "skillcheck":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.searchBySkillCheck(message, args[1]);
                    break;
                case "skilltarget":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify an argument");
                        break;
                    }
                    await Library.instance.searchBySkillTarget(message, args[1]);
                    break;
                case "reload":
                    if (message.Author.Id == config.instance.MajorIDConverted ||
                            message.Author.Id == config.instance.JinIDConverted)
                    {
                        reloadData(message);
                    }
                    break;

                case "roll":
                    if (args.Length < 2)
                    {
                        await message.Channel.SendMessageAsync("You must specify a number of dice to roll");
                        break;
                    }
                    await Dice.instance.rollDice(message, args);
                    break;
                case "rollstats":
                    await Dice.instance.rollStats(message);
                    break;
                case "restart":
                    await exitCheck(message, RestartOptions.restart);
                    break;
                case "virus":
                    {
                        if (args.Length < 2)
                        {
                            await message.Channel.SendMessageAsync("You must specify a virus name");
                            break;
                        }
                        args = args.Skip(1).Take(args.Length - 1).ToArray();
                        string toGet = string.Join(" ", args);
                        await VirusCompendium.instance.sendVirus(message, toGet);
                        break;
                    }
                case "cr":
                    {
                        if (args.Length < 2)
                        {
                            await message.Channel.SendMessageAsync("You must specify a virus CR");
                            break;
                        }
                        await VirusCompendium.instance.sendCR(message, args[1]);
                        break;
                    }
                case "encounter":
                    {
                        await VirusCompendium.instance.randomEncounter(message, args);
                        break;
                    }
                case "viruselement":
                    {
                        if (args.Length < 2)
                        {
                            await message.Channel.SendMessageAsync("You must specify an argument");
                            break;
                        }
                        await VirusCompendium.instance.sendVirusElements(message, args[1]);
                        break;
                    }
                case "help":
                    await message.Author.SendMessageAsync(this.helpText);
                    return;
                default:
                    await Library.instance.sendChip(message, args[0]);
                    return;
            }
        }

        private async Task exitCheck(SocketMessage message, RestartOptions option)
        {
            if (message.Author.Id == config.instance.MajorIDConverted || message.Author.Id == config.instance.JinIDConverted)
            {
                await _client.SetStatusAsync(UserStatus.Invisible);
                Dice.instance.Dispose();
                System.Environment.Exit((int)option);
            }
            else
            {
                await message.Channel.SendMessageAsync("You do not have permission to do this, if this is in error, inform Major");
            }
        }

        private Task[] reloadData(SocketMessage message = null)
        {
            Task[] toReturn = new Task[3];
            toReturn[0] = Task.Run(async () =>
                            {
                                try
                                {
                                    await Library.instance.loadChips(message);
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

            return toReturn;
        }

    }
}
