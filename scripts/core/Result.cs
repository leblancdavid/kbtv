#nullable enable

using System;

namespace KBTV.Core
{
    /// <summary>
    /// Represents either a successful result or an error.
    /// Replaces null returns and bool+out patterns for cleaner error handling.
    /// </summary>
    public readonly struct Result<T>
    {
        public T? Value { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }

        private Result(T? value, bool isSuccess, string errorMessage, string errorCode)
        {
            Value = value;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static Result<T> Ok(T value) => new(value, true, string.Empty, string.Empty);

        public static Result<T> Fail(string errorMessage, string errorCode = "ERROR")
            => new(default, false, errorMessage, errorCode);

        public static Result<T> Fail(Exception exception, string errorCode = "EXCEPTION")
            => new(default, false, exception.Message, errorCode);

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, string, TResult> onFailure)
        {
            return IsSuccess
                ? onSuccess(Value!)
                : onFailure(ErrorMessage, ErrorCode);
        }

        public void Switch(Action<T> onSuccess, Action<string, string> onFailure)
        {
            if (IsSuccess)
                onSuccess(Value!);
            else
                onFailure(ErrorMessage, ErrorCode);
        }

        public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            return IsSuccess
                ? Result<TResult>.Ok(mapper(Value!))
                : Result<TResult>.Fail(ErrorMessage, ErrorCode);
        }

        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
                action(Value!);
            return this;
        }

        public Result<T> OnFailure(Action<string, string> action)
        {
            if (!IsSuccess)
                action(ErrorMessage, ErrorCode);
            return this;
        }

        public T? ValueOrDefault() => Value;
        public T ValueOrThrow() => IsSuccess ? Value! : throw new InvalidOperationException(ErrorMessage);

        public override string ToString()
        {
            return IsSuccess
                ? $"Ok({Value})"
                : $"Fail({ErrorCode}: {ErrorMessage})";
        }
    }

    public static class ResultExtensions
    {
        public static Result<T> ToResult<T>(this T? value, string errorMessage, string errorCode = "NULL")
        {
            return value != null
                ? Result<T>.Ok(value)
                : Result<T>.Fail(errorMessage, errorCode);
        }

        public static Result<T> Try<T>(Func<T> func, string errorCode = "OPERATION_FAILED")
        {
            try
            {
                return Result<T>.Ok(func());
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex, errorCode);
            }
        }
    }
}
