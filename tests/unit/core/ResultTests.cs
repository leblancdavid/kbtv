#nullable enable

using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;

namespace KBTV.Tests.Unit.Core
{
    public class ResultTests : KBTVTestClass
    {
        public ResultTests(Node testScene) : base(testScene) { }

        [Test]
        public void Ok_CreatesSuccessfulResult()
        {
            var result = Result<int>.Ok(42);
            AssertThat(result.IsSuccess);
            AssertThat(!result.IsFailure);
            AssertThat(result.Value == 42);
            AssertThat(string.IsNullOrEmpty(result.ErrorMessage));
            AssertThat(string.IsNullOrEmpty(result.ErrorCode));
        }

        [Test]
        public void Fail_CreatesFailedResult()
        {
            var result = Result<int>.Fail("Something went wrong", "ERR_001");
            AssertThat(!result.IsSuccess);
            AssertThat(result.IsFailure);
            AssertThat(result.Value == default);
            AssertThat(result.ErrorMessage == "Something went wrong");
            AssertThat(result.ErrorCode == "ERR_001");
        }

        [Test]
        public void Fail_WithException_CreatesFailedResult()
        {
            var exception = new InvalidOperationException("Test exception");
            var result = Result<int>.Fail(exception, "EXCEPTION");
            AssertThat(!result.IsSuccess);
            AssertThat(result.ErrorMessage == "Test exception");
            AssertThat(result.ErrorCode == "EXCEPTION");
        }

        [Test]
        public void Match_Success_CallsOnSuccess()
        {
            var result = Result<string>.Ok("hello");
            var matched = result.Match(
                value => value.ToUpper(),
                (msg, code) => "fail"
            );
            AssertThat(matched == "HELLO");
        }

        [Test]
        public void Match_Failure_CallsOnFailure()
        {
            var result = Result<string>.Fail("error", "ERR");
            var matched = result.Match(
                value => "success",
                (msg, code) => $"{code}: {msg}"
            );
            AssertThat(matched == "ERR: error");
        }

        [Test]
        public void Switch_Success_CallsOnSuccess()
        {
            var result = Result<int>.Ok(10);
            string? captured = null;
            result.Switch(
                value => captured = value.ToString(),
                (_, _) => captured = "fail"
            );
            AssertThat(captured == "10");
        }

        [Test]
        public void Switch_Failure_CallsOnFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            string? captured = null;
            result.Switch(
                value => captured = "success",
                (msg, code) => captured = code
            );
            AssertThat(captured == "ERR");
        }

        [Test]
        public void Map_TransformsSuccessValue()
        {
            var result = Result<int>.Ok(5);
            var mapped = result.Map(x => x * 2);
            AssertThat(mapped.IsSuccess);
            AssertThat(mapped.Value == 10);
        }

        [Test]
        public void Map_PropagatesFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            var mapped = result.Map(x => x * 2);
            AssertThat(!mapped.IsSuccess);
            AssertThat(mapped.ErrorMessage == "error");
        }

        [Test]
        public void OnSuccess_ExecutesActionOnSuccess()
        {
            var result = Result<int>.Ok(5);
            int captured = 0;
            result.OnSuccess(value => captured = value);
            AssertThat(captured == 5);
        }

        [Test]
        public void OnSuccess_DoesNotExecuteOnFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            int captured = 0;
            result.OnSuccess(value => captured = value);
            AssertThat(captured == 0);
        }

        [Test]
        public void OnFailure_ExecutesActionOnFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            string? captured = null;
            result.OnFailure((msg, code) => captured = code);
            AssertThat(captured == "ERR");
        }

        [Test]
        public void OnFailure_DoesNotExecuteOnSuccess()
        {
            var result = Result<int>.Ok(5);
            bool executed = false;
            result.OnFailure((_, _) => executed = true);
            AssertThat(!executed);
        }

        [Test]
        public void ValueOrDefault_ReturnsValue()
        {
            var result = Result<int>.Ok(42);
            AssertThat(result.ValueOrDefault() == 42);
        }

        [Test]
        public void ValueOrDefault_ReturnsDefaultOnFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            AssertThat(result.ValueOrDefault() == 0);
        }

        [Test]
        public void ValueOrThrow_ReturnsValueOnSuccess()
        {
            var result = Result<int>.Ok(42);
            AssertThat(result.ValueOrThrow() == 42);
        }

        [Test]
        public void ValueOrThrow_ThrowsOnFailure()
        {
            var result = Result<int>.Fail("error", "ERR");
            AssertThrows<InvalidOperationException>(() => result.ValueOrThrow());
        }

        [Test]
        public void ToString_Success_ShowsOkFormat()
        {
            var result = Result<int>.Ok(42);
            AssertThat(result.ToString() == "Ok(42)");
        }

        [Test]
        public void ToString_Failure_ShowsFailFormat()
        {
            var result = Result<int>.Fail("error", "ERR_001");
            AssertThat(result.ToString() == "Fail(ERR_001: error)");
        }
    }
}
