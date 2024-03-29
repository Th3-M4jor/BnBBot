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

        private readonly DiceExpression die;
        private Dice()
        {
            die = new DiceExpression();
        }

        public async Task RollDice(SocketMessage message, string[] args)
        {
            if (args.Length < 2)
            {
                await message.Channel.SendMessageAsync("You must specify a number of dice to roll");
                return;
            }
            var joinedArgs = (new ArraySegment<string>(args, 1, args.Length - 1));
            var toRoll = string.Join(' ', joinedArgs).ToLower();
            List<long> totalRolls = new List<long>();
            long result = die.R(toRoll, ref totalRolls);
            string eachRoll = string.Join(", ", totalRolls.Select(x => x.ToString()).ToArray());
            if (eachRoll.Length > 1900)
            {
                eachRoll = "There were too many die rolls to show the result of each one";
            }
            await message.Channel.SendMessageAsync(string.Format("{0}, you rolled: {1}, ({2})", message.Author.Mention, result, eachRoll));
        }

        public uint GetRandomNum()
        {
            return die.GetRandNum();
        }

        public async Task RollStats(SocketMessage message, string[] args = null)
        {
            long[] rolls = new long[4];
            long[] stats = new long[6];
            var ignoreList = new List<long>();
            for (int i = 0; i < 6; i++)
            {
                ignoreList.Clear();
                for (int j = 0; j < 4; j++)
                {
                    rolls[j] = die.R("1d6", ref ignoreList);
                }
                int toSkip = FindIndexOfSmallest(ref rolls);
                long sum = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (j == toSkip) continue;
                    sum += rolls[j];
                }
                stats[i] = sum;
            }

            await message.Channel.SendMessageAsync(
                string.Format("{0}, 4d6 drop the lowest:\n{1}",
                                    message.Author.Mention,
                                    string.Join(", ", stats.Select(x => x.ToString()).ToArray())));

        }

        private int FindIndexOfSmallest<T>(ref T[] toCheck) where T : IComparable<T>
        {
            int index = 0;

            for (int i = 1; i < toCheck.Length; i++)
            {
                //if (toCheck[i] < toCheck[index])
                if (toCheck[i].CompareTo(toCheck[index]) < 0)
                    index = i;
            }
            return index;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    die.Dispose();
                    // TODO: dispose managed state (managed objects).
                }
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Dice()
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