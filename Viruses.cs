using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Discord.WebSocket;

namespace csharp
{
    public class Virus
    {
        public string Name { get; private set; }

        public string Element { get; private set; }

        public byte CR { get; private set; }

        public string Description { get; private set; }

        public Virus(string name, string element, string description, byte CR)
        {
            this.Name = name ?? throw new ArgumentNullException();
            this.Element = element ?? throw new ArgumentNullException();
            this.Description = description ?? throw new ArgumentNullException();
            this.CR = CR;
        }

    }
    public class VirusCompendium
    {
        public static readonly VirusCompendium instance = new VirusCompendium();

        public const string virusURL = "https://docs.google.com/feeds/download/documents/export/Export?id=1PZKYP0mzzxMTmjJ8CfrUMapgQPHgi24Ev6VB3XLBUrU&exportFormat=txt";

        public const string virusRegex = @"((.+)\s\((\w+)\))";
        public const string crRegex = @"CR\s+(\d+)";

        private Regex virusCheck;
        private Regex crCheck;

        private ConcurrentDictionary<string, Virus> compendium;

        private VirusCompendium()
        {
            virusCheck = new Regex(virusRegex, RegexOptions.ECMAScript);
            crCheck = new Regex(crRegex, RegexOptions.ECMAScript);
        }

        public async Task loadViruses(SocketMessage message = null)
        {
            var newCompendium = new ConcurrentDictionary<string, Virus>();
            List<string> duplicates = new List<string>();
            string document = (await Library.client.GetStringAsync(virusURL)).Replace("â€™", "'");
            document = Regex.Replace(document, "[\r]", string.Empty);
            var VirusList = document.Split("\n").Where(a => a.Trim() != string.Empty).ToArray();
            byte currentCR = 0;
            string currentVirusName = string.Empty;
            string currentVirusElement = string.Empty;
            string currentVirusFullName = string.Empty;
            StringBuilder virusDescription = new StringBuilder();
            bool foundDuplicate = false;
            for (long i = 0; i < VirusList.LongLength; i++)
            {
                var crRes = crCheck.Match(VirusList[i]);
                var virusNameRes = virusCheck.Match(VirusList[i]);
                if (crRes.Success || virusNameRes.Success)
                {
                    if (virusDescription.Length != 0)
                    {
                        if (duplicates.Contains(currentVirusName.ToLower())) //name was in the duplicates
                        {
                            bool addRes = newCompendium.TryAdd(currentVirusFullName.ToLower(), new Virus(
                                currentVirusFullName, currentVirusElement,
                                virusDescription.ToString(), currentCR
                            ));
                            if (addRes == false)
                            {
                                foundDuplicate = true;
                            }
                        }
                        else
                        {
                            bool addRes = newCompendium.TryAdd(currentVirusName.ToLower(), new Virus(
                                    currentVirusName, currentVirusElement,
                                    virusDescription.ToString(), currentCR
                            ));
                            if (addRes == false) //found a duplicate name, fix by adding element to name
                            {
                                newCompendium.Remove(currentVirusName.ToLower(), out Virus Val);
                                string newName = Val.Name + " (" + Val.Element + ")";
                                addRes = newCompendium.TryAdd(newName.ToLower(), new Virus(
                                    newName, Val.Element, Val.Description, Val.CR
                                ));

                                //single "&" here so second one gets added too
                                addRes = addRes & newCompendium.TryAdd(currentVirusFullName.ToLower(), new Virus(
                                currentVirusFullName, currentVirusElement,
                                virusDescription.ToString(), currentCR
                                ));
                                if (addRes == false)
                                {
                                    foundDuplicate = true;
                                }
                                duplicates.Add(currentVirusName.ToLower());
                            }
                        }
                        virusDescription.Clear();
                    }
                    if (crRes.Success)
                        currentCR = byte.Parse(crRes.Groups[1].ToString());
                    else if (virusNameRes.Success)
                    {
                        currentVirusFullName = virusNameRes.Groups[1].ToString();
                        currentVirusName = virusNameRes.Groups[2].ToString();
                        currentVirusElement = virusNameRes.Groups[3].ToString();
                    }
                    continue;
                }

                //else is part of a virus description
                virusDescription.AppendLine(VirusList[i]);
            }
            string wasDuplicates = foundDuplicate ? "Yes" : "No";
            string reply = string.Format("{0} Viruses loaded, were duplicates found: {1}", newCompendium.Count, wasDuplicates);
            this.compendium = newCompendium;
            if (message != null)
            {
                await message.Channel.SendMessageAsync(reply);
            }
            else System.Console.WriteLine(reply);
#if !DEBUG
            var toConvert = (from kvp in this.compendium select kvp.Value).OrderBy(virus => virus.CR).ThenBy(virus => virus.Name);
            string toWrite = JsonConvert.SerializeObject(toConvert, Formatting.Indented);
            var writeSream = System.IO.File.CreateText("./virusCompendium.json");
            await writeSream.WriteAsync(toWrite);
            await writeSream.FlushAsync();
            writeSream.Dispose();
#endif
            Console.WriteLine(string.Join(", ", duplicates));
        }

