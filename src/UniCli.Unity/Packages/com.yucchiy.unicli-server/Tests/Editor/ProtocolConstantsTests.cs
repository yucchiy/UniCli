using System;
using NUnit.Framework;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class ProtocolConstantsTests
    {
        [Test]
        public void ValidateMagicBytes_CorrectBytes_ReturnsTrue()
        {
            var buffer = new byte[] { 0x55, 0x43, 0x4C, 0x49, 0x00, 0x00 };
            Assert.IsTrue(ProtocolConstants.ValidateMagicBytes(buffer));
        }

        [Test]
        public void ValidateMagicBytes_WrongBytes_ReturnsFalse()
        {
            var buffer = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.IsFalse(ProtocolConstants.ValidateMagicBytes(buffer));
        }

        [Test]
        public void ValidateMagicBytes_TooShort_ReturnsFalse()
        {
            var buffer = new byte[] { 0x55, 0x43 };
            Assert.IsFalse(ProtocolConstants.ValidateMagicBytes(buffer));
        }

        [Test]
        public void ValidateMagicBytes_EmptyBuffer_ReturnsFalse()
        {
            Assert.IsFalse(ProtocolConstants.ValidateMagicBytes(Array.Empty<byte>()));
        }

        [Test]
        public void ValidateMagicBytes_ExactFourBytes_ReturnsTrue()
        {
            var buffer = new byte[] { 0x55, 0x43, 0x4C, 0x49 };
            Assert.IsTrue(ProtocolConstants.ValidateMagicBytes(buffer));
        }

        [Test]
        public void BuildHandshakeBuffer_ReturnsCorrectLength()
        {
            var buffer = ProtocolConstants.BuildHandshakeBuffer();
            Assert.AreEqual(ProtocolConstants.HandshakeSize, buffer.Length);
        }

        [Test]
        public void BuildHandshakeBuffer_StartsWithMagicBytes()
        {
            var buffer = ProtocolConstants.BuildHandshakeBuffer();
            Assert.IsTrue(ProtocolConstants.ValidateMagicBytes(buffer));
        }

        [Test]
        public void BuildHandshakeBuffer_ContainsProtocolVersion()
        {
            var buffer = ProtocolConstants.BuildHandshakeBuffer();
            var version = BitConverter.ToUInt16(buffer, 4);
            Assert.AreEqual(ProtocolConstants.ProtocolVersion, version);
        }

        [Test]
        public void HandshakeSize_IsSix()
        {
            Assert.AreEqual(6, ProtocolConstants.HandshakeSize);
        }

        [Test]
        public void MaxMessageSize_IsOneMB()
        {
            Assert.AreEqual(1024 * 1024, ProtocolConstants.MaxMessageSize);
        }
    }
}
