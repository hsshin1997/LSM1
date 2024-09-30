namespace LSM.CrcLibrary
{
    public static class CrcCcitt
    {
        private const ushort Polynomial = 0x1021;
        private const ushort InitialValue = 0xFFFF;

        public static byte[] ComputeChecksum(byte[] data)
        {
            ushort crc = InitialValue;

            foreach (byte b in data)
            {
                crc ^= (ushort)(b<<8);

                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 0x8000) != 0
                        ? (ushort)((crc << 1) ^ Polynomial)
                        : (ushort)((crc << 1));
                }
            }

            byte[] crcBytes = new byte[2];
            crcBytes[0] = (byte)(crc >> 8); // high byte
            crcBytes[1] = (byte)(crc & 0xFF); // low byte
            return crcBytes; 
        }

        public static bool VerifyCheckSum(byte[] dataWithCrc)
        {
            if (dataWithCrc.Length < 3) return false;

            int dataLength = dataWithCrc.Length - 2;
            ushort computedCrc = ComputeChecksum(dataWithCrc, 0, dataLength);

            ushort receivedCrc = (ushort)((dataWithCrc[dataLength] << 8) | dataWithCrc[dataLength + 1]);

            return computedCrc == receivedCrc;
        }

        public static ushort ComputeChecksum(byte[] data, int offset, int length)
        {
            ushort crc = InitialValue;

            for (int i = offset; i < offset + length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 0x8000) != 0
                        ? (ushort)((crc << 1) ^ Polynomial)
                        : (ushort)((crc << 1));
                }
            }

            return crc;
        }
    }
}
