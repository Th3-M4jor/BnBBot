using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using System;

namespace csharp
{
    class Dice : IDisposable
    {
        public static readonly Dice instance = new Dice();

        private DiceExpression die;
        private Dice()
        {
            die = new DiceExpression();
        }

        public async Task rollDice(SocketMessage message, string[] args)
        {
            var joinedArgs = (new ArraySegment<string>(args, 1, args.Length - 1));
            var toRoll = string.Join(' ', joinedArgs).ToLower();
            List<uint> totalRolls = new List<uint>();
            uint result = die.R(toRoll, ref totalRolls);
            string eachRoll = string.Join(", " , totalRolls.Select(x => x.ToString()).ToArray());
            if(eachRoll.Length > 1900)
            {
                eachRoll = "There were too many die rolls to show the result of each one";
            }
            await message.Channel.SendMessageAsync(string.Format("{0}, you rolled: {1}, ({2})", message.Author.Mention, result, eachRoll));
        }

        public uint getRandomNum()
        {
            return die.getRandNum();
        }

        public void Dispose()
        {
            die.Dispose();
        }

        public async Task rollStats(SocketMessage message)
        {
            uint[] rolls = new uint[4];
            uint[] stats = new uint[6];
            var ignoreList = new List<uint>();
            for (int i = 0; i < 6; i++)
            {
                ignoreList.Clear();
                for (int j = 0; j < 4; j++)
                {
                    rolls[j] = die.R("1d6", ref ignoreList);
                }
                int toSkip = findIndexOfSmallest(ref rolls);
                uint sum = 0;
                for(int j = 0; j < 4; j++)
                {
                    if(j == toSkip) continue;
                    sum += rolls[j];
                }
                stats[i] = sum;
            }

            await message.Channel.SendMessageAsync(
                string.Format("{0}, 4d6 drop the lowest:\n{1}",
                                    message.Author.Mention,
                                    string.Join(", ", stats.Select(x => x.ToString()).ToArray())));

        }

        private int findIndexOfSmallest(ref uint[] toCheck)
        {
            int index = 0;

            for (int i = 1; i < toCheck.Length; i++)
            {
                if (toCheck[i] < toCheck[index])
                    index = i;
            }
            return index;
        }

    }
}