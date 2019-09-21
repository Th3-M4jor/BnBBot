using System;
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

        private IntPtr randPtr;
        private bool randFreed = true;

        [DllImport("./lib/randGen.so")]
        private static extern IntPtr initRand();

        [DllImport("./lib/randGen.so")]
        private static extern uint getRand(IntPtr randPtr);

        [DllImport("./lib/randGen.so")]
        private static extern void freeRand(IntPtr randPtr);


        public DiceExpression()
        {
            r = new RNGCryptoServiceProvider();
            randPtr = initRand();
            randFreed = false;
        }

        public uint getRandNum()
        {
            if(randFreed == true)
            {
                throw new Exception("resource closed");
            }
            return getRand(randPtr);
        }


        /// <summary>Roll</summary>
        /// <param name="s">string to be evaluated</param>
        /// <returns>result of evaluated string</returns>
        public uint R(string s, ref List<uint> rolledSoFar)
        {
            if(randFreed == true)
            {
                throw new Exception("resource closed");
            }
            uint toReturn = 0;

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
                int AmtToRoll = 0;
                // Die definition is our highest order of precedence
                var d = a[0].Split('d');

                // This operand will be our die count, static digits, or else something we don't understand
                if (!int.TryParse(d[0].Trim(), out AmtToRoll))
                    AmtToRoll = 0;

                int f;

                // Multiple definitions ("2d6d8") iterate through left-to-right: (2d6)d8
                for (int i = 1; i < d.Count(); i++)
                {
                    // If we don't have a right side (face count), assume 6
                    if (!int.TryParse(d[i].Trim(), out f))
                        f = 6;

                    uint u = 0;

                    // If we don't have a die count, use 1
                    for (int j = 0; j < (AmtToRoll == 0 ? 1 : AmtToRoll); j++)
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
                        var numToAdd = (uint)(getRand(randPtr) % f + 1);
                        rolledSoFar.Add(numToAdd);

                        u += numToAdd;
                    }

                    toReturn += u;
                }
            }
            return toReturn;
        }

        public void Dispose()
        {
            if(randFreed == true) return; //already disposed
            freeRand(randPtr);
            randFreed = true;
        }
    }
}