namespace MagicPot.Backend
{
    /// <summary>
    /// Based on https://gist.github.com/diev/e92a1fbc5ce546e15a4ac69ceca06ce8.
    /// </summary>
    public static class Base36
    {
        /// <summary>
        /// Characters table.
        /// </summary>
        private const string Alphabet32 = @"0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Decodes Base36 string back to int.
        /// </summary>
        /// <param name="toDecode">Base36 value to decode.</param>
        /// <returns>Decoded value.</returns>
        public static int DecodeInt(string toDecode)
        {
            int result = 0;

            foreach (char c in toDecode.ToLowerInvariant())
            {
                result = (result * 36) + Alphabet32.IndexOf(c);
            }

            return result;
        }

        /// <summary>
        /// Decodes Base36 string back to long.
        /// </summary>
        /// <param name="toDecode">Base36 value to decode.</param>
        /// <returns>Decoded value.</returns>
        public static long DecodeLong(string toDecode)
        {
            long result = 0;

            foreach (char c in toDecode.ToLowerInvariant())
            {
                result = (result * 36) + Alphabet32.IndexOf(c);
            }

            return result;
        }

        /// <summary>
        /// Encodes int value to Base36. Optionally, pads with zeroes.
        /// </summary>
        /// <param name="toEncode">Value to encode.</param>
        /// <param name="padLeft">Length to pad with zeroes.</param>
        /// <returns>Encoded string.</returns>
        public static string Encode(int toEncode, int padLeft = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(toEncode, 0);

            char[] tmp = ['0', '0', '0', '0', '0', '0', '0', '0', '0', '0']; // 10
            int i = tmp.Length, n = 0;

            while (toEncode > 0)
            {
                int remainder = toEncode % 36;
                toEncode /= 36;
                tmp[--i] = Alphabet32[remainder];
                n++;
            }

            if (padLeft > 0)
            {
                i = tmp.Length - padLeft;
                n = padLeft;
            }
            else if (n == 0)
            {
                i = tmp.Length - 1;
                n = 1;
            }

            return new string(tmp, i, n);
        }

        /// <summary>
        /// Encodes long value to Base36. Optionally, pads with zeroes.
        /// </summary>
        /// <param name="toEncode">Value to encode.</param>
        /// <param name="padLeft">Length to pad with zeroes.</param>
        /// <returns>Encoded string.</returns>
        public static string Encode(long toEncode, int padLeft = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(toEncode, 0);

            char[] tmp = ['0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0']; // 20
            int i = tmp.Length, n = 0;

            while (toEncode > 0)
            {
                int remainder = (int)(toEncode % 36);
                toEncode /= 36;
                tmp[--i] = Alphabet32[remainder];
                n++;
            }

            if (padLeft > 0)
            {
                i = tmp.Length - padLeft;
                n = padLeft;
            }
            else if (n == 0)
            {
                i = tmp.Length - 1;
                n = 1;
            }

            return new string(tmp, i, n);
        }
    }
}
