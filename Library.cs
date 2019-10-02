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
using System.Drawing.Imaging;

namespace csharp
{
    public class Library
    {
        public const string ChipUrl = "https://docs.google.com/feeds/download/documents/export/Export?id=1lvAKkymOplIJj6jS-N5__9aLIDXI6bETIMz01MK9MfY&exportFormat=txt";
        public const string regexVal = @"(.+?)\s\-\s(.+?)\s\|\s(.+?)\s\|\s(.+?)\s\|\s(\d+d\d+|\-\-)\s?(?:damage)?\s?\|?\s?(Mega|Giga)?\s\|\s(\d+|\d+\-\d+|\-\-)\s?(?:hits?)\.?";

        public const string saveRegexVal = @"an?\s(\w+)\scheck\sof\s\[DC\s\d+\s\+\s(\w+)\]";
        public static readonly Library instance = new Library();
        public static readonly System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        private ConcurrentDictionary<string, chip> chipLibrary;

        private readonly Regex chipTest;

        private readonly Regex saveRegexTest;


        private Library()
        {
            chipLibrary = new ConcurrentDictionary<string, chip>();
            chipTest = new Regex(regexVal, RegexOptions.ECMAScript);
            saveRegexTest = new Regex(saveRegexVal, RegexOptions.ECMAScript);
        }

        public async Task LoadChips(Discord.WebSocket.SocketMessage message = null)
        {
            //chipLibrary.Clear();
            ConcurrentDictionary<string, chip> newChipLibrary = new ConcurrentDictionary<string, chip>();
            string chips = (await client.GetStringAsync(ChipUrl)).Replace("â€™", "'");
            chips = Regex.Replace(chips, "[\r]", string.Empty);
            var chipList = chips.Split("\n").ToList();
            if (File.Exists("./specialChips.txt"))
            {
                chipList.AddRange((await GetFileContents("./specialChips.txt")).Split("\n"));
            }
            chipList = chipList.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            .Where(a => a.Trim() != string.Empty).ToList();
            var badChips = new List<string>();
            for (int i = 0; i < chipList.Count; i += 2)
            {
                var res = chipTest.Match(chipList[i]);
                if (!res.Success)
                {
                    badChips.Add(chipList[i]);
                    continue;
                }
                string skillUser = null;
                string skillTarget = null;
                var skillRes = saveRegexTest.Match(chipList[i + 1]);
                if (skillRes.Success)
                {
                    skillTarget = skillRes.Groups[1].ToString(); //skill the target uses
                    skillUser = skillRes.Groups[2].ToString(); //skill the user uses
                }
                chip toAdd = new chip(
                    res.Groups[1].ToString(), //name
                    res.Groups[4].ToString(), //range
                    res.Groups[5].ToString(), //damage
                    res.Groups[7].ToString(), //hits
                    res.Groups[6].Success ?
                        res.Groups[6].ToString() : "Standard", //type
                    res.Groups[2]
                       .ToString()
                       .Split(", "), //element
                    res.Groups[3]
                       .ToString()
                       .Split(", "), //skill
                    chipList[i + 1], //description
                    chipList[i]
                        + "\n"
                        + chipList[i + 1], //all
                    skillUser,
                    skillTarget
                    );
                newChipLibrary.TryAdd(res.Groups[1].ToString().ToLower(), toAdd);
            }
            chipLibrary = newChipLibrary;
            string reply;

            if (badChips.Count == 0)
            {
                reply = string.Format("{0} chips loaded", chipLibrary.Count);
            }
            else if (badChips.Count == 1)
            {
                reply = string.Format("There was one bad chip\n```{0}```", badChips[0]);
            }
            else
            {
                reply = string.Format("There were {0} bad chips\n```{1}```", badChips.Count, badChips[0]);
            }

            if (message != null)
            {
                await message.Channel.SendMessageAsync(reply);
            }
            else System.Console.WriteLine(reply);
#if !DEBUG
            var toConvert = (from kvp in chipLibrary.AsParallel().
                WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                             select kvp.Value)
                .OrderBy(aChip => aChip.Name).ToList();
            string toWrite = JsonConvert.SerializeObject(toConvert, Formatting.Indented);
            var writeSream = System.IO.File.CreateText("./chips.json");
            await writeSream.WriteAsync(toWrite);
            await writeSream.FlushAsync();
            writeSream.Dispose();
#endif

        }

        public async Task SendChip(Discord.WebSocket.SocketMessage message, string[] args)
        {
            
            string name = (args.Length < 2) ? args[0] : args[1];
            bool exists = this.chipLibrary.TryGetValue(name.ToLower(), out chip Value);

            if (exists)
            {
                await message.Channel.SendMessageAsync("```" + Value.All + "```");
                //await sendChipAsEmbed(message, Value);
                return;
            }

            //else doesn't exist

            var chipList = (from kvp in chipLibrary.AsParallel().
                WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Key.Contains(name.ToLower())
                            select kvp.Value.Name).OrderBy(chip => chip).ToArray();

            switch (chipList.Length)
            {
                case 0:
                    {
                        //no chips found
                        await message.Channel.SendMessageAsync("That doesn't exist");
                        return;
                    }
                case 1:
                    {
                        //one chip has a name that contains it
                        this.chipLibrary.TryGetValue(chipList[0].ToLower(), out chip foundVal);
                        await message.Channel.SendMessageAsync("```" + foundVal.All + "```");
                        //await sendChipAsEmbed(message, foundVal);
                        return;
                    }

                default:
                    {
                        await SendStringArrayAsMessage(message, chipList);
                        return;
                    }
            }

        }

