using System;
using NUnit.Framework;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class CommandFieldInfoExtractorTests
    {
        [Serializable]
        private class MultiFieldRequest
        {
            public string name;
            public int count;
            public bool enabled;
        }

        [Serializable]
        private class DefaultValuesRequest
        {
            public int count = 42;
            public bool verbose = true;
            public string prefix = "test";
        }

        [Serializable]
        private class ArrayFieldRequest
        {
            public string[] tags;
            public int[] ids;
        }

        [Serializable]
        private class FloatDoubleRequest
        {
            public float speed;
            public double precision;
        }

        [Test]
        public void ExtractFieldInfos_UnitType_ReturnsEmptyArray()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(Unit));

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void ExtractFieldInfos_MultipleFields_ExtractsCorrectly()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(MultiFieldRequest));

            Assert.AreEqual(3, result.Length);

            Assert.AreEqual("name", result[0].name);
            Assert.AreEqual("string", result[0].type);

            Assert.AreEqual("count", result[1].name);
            Assert.AreEqual("int", result[1].type);

            Assert.AreEqual("enabled", result[2].name);
            Assert.AreEqual("bool", result[2].type);
        }

        [Test]
        public void ToSimpleTypeName_Primitives_ReturnsExpectedNames()
        {
            Assert.AreEqual("string", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(string)));
            Assert.AreEqual("int", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(int)));
            Assert.AreEqual("bool", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(bool)));
            Assert.AreEqual("float", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(float)));
            Assert.AreEqual("double", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(double)));
        }

        [Test]
        public void ToSimpleTypeName_Arrays_ReturnsExpectedNames()
        {
            Assert.AreEqual("string[]", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(string[])));
            Assert.AreEqual("int[]", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(int[])));
            Assert.AreEqual("MultiFieldRequest[]", CommandFieldInfoExtractor.ToSimpleTypeName(typeof(MultiFieldRequest[])));
        }

        [Test]
        public void ExtractFieldInfos_DefaultValues_ExtractsNonDefaultValues()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(DefaultValuesRequest));

            Assert.AreEqual(3, result.Length);

            Assert.AreEqual("count", result[0].name);
            Assert.AreEqual("42", result[0].defaultValue);

            Assert.AreEqual("verbose", result[1].name);
            Assert.AreEqual("true", result[1].defaultValue);

            Assert.AreEqual("prefix", result[2].name);
            Assert.AreEqual("test", result[2].defaultValue);
        }

        [Test]
        public void ExtractFieldInfos_DefaultZeroValues_ReturnsEmptyString()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(MultiFieldRequest));

            Assert.AreEqual("", result[0].defaultValue); // string null → ""
            Assert.AreEqual("", result[1].defaultValue); // int 0 → ""
            Assert.AreEqual("", result[2].defaultValue); // bool false → ""
        }

        [Test]
        public void ExtractFieldInfos_FloatDoubleFields_ExtractsTypes()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(FloatDoubleRequest));

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("float", result[0].type);
            Assert.AreEqual("double", result[1].type);
        }

        [Test]
        public void ExtractFieldInfos_ArrayFields_ExtractsTypes()
        {
            var result = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(ArrayFieldRequest));

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("string[]", result[0].type);
            Assert.AreEqual("int[]", result[1].type);
        }
    }
}
