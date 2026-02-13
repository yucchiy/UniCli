namespace UniCli.Protocol
{
    public static class ProtocolConstants
    {
        public static readonly byte[] MagicBytes = { 0x55, 0x43, 0x4C, 0x49 }; // "UCLI"
        public const ushort ProtocolVersion = 1;
        public const int HandshakeSize = 6; // 4 (magic) + 2 (version)
    }
}
