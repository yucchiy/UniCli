using System;

namespace UniCli.Protocol
{
    public static class ProtocolConstants
    {
        public static readonly byte[] MagicBytes = { 0x55, 0x43, 0x4C, 0x49 }; // "UCLI"
        public const ushort ProtocolVersion = 1;
        public const int HandshakeSize = 6; // 4 (magic) + 2 (version)
        public const int MaxMessageSize = 1024 * 1024; // 1 MB

        public static bool ValidateMagicBytes(byte[] buffer)
        {
            return buffer.Length >= 4
                && buffer[0] == MagicBytes[0]
                && buffer[1] == MagicBytes[1]
                && buffer[2] == MagicBytes[2]
                && buffer[3] == MagicBytes[3];
        }

        public static byte[] BuildHandshakeBuffer()
        {
            var buffer = new byte[HandshakeSize];
            Array.Copy(MagicBytes, 0, buffer, 0, 4);
            BitConverter.GetBytes(ProtocolVersion).CopyTo(buffer, 4);
            return buffer;
        }
    }
}
