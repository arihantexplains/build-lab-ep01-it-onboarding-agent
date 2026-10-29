using System;
using System.Security.Cryptography;

namespace OnboardingAgent
{
    /// <summary>Generates temporary passwords from a cryptographic random source.</summary>
    public static class TempPassword
    {
        // Look-alike characters (I, l, O, 0, 1) are left out because the password is read aloud or retyped.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%*?-+=";
        private const int Length = 16;

        /// <summary>Creates a 16 character password with at least one upper case letter, lower case letter, digit and symbol, in shuffled order.</summary>
        /// <returns>The new temporary password.</returns>
        public static string Generate()
        {
            var all = Upper + Lower + Digits + Symbols;
            var chars = new char[Length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                chars[0] = Pick(rng, Upper);
                chars[1] = Pick(rng, Lower);
                chars[2] = Pick(rng, Digits);
                chars[3] = Pick(rng, Symbols);
                for (var i = 4; i < Length; i++)
                {
                    chars[i] = Pick(rng, all);
                }

                for (var i = Length - 1; i > 0; i--)
                {
                    var j = Next(rng, i + 1);
                    var swap = chars[i];
                    chars[i] = chars[j];
                    chars[j] = swap;
                }
            }

            return new string(chars);
        }

        private static char Pick(RandomNumberGenerator rng, string set)
        {
            return set[Next(rng, set.Length)];
        }

        private static int Next(RandomNumberGenerator rng, int exclusiveMax)
        {
            // Rejection sampling avoids modulo bias.
            var max = (uint)exclusiveMax;
            var limit = uint.MaxValue - (uint.MaxValue % max);
            var buffer = new byte[4];
            while (true)
            {
                rng.GetBytes(buffer);
                var value = BitConverter.ToUInt32(buffer, 0);
                if (value < limit)
                {
                    return (int)(value % max);
                }
            }
        }
    }
}