        public async Task SearchByElement(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify an element");
                return;
            }

            var chipList = (from kvp in chipLibrary.AsParallel().
                WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where Array.Exists(kvp.Value.Element,
                                Element => Element.Equals(args[1], StringComparison.OrdinalIgnoreCase))
                            select kvp.Value.Name).OrderBy(Name => Name).ToArray();
            if (chipList.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
            }
            else
            {
                await SendStringArrayAsMessage(message, chipList);
            }
        }

        public async Task SearchBySkill(SocketMessage message, string[] args)
        {
            if(args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify a skill name");
                return;
            }

            var chipList = (from kvp in chipLibrary.AsParallel().
                WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where Array.Exists(kvp.Value.Skill,
                                Skill => Skill.Equals(args[1], StringComparison.OrdinalIgnoreCase))
                            select kvp.Value.Name).OrderBy(Name => Name).ToArray();
            if (chipList.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
            }
            else
            {
                await SendStringArrayAsMessage(message, chipList);
            }
        }

        public async Task SearchBySkillCheck(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify an argument");
                return;
            }
            var chipList = (from kvp in chipLibrary.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.SkillTarget.Equals(args[1], StringComparison.OrdinalIgnoreCase) ||
                                kvp.Value.SkillUser.Equals(args[1], StringComparison.OrdinalIgnoreCase)
                            select kvp.Value.Name).OrderBy(Name => Name).ToArray();
            if (chipList.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
            }
            else
            {
                await SendStringArrayAsMessage(message, chipList);
            }
        }

        public async Task SearchBySkillUser(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify a skill");
                return;
            }
            var chipList = (from kvp in chipLibrary.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.SkillUser.Equals(args[1], StringComparison.OrdinalIgnoreCase)
                            select kvp.Value.Name).OrderBy(Name => Name).ToArray();
            if (chipList.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
            }
            else
            {
                await SendStringArrayAsMessage(message, chipList);
            }
        }

        public async Task SearchBySkillTarget(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify an argument");
                return;
            }
            var chipList = (from kvp in chipLibrary.AsParallel().WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                            where kvp.Value.SkillTarget.Equals(args[1], StringComparison.OrdinalIgnoreCase)
                            select kvp.Value.Name).OrderBy(Name => Name).ToArray();
            if (chipList.Length == 0)
            {
                await message.Channel.SendMessageAsync("Nothing matched your search");
            }
            else
            {
                await SendStringArrayAsMessage(message, chipList);
            }
        }

        private async Task SendChipAsEmbed(SocketMessage message, chip toSend)
        {
            var embed = new Discord.EmbedBuilder
            {
                Title = toSend.Name
            };
            if (toSend.Type == "Mega")
            {
                embed.Color = new Color(0x90F8F8); //Megachip Blue
            }
            else if (toSend.Type == "Giga")
            {
                embed.Color = new Color(0xF8C8D8);
            }
            MemoryStream imageStream = new MemoryStream();
            ChipImages.Instance.getElement(toSend.Element).Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
            //File.WriteAllBytes("./test.png", imageStream.GetBuffer());
            embed.AddField("Element:", String.Join(", ", toSend.Element), true);
            if (toSend.Skill[0] != "--")
            {
                embed.AddField("Skill", String.Join(", ", toSend.Skill), true);
            }
            embed.ThumbnailUrl = "attachment://unknown.png";
            //embed.ThumbnailUrl = ChipImages.fireURL;
            embed.AddField("Range:", toSend.Range, true);
            if (toSend.Damage != "--")
            {
                embed.AddField("Damage:", toSend.Damage, true);
            }
            if (!toSend.Hits.StartsWith('0'))
            {
                embed.AddField("Hits:", toSend.Hits, true);
            }
            //embed.AddField("Description:", toSend.Description);
            embed.WithDescription(toSend.Description);
            embed.WithFooter(toSend.Type);
            var valToSend = embed.Build();
            //Console.WriteLine(imageStream.Position);
            imageStream.Seek(0, SeekOrigin.Begin); //reset to the beginning because apparently not automatic
                                                   //await message.Channel.SendMessageAsync(embed: valToSend);
                                                   //await message.Channel.SendFileAsync(imageStream, "b1nzy.png");
                                                   //embed: new EmbedBuilder {ImageUrl = "attachment://b1nzy.png"}.Build());
            await message.Channel.SendFileAsync(imageStream, "unknown.png", embed: valToSend);
            imageStream.Dispose();
        }

        public static async Task SendStringArrayAsMessage(SocketMessage message, string[] toSend)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(toSend[0]);
            for (int i = 1; i < toSend.Length; i++)
            {
                builder.Append(", ");
                if ((builder.Length + toSend[i].Length) > 2000)
                {
                    await message.Channel.SendMessageAsync(builder.ToString());
                    builder.Clear();
                }
                builder.Append(toSend[i]);
            }
            await message.Channel.SendMessageAsync(builder.ToString());
        }

        public static async Task<string> GetFileContents(string path)
        {
            using (var stream = File.OpenText(path))
            {
                return await stream.ReadToEndAsync();
            }
        }

    }
}