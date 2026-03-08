using System;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Internal;

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

        [Serializable]
        private class NestedConfig
        {
            public string profile;
            public int retries;
        }

        [Serializable]
        private class NestedTypeRequest
        {
            public NestedConfig config;
            public NestedConfig[] configs;
        }

        [Serializable]
        private class RecursiveRequest
        {
            public RecursiveRequest child;
        }

        [Serializable]
        private class MutualRecursiveA
        {
            public MutualRecursiveB child;
        }

        [Serializable]
        private class MutualRecursiveB
        {
            public MutualRecursiveA parent;
        }

        [Serializable]
        private class AlphaContainer
        {
            [Serializable]
            public class Duplicate
            {
                public string profile;
            }
        }

        [Serializable]
        private class BetaContainer
        {
            [Serializable]
            public class Duplicate
            {
                public int retries;
            }
        }

        [Serializable]
        private class CollidingTypeRequest
        {
            public AlphaContainer.Duplicate alpha;
            public BetaContainer.Duplicate beta;
            public AlphaContainer.Duplicate[] alphaArray;
        }

        [Test]
        public void Extract_UnitType_ReturnsEmptyMetadata()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(Unit));

            Assert.AreEqual(0, result.Fields.Length);
            Assert.AreEqual(0, result.TypeDetails.Length);
        }

        [Test]
        public void Extract_MultipleFields_ExtractsCorrectly()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(MultiFieldRequest));

            Assert.AreEqual(3, result.Fields.Length);
            Assert.AreEqual(0, result.TypeDetails.Length);

            Assert.AreEqual("name", result.Fields[0].name);
            Assert.AreEqual("string", result.Fields[0].type);
            Assert.AreEqual("", result.Fields[0].typeId);

            Assert.AreEqual("count", result.Fields[1].name);
            Assert.AreEqual("int", result.Fields[1].type);
            Assert.AreEqual("", result.Fields[1].typeId);

            Assert.AreEqual("enabled", result.Fields[2].name);
            Assert.AreEqual("bool", result.Fields[2].type);
            Assert.AreEqual("", result.Fields[2].typeId);
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
        public void Extract_DefaultValues_ExtractsNonDefaultValues()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(DefaultValuesRequest));

            Assert.AreEqual(3, result.Fields.Length);

            Assert.AreEqual("count", result.Fields[0].name);
            Assert.AreEqual("42", result.Fields[0].defaultValue);

            Assert.AreEqual("verbose", result.Fields[1].name);
            Assert.AreEqual("true", result.Fields[1].defaultValue);

            Assert.AreEqual("prefix", result.Fields[2].name);
            Assert.AreEqual("test", result.Fields[2].defaultValue);
        }

        [Test]
        public void Extract_DefaultZeroValues_ReturnsEmptyString()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(MultiFieldRequest));

            Assert.AreEqual("", result.Fields[0].defaultValue); // string null -> ""
            Assert.AreEqual("", result.Fields[1].defaultValue); // int 0 -> ""
            Assert.AreEqual("", result.Fields[2].defaultValue); // bool false -> ""
        }

        [Test]
        public void Extract_FloatDoubleFields_ExtractsTypes()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(FloatDoubleRequest));

            Assert.AreEqual(2, result.Fields.Length);
            Assert.AreEqual("float", result.Fields[0].type);
            Assert.AreEqual("double", result.Fields[1].type);
        }

        [Test]
        public void Extract_ArrayFields_ExtractsTypes()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(ArrayFieldRequest));

            Assert.AreEqual(2, result.Fields.Length);
            Assert.AreEqual("string[]", result.Fields[0].type);
            Assert.AreEqual("int[]", result.Fields[1].type);
            Assert.AreEqual(0, result.TypeDetails.Length);
        }

        [Test]
        public void Extract_NestedTypes_ExtractsTypeDetails()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(NestedTypeRequest));

            Assert.AreEqual(2, result.Fields.Length);

            Assert.AreEqual("config", result.Fields[0].name);
            Assert.AreEqual("NestedConfig", result.Fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(NestedConfig)),
                result.Fields[0].typeId);

            Assert.AreEqual("configs", result.Fields[1].name);
            Assert.AreEqual("NestedConfig[]", result.Fields[1].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(NestedConfig)),
                result.Fields[1].typeId);

            Assert.AreEqual(1, result.TypeDetails.Length);
            Assert.AreEqual("NestedConfig", result.TypeDetails[0].typeName);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(NestedConfig)),
                result.TypeDetails[0].typeId);
            Assert.AreEqual(2, result.TypeDetails[0].fields.Length);
            Assert.AreEqual("profile", result.TypeDetails[0].fields[0].name);
            Assert.AreEqual("string", result.TypeDetails[0].fields[0].type);
            Assert.AreEqual("", result.TypeDetails[0].fields[0].typeId);
            Assert.AreEqual("retries", result.TypeDetails[0].fields[1].name);
            Assert.AreEqual("int", result.TypeDetails[0].fields[1].type);
            Assert.AreEqual("", result.TypeDetails[0].fields[1].typeId);
        }

        [Test]
        public void Extract_RecursiveType_CollectsFiniteTypeDetails()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(RecursiveRequest));

            Assert.AreEqual(1, result.Fields.Length);
            Assert.AreEqual("RecursiveRequest", result.Fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(RecursiveRequest)),
                result.Fields[0].typeId);
            Assert.AreEqual(1, result.TypeDetails.Length);
            Assert.AreEqual("RecursiveRequest", result.TypeDetails[0].typeName);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(RecursiveRequest)),
                result.TypeDetails[0].typeId);
            Assert.AreEqual(1, result.TypeDetails[0].fields.Length);
            Assert.AreEqual("child", result.TypeDetails[0].fields[0].name);
            Assert.AreEqual("RecursiveRequest", result.TypeDetails[0].fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(RecursiveRequest)),
                result.TypeDetails[0].fields[0].typeId);
        }

        [Test]
        public void Extract_MutualRecursiveTypes_CollectsEachTypeOnce()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(MutualRecursiveA));

            Assert.AreEqual(1, result.Fields.Length);
            Assert.AreEqual("MutualRecursiveB", result.Fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(MutualRecursiveB)),
                result.Fields[0].typeId);
            Assert.AreEqual(2, result.TypeDetails.Length);

            Assert.AreEqual("MutualRecursiveB", result.TypeDetails[0].typeName);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(MutualRecursiveB)),
                result.TypeDetails[0].typeId);
            Assert.AreEqual(1, result.TypeDetails[0].fields.Length);
            Assert.AreEqual("MutualRecursiveA", result.TypeDetails[0].fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(MutualRecursiveA)),
                result.TypeDetails[0].fields[0].typeId);

            Assert.AreEqual("MutualRecursiveA", result.TypeDetails[1].typeName);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(MutualRecursiveA)),
                result.TypeDetails[1].typeId);
            Assert.AreEqual(1, result.TypeDetails[1].fields.Length);
            Assert.AreEqual("MutualRecursiveB", result.TypeDetails[1].fields[0].type);
            Assert.AreEqual(
                CommandFieldInfoExtractor.GetTypeId(typeof(MutualRecursiveB)),
                result.TypeDetails[1].fields[0].typeId);
        }

        [Test]
        public void Extract_CollidingTypeNames_UsesDistinctTypeIds()
        {
            var result = CommandFieldInfoExtractor.Extract(typeof(CollidingTypeRequest));
            var alphaTypeId = CommandFieldInfoExtractor.GetTypeId(typeof(AlphaContainer.Duplicate));
            var betaTypeId = CommandFieldInfoExtractor.GetTypeId(typeof(BetaContainer.Duplicate));

            Assert.AreEqual(3, result.Fields.Length);

            Assert.AreEqual("Duplicate", result.Fields[0].type);
            Assert.AreEqual(alphaTypeId, result.Fields[0].typeId);

            Assert.AreEqual("Duplicate", result.Fields[1].type);
            Assert.AreEqual(betaTypeId, result.Fields[1].typeId);

            Assert.AreEqual("Duplicate[]", result.Fields[2].type);
            Assert.AreEqual(alphaTypeId, result.Fields[2].typeId);

            Assert.AreEqual(2, result.TypeDetails.Length);
            Assert.AreEqual("Duplicate", result.TypeDetails[0].typeName);
            Assert.AreEqual(alphaTypeId, result.TypeDetails[0].typeId);
            Assert.AreEqual("Duplicate", result.TypeDetails[1].typeName);
            Assert.AreEqual(betaTypeId, result.TypeDetails[1].typeId);
            Assert.AreNotEqual(result.TypeDetails[0].typeId, result.TypeDetails[1].typeId);

            Assert.AreEqual("profile", result.TypeDetails[0].fields[0].name);
            Assert.AreEqual("", result.TypeDetails[0].fields[0].typeId);
            Assert.AreEqual("retries", result.TypeDetails[1].fields[0].name);
            Assert.AreEqual("", result.TypeDetails[1].fields[0].typeId);
        }
    }
}
