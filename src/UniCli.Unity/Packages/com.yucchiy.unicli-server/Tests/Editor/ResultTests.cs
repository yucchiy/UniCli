using System;
using NUnit.Framework;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class ResultTests
    {
        [Test]
        public void Success_IsSuccess_ReturnsTrue()
        {
            var result = Result<int, string>.Success(42);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.IsError);
        }

        [Test]
        public void Success_SuccessValue_ReturnsValue()
        {
            var result = Result<int, string>.Success(42);
            Assert.AreEqual(42, result.SuccessValue);
        }

        [Test]
        public void Success_ErrorValue_ThrowsInvalidOperation()
        {
            var result = Result<int, string>.Success(42);
            Assert.Throws<InvalidOperationException>(() => { var _ = result.ErrorValue; });
        }

        [Test]
        public void Error_IsError_ReturnsTrue()
        {
            var result = Result<int, string>.Error("fail");
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void Error_ErrorValue_ReturnsValue()
        {
            var result = Result<int, string>.Error("fail");
            Assert.AreEqual("fail", result.ErrorValue);
        }

        [Test]
        public void Error_SuccessValue_ThrowsInvalidOperation()
        {
            var result = Result<int, string>.Error("fail");
            Assert.Throws<InvalidOperationException>(() => { var _ = result.SuccessValue; });
        }

        [Test]
        public void MatchFunc_Success_CallsOnSuccess()
        {
            var result = Result<int, string>.Success(10);
            var output = result.Match(
                v => v * 2,
                e => -1);
            Assert.AreEqual(20, output);
        }

        [Test]
        public void MatchFunc_Error_CallsOnError()
        {
            var result = Result<int, string>.Error("bad");
            var output = result.Match(
                v => "ok",
                e => e.ToUpper());
            Assert.AreEqual("BAD", output);
        }

        [Test]
        public void MatchFunc_NullOnSuccess_ThrowsArgumentNull()
        {
            var result = Result<int, string>.Success(1);
            Assert.Throws<ArgumentNullException>(() =>
                result.Match<int>(null, e => 0));
        }

        [Test]
        public void MatchFunc_NullOnError_ThrowsArgumentNull()
        {
            var result = Result<int, string>.Success(1);
            Assert.Throws<ArgumentNullException>(() =>
                result.Match<int>(v => v, null));
        }

        [Test]
        public void MatchAction_Success_CallsOnSuccess()
        {
            var result = Result<int, string>.Success(5);
            var called = "";
            result.Match(
                v => called = $"success:{v}",
                e => called = $"error:{e}");
            Assert.AreEqual("success:5", called);
        }

        [Test]
        public void MatchAction_Error_CallsOnError()
        {
            var result = Result<int, string>.Error("oops");
            var called = "";
            result.Match(
                v => called = $"success:{v}",
                e => called = $"error:{e}");
            Assert.AreEqual("error:oops", called);
        }

        [Test]
        public void MatchAction_NullOnSuccess_ThrowsArgumentNull()
        {
            var result = Result<int, string>.Success(1);
            Assert.Throws<ArgumentNullException>(() =>
                result.Match(null, e => { }));
        }

        [Test]
        public void MatchAction_NullOnError_ThrowsArgumentNull()
        {
            var result = Result<int, string>.Success(1);
            Assert.Throws<ArgumentNullException>(() =>
                result.Match(v => { }, null));
        }
    }
}
