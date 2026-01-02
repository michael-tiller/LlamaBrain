using System;

namespace LlamaBrain.Core.FunctionCalling
{
    /// <summary>
    /// The result of executing a function call.
    /// Contains the return value and any error information.
    /// </summary>
    [Serializable]
    public class FunctionCallResult
    {
        /// <summary>
        /// Whether the function call executed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result value from the function (typically JSON-serializable).
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Error message if the function call failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The call ID that this result corresponds to (if provided in the original call).
        /// </summary>
        public string? CallId { get; set; }

        /// <summary>
        /// Creates a successful function call result.
        /// </summary>
        /// <param name="result">The result value from the function</param>
        /// <param name="callId">Optional call ID</param>
        /// <returns>A successful FunctionCallResult</returns>
        public static FunctionCallResult SuccessResult(object? result, string? callId = null)
        {
            return new FunctionCallResult
            {
                Success = true,
                Result = result,
                CallId = callId
            };
        }

        /// <summary>
        /// Creates a failed function call result.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure</param>
        /// <param name="callId">Optional call ID</param>
        /// <returns>A failed FunctionCallResult</returns>
        public static FunctionCallResult FailureResult(string errorMessage, string? callId = null)
        {
            return new FunctionCallResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                CallId = callId
            };
        }

        /// <summary>
        /// Returns a string representation of this function call result.
        /// </summary>
        /// <returns>A string representation of the result</returns>
        public override string ToString()
        {
            if (Success)
            {
                return $"FunctionCallResult[Success]{(CallId != null ? $" (ID: {CallId})" : "")}";
            }
            return $"FunctionCallResult[Failed] {ErrorMessage}{(CallId != null ? $" (ID: {CallId})" : "")}";
        }
    }
}
