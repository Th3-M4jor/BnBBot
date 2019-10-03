using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Discord.WebSocket;
using Discord;

namespace csharp
{
    public class NCP
    {
        public string Name { get; private set; }
        public byte EBCost { get; private set; }

        public string Color { get; private set; }
        public string All { get; private set; }

        public string Description { get; private set; }

        public NCP(string name, string color, string all, string description, byte ebCost)
        {
            this.Name = name ?? throw new ArgumentNullException();
            this.Color = color ?? throw new ArgumentNullException();
            this.All = all ?? throw new ArgumentNullException();
            this.Description = description ?? throw new ArgumentNullException();
            this.EBCost = ebCost;
        }

    }
    public class NCPLibrary
    {
        public const string NCPUrl = "https://docs.google.com/feeds/download/documents/export/Export?id=1cPLJ2tAUebIVZU4k7SVnyABpR9jQd7jarzix7oVys9M&exportFormat=txt";

        private const string NCPRegex = @"(.+)\s\((\d+)\sEB\)\s\-\s(.+)";
        private string[] NCPColors = { "White", "Pink", "Yellow", "Green", "Blue", "Red", "Gray" };
        public static readonly NCPLibrary instance = new NCPLibrary();
        private ConcurrentDictionary<string, NCP> NCPs;

        private Regex NCPTest;

        private NCPLibrary()
        {
            NCPs = new ConcurrentDictionary<string, NCP>();
            NCPTest = new Regex(NCPRegex, RegexOptions.ECMAScript);
        }

        public async Task loadNCPs(Discord.WebSocket.SocketMessage message = null)
        {
            ConcurrentDictionary<string, NCP> newNCPLibrary = new ConcurrentDictionary<string, NCP>();
            string document = (await Library.client.GetStringAsync(NCPUrl)).Replace("â€™", "'");
            document = Regex.Replace(document, "[\r]", string.Empty);
            var NCPList = document.Split("\n").Where(a => a.Trim() != string.Empty).ToArray();
            string currentColor = null;
            string newColor = null;
            foreach (var cust in NCPList)
            {
                newColor = NCPColors.FirstOrDefault(stringToCheck => stringToCheck.Equals(cust.Trim(), StringComparison.OrdinalIgnoreCase));
                if (newColor != null)
                {
                    currentColor = newColor;
                    continue;
                }
                var res = NCPTest.Match(cust);
                if (!res.Success)
                {
                    continue;
                }
                if (!res.Groups[1].Success || !res.Groups[2].Success)
                {
                    continue;
                }
                newNCPLibrary.TryAdd(res.Groups[1].ToString().ToLower().Trim(),
                                     new NCP(res.Groups[1].ToString().Trim(),
                                             currentColor,
                                             cust,
                                             res.Groups[3].ToString().Trim(),
                                             byte.Parse(res.Groups[2].ToString()
                                                        )
                                             )
                                    );
            }
            this.NCPs = newNCPLibrary;
            string reply = string.Format("{0} programs loaded", newNCPLibrary.Count);
            if (message != null)
            {
                await message.Channel.SendMessageAsync(reply);
            }
            else System.Console.WriteLine(reply);
#if !DEBUG
            var toConvert = (from kvp in this.NCPs select kvp.Value).OrderBy(aNCP => aNCP.Color).ThenBy(aNCP => aNCP.Name);
            string toWrite = JsonConvert.SerializeObject(toConvert, Formatting.Indented);
            var writeSream = System.IO.File.CreateText("./naviCust.json");
            await writeSream.WriteAsync(toWrite);
            await writeSream.FlushAsync();
            writeSream.Dispose();
#endif
        }

        public async Task SendNCP(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify an argument");
                return;
            }

            args = args.Skip(1).Take(args.Length - 1).ToArray();
            string name = string.Join(" ", args);

            bool exists = this.NCPs.TryGetValue(name.ToLower(), out NCP Value);

            if (exists)
            {
                await message.Channel.SendMessageAsync("```" + Value.Name + " - (" + Value.EBCost + " EB) - " + Value.Color +
                                                        "\n" + Value.Description + "```");
                
                return;
            }

            var NCPList = (from kvp in NCPs.AsParallel().
                WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                           where kvp.Key.Contains(name.ToLower())
                           select kvp.Value.Name).OrderBy(NCP => NCP).ToArray();
            switch (NCPList.Length)
            {
                case 0:
                    {
                        //no ncp found
                        await message.Channel.SendMessageAsync("That doesn't exist");
                        return;
                    }
                case 1:
                    {
                        //one ncp has a name that contains it
                        this.NCPs.TryGetValue(NCPList[0].ToLower(), out NCP foundVal);
                        await message.Channel.SendMessageAsync("```" + foundVal.Name + " - (" + foundVal.EBCost + " EB) - " + foundVal.Color +
                                                        "\n" + foundVal.Description + "```");
                        
                        return;
                    }

                default:
                    {
                        await Library.SendStringArrayAsMessage(message, NCPList);
                        return;
                    }
            }
        }
    }
}
