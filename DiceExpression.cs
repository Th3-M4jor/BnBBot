using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace csharp
{
    class DiceExpression : IDisposable
    {
        /// <summary>Our Random object.  Make it a first-class citizen so that it produces truly *random* results</summary>
        private RNGCryptoServiceProvider r;

        //private FileStream devRand;

        /*private IntPtr randPtr;

        [DllImport("./lib/randGen.so")]
        private static extern IntPtr initRand();

        [DllImport("./lib/randGen.so")]
        private static extern uint getRand(IntPtr randPtr);

        [DllImport("./lib/randGen.so")]
        private static extern void freeRand(IntPtr randPtr);*/
        private readonly Func<uint> randPtr = null;

        public DiceExpression()
        {
            r = new RNGCryptoServiceProvider();
            randPtr = InitRand();

        }

        public uint GetRandNum()
        {
            if (disposedValue == true)
            {
                //throw new Exception("resource closed");
                throw new ObjectDisposedException("DiceExpression");
            }
            return randPtr.Invoke();
        }


        /// <summary>Roll</summary>
        /// <param name="s">string to be evaluated</param>
        /// <returns>result of evaluated string</returns>
        public long R(string s, ref List<long> rolledSoFar)
        {
            if (disposedValue == true)
            {
                throw new Exception("resource closed");
            }
            long toReturn = 0;

            // Addition is lowest order of precedence
            var a = s.Split('+');

            // Add results of each group
            if (a.Count() > 1)
            {
                foreach (var b in a)
                {
                    toReturn += R(b, ref rolledSoFar);
                }
            }
            else
            {
                // Die definition is our highest order of precedence
                var d = a[0].Split('d');
                if (d.Length == 1)
                {
                    if (long.TryParse(d[0], out long AmtToAdd))
                    {
                        rolledSoFar.Add(AmtToAdd);
                        return (uint)(toReturn + AmtToAdd);
                    }
                }
                // This operand will be our die count, static digits, or else something we don't understand
                if (!long.TryParse(d[0].Trim(), out long AmtToRoll))
                    AmtToRoll = 0;


                // Multiple definitions ("2d6d8") iterate through left-to-right: (2d6)d8
                for (long i = 1; i < d.Count(); i++)
                {

                    // If we don't have a right side (face count), assume 6
                    if (!long.TryParse(d[i].Trim(), out long f))
                        f = 6;

                    long u = 0;

                    // If we don't have a die count, use 1
                    for (long j = 0; j < (AmtToRoll == 0 ? 1 : AmtToRoll); j++)
                    {
                        /*byte[] arrToFill = new byte[sizeof(int)];
                        
                        if(f < 256)
                        {
                            byte[] singleByte = new byte[1];
                            r.GetBytes(singleByte);
                            arrToFill[0] = singleByte[0];
                        }
                        else
                        {
                            r.GetBytes(arrToFill);
                        }
                        //u += r.Next(1, f);
                        int numToAdd = (BitConverter.ToInt32(arrToFill, 0) % f);
                        if (numToAdd < 0) numToAdd *= -1;
                        rolledSoFar.Add(numToAdd + 1);*/
                        var numToAdd = (randPtr.Invoke() % f + 1);
                        rolledSoFar.Add(numToAdd);

                        u += numToAdd;
                    }

                    toReturn += u;
                }
            }
            return toReturn;
        }

        private uint GetRandLinux()
        {
            Span<byte> bytes = stackalloc byte[4];
            var devRand = File.OpenRead("/dev/random/");
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)devRand.ReadByte();
            }
            devRand.Dispose();
            return BitConverter.ToUInt32(bytes);
        }

        private uint GetRandWindows()
        {
            Span<byte> bytes = stackalloc byte[4];
            r.GetBytes(bytes);
            return BitConverter.ToUInt32(bytes);
        }

        private Func<uint> InitRand()
        {
            r = new RNGCryptoServiceProvider();
            return GetRandWindows;
            /*if (File.Exists("/dev/random"))
            {
                Console.WriteLine("using /dev/random");
                //devRand = File.OpenRead("/dev/random");
                return GetRandLinux;
            }
            else
            {
                Console.WriteLine("No /dev/random using alternative");
                r = new RNGCryptoServiceProvider();
                return GetRandWindows;
            }*/
        }



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    r?.Dispose();
                    //devRand?.Dispose();
                    // TODO: dispose managed state (managed objects).
                }
                //freeRand(randPtr);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DiceExpression()
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