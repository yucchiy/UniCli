using System;

namespace UniCli.Protocol
{
    /// <summary>
    /// Represents either a success or failure result
    /// </summary>
    /// <typeparam name="TSuccess">Type of the success value</typeparam>
    /// <typeparam name="TError">Type of the error value</typeparam>
    public readonly struct Result<TSuccess, TError>
    {
        private readonly bool _isSuccess;
        private readonly TSuccess? _successValue;
        private readonly TError? _errorValue;

        private Result(bool isSuccess, TSuccess? successValue, TError? errorValue)
        {
            _isSuccess = isSuccess;
            _successValue = successValue;
            _errorValue = errorValue;
        }

        /// <summary>
        /// Whether the result is a success
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Whether the result is an error
        /// </summary>
        public bool IsError => !_isSuccess;

        /// <summary>
        /// The success value (throws if IsSuccess is false)
        /// </summary>
        public TSuccess SuccessValue
        {
            get
            {
                if (!_isSuccess)
                    throw new InvalidOperationException("Cannot access SuccessValue when result is an error");
                return _successValue!;
            }
        }

        /// <summary>
        /// The error value (throws if IsSuccess is true)
        /// </summary>
        public TError ErrorValue
        {
            get
            {
                if (_isSuccess)
                    throw new InvalidOperationException("Cannot access ErrorValue when result is a success");
                return _errorValue!;
            }
        }

        /// <summary>
        /// Creates a success result
        /// </summary>
        public static Result<TSuccess, TError> Success(TSuccess value)
        {
            return new Result<TSuccess, TError>(true, value, default);
        }

        /// <summary>
        /// Creates an error result
        /// </summary>
        public static Result<TSuccess, TError> Error(TError error)
        {
            return new Result<TSuccess, TError>(false, default, error);
        }

        /// <summary>
        /// Pattern matches on success or error, returning a value
        /// </summary>
        public TResult Match<TResult>(
            Func<TSuccess, TResult> onSuccess,
            Func<TError, TResult> onError)
        {
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            return _isSuccess ? onSuccess(_successValue!) : onError(_errorValue!);
        }

        /// <summary>
        /// Pattern matches on success or error, executing an action
        /// </summary>
        public void Match(
            Action<TSuccess> onSuccess,
            Action<TError> onError)
        {
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            if (_isSuccess)
                onSuccess(_successValue!);
            else
                onError(_errorValue!);
        }
    }
}
