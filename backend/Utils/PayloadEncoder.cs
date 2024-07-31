namespace MagicPot.Backend.Utils
{
    using System.Buffers.Binary;
    using MagicPot.Backend.Data;
    using TonLibDotNet.Cells;

    public static class PayloadEncoder
    {
        private const long TransactionPayload = 0x2ab06b49d43ebd3b; // random value

        public static byte[] Encode(long potId, long userId)
        {
            var payloadBytes = new byte[8];
            var potBytes = new byte[8];
            var userBytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(payloadBytes, TransactionPayload);
            BinaryPrimitives.WriteInt64BigEndian(potBytes, potId);
            BinaryPrimitives.WriteInt64BigEndian(userBytes, userId);

            var res = new byte[24];
            for (var i = 0; i < 8; i++)
            {
                res[(i * 3) + 0] = payloadBytes[i];
                res[(i * 3) + 1] = potBytes[i];
                res[(i * 3) + 2] = userBytes[i];
            }

            return res;
        }

        public static Cell EncodeToCell(long potId, long userId, string? referrerAddress)
        {
            return new CellBuilder()
                .StoreBytes(PayloadEncoder.Encode(potId, userId))
                .StoreBit(false) // not used, just to break byte alignment of address value
                .StoreAddressIntStd2(referrerAddress)
                .Build();
        }

        public static bool TryDecode(byte[] bytes, out long potId, out long userId)
        {
            potId = 0;
            userId = 0;

            if (bytes.Length != 24)
            {
                return false;
            }

            var payloadBytes = new[] { bytes[0], bytes[3], bytes[6], bytes[9], bytes[12], bytes[15], bytes[18], bytes[21] };
            var payload = BinaryPrimitives.ReadInt64BigEndian(payloadBytes);
            if (payload != TransactionPayload)
            {
                return false;
            }

            var potBytes = new[] { bytes[1], bytes[4], bytes[7], bytes[10], bytes[13], bytes[16], bytes[19], bytes[22] };
            potId = BinaryPrimitives.ReadInt64BigEndian(potBytes);

            var userBytes = new[] { bytes[2], bytes[5], bytes[8], bytes[11], bytes[14], bytes[17], bytes[20], bytes[23] };
            userId = BinaryPrimitives.ReadInt64BigEndian(userBytes);

            return true;
        }
    }
}