        public async Task sendVirus(SocketMessage message, string name)
        {
            bool exists = this.compendium.TryGetValue(name.ToLower(), out Virus foundVirus);
            if (exists)
            {
                string toSend = string.Format("```{0} - CR {1}\n{2}```", foundVirus.Name, foundVirus.CR, foundVirus.Description);
                await message.Channel.SendMessageAsync(toSend);
                return;
            }
            var virusList = (from kvp in compendium.AsParallel().
            WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                             where kvp.Key.Contains(name.ToLower())
                             select kvp.Value.Name).OrderBy(Virus => Virus).ToArray();
            switch (virusList.Length)
            {
                case 0:
                    {
                        await message.Channel.SendMessageAsync("No viruses with that name");
                        return;
                    }
                case 1:
                    {
                        this.compendium.TryGetValue(virusList[1].ToLower(), out foundVirus);
                        string toSend = string.Format("```{0} - CR {1}\n{2}```", foundVirus.Name, foundVirus.CR, foundVirus.Description);
                        await message.Channel.SendMessageAsync(toSend);
                        return;
                    }
                default:
                    await Library.sendStringArrayAsMessage(message, virusList);
                    return;
            }

        }

        public async Task sendCR(SocketMessage message, string name)
        {
            if (!int.TryParse(name, out int CRNum))
            {
                await message.Channel.SendMessageAsync("That is not a valid number");
                return;
            }
            var virusList = (from kvp in compendium.AsParallel().
            WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                             where kvp.Value.CR == CRNum
                             select kvp.Value.Name).OrderBy(Virus => Virus).ToArray();
            if (virusList.Length == 0)
            {
                await message.Channel.SendMessageAsync("There are no viruses in that CR");
            }
            else
            {
                await Library.sendStringArrayAsMessage(message, virusList);
            }
        }

        public async Task randomEncounter(SocketMessage message, string[] args)
        {
            if (args.Length < 3)
            {
                await message.Channel.SendMessageAsync("You must send both a CR and number of viruses");
                await message.Channel.SendMessageAsync("EX:\n```%encounter 2-3 5```\n"
                                            + "This will give 5 random viruses of CR 2 or 3");
                return;
            }

            if (!uint.TryParse(args[2], out uint numViruses) || numViruses == 0)
            {
                await message.Channel.SendMessageAsync("That is not a positive number of viruses");
                return;
            }

            bool isSingleCR = uint.TryParse(args[1], out uint crNum);
            if (isSingleCR)
            {
                await sendSingleCR(message, numViruses, crNum);
            }
            else
            {
                var res = args[1].Trim().Split('-');
                if (res.Length != 2)
                {
                    await message.Channel.SendMessageAsync("That is an invalid CR range");
                    return;
                }
                if (!uint.TryParse(res[0], out uint lowCRNum) || !(uint.TryParse(res[1], out uint highCRNum)))
                {
                    await message.Channel.SendMessageAsync("That is an invalid CR range");
                    return;
                }
                if (lowCRNum > highCRNum)
                {
                    uint temp = lowCRNum;
                    lowCRNum = highCRNum;
                    highCRNum = lowCRNum;
                }
                await sendCRRange(message, numViruses, lowCRNum, highCRNum);
            }
        }

        private async Task sendSingleCR(SocketMessage message, uint numViruses, uint CR)
        {
            var toSelect = (from kvp in compendium.AsParallel()
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.CR == CR
                            select kvp.Value.Name).ToArray();
            if (toSelect.Length == 0)
            {
                await message.Channel.SendMessageAsync("There weren't any viruses in that CR");
                return;
            }
            string[] chosenViruses = new string[numViruses];
            for (uint i = 0; i < chosenViruses.Length; i++)
            {
                uint randVal = Dice.instance.getRandomNum() % (uint)toSelect.Length;
                chosenViruses[i] = toSelect[randVal];
            }
            await Library.sendStringArrayAsMessage(message, chosenViruses);
        }

        private async Task sendCRRange(SocketMessage message, uint numViruses, uint lowCR, uint highCR)
        {
            var toSelect = (from kvp in compendium.AsParallel()
               .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.CR >= lowCR && kvp.Value.CR <= highCR
                            select kvp.Value.Name).ToArray();
            if (toSelect.Length == 0)
            {
                await message.Channel.SendMessageAsync("There weren't any viruses in that CR range");
                return;
            }
            string[] chosenViruses = new string[numViruses];
            for (uint i = 0; i < chosenViruses.Length; i++)
            {
                uint randVal = Dice.instance.getRandomNum() % (uint)toSelect.Length;
                chosenViruses[i] = toSelect[randVal];
            }
            await Library.sendStringArrayAsMessage(message, chosenViruses);
        }

        public async Task sendVirusElements(SocketMessage message, string arg)
        {
            arg = arg.ToLower();
            var toSend = (from kvp in compendium.AsParallel()
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.Element.ToLower() == arg
                            select kvp.Value.Name).OrderBy(Virus => Virus).ToArray();
            if(toSend.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
                return;
            }
            await Library.sendStringArrayAsMessage(message, toSend);
        }

    }

}