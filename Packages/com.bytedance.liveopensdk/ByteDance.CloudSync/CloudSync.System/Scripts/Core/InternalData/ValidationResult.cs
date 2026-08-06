using System;

namespace ByteDance.CloudSync.MatchManager
{
    internal class ValidationResult : ValidationResult<object>
    {
        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsSuccess = true
            };
        }

        public static ValidationResult Success(string message, object data = default)
        {
            return new ValidationResult
            {
                IsSuccess = true,
                Message = message,
                Data = data,
            };
        }

        public static ValidationResult Fail()
        {
            return new ValidationResult
            {
                IsSuccess = false,
                Message = string.Empty,
            };
        }

        public static ValidationResult Fail(string message, object data = default, Exception exception = null)
        {
            return new ValidationResult
            {
                IsSuccess = false,
                Message = message,
                Data = data,
                Exception = exception,
            };
        }
    }

    internal class ValidationResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public Exception Exception { get; set; }

        public static implicit operator bool(ValidationResult<T> obj) => obj is { IsSuccess: true };
    }
}