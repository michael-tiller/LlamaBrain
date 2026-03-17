using System;
using System.Threading;

namespace LlamaBrain.Core.StructuredOutput
{
    /// <summary>
    /// Metrics for tracking structured output pipeline performance.
    /// Used to monitor success rates and fallback usage.
    /// </summary>
    [Serializable]
    public sealed class StructuredPipelineMetrics
    {
        private int _totalRequests;
        private int _structuredSuccessCount;
        private int _structuredFailureCount;
        private int _regexFallbackCount;
        private int _regexDirectCount;
        private int _validationFailureCount;
        private int _totalRetries;
        private int _mutationsExecuted;
        private int _intentsEmitted;
        private int _cacheHitCount;
        private int _cacheMissCount;

        /// <summary>
        /// Total number of pipeline requests processed.
        /// </summary>
        public int TotalRequests => _totalRequests;

        /// <summary>
        /// Number of requests that used structured output successfully.
        /// </summary>
        public int StructuredSuccessCount => _structuredSuccessCount;

        /// <summary>
        /// Number of requests where structured output failed.
        /// </summary>
        public int StructuredFailureCount => _structuredFailureCount;

        /// <summary>
        /// Number of requests that fell back to regex parsing.
        /// </summary>
        public int RegexFallbackCount => _regexFallbackCount;

        /// <summary>
        /// Number of requests that used regex parsing from the start.
        /// </summary>
        public int RegexDirectCount => _regexDirectCount;

        /// <summary>
        /// Number of validation failures.
        /// </summary>
        public int ValidationFailureCount => _validationFailureCount;

        /// <summary>
        /// Number of retries across all requests.
        /// </summary>
        public int TotalRetries => _totalRetries;

        /// <summary>
        /// Number of mutations successfully executed.
        /// </summary>
        public int MutationsExecuted => _mutationsExecuted;

        /// <summary>
        /// Number of intents successfully emitted.
        /// </summary>
        public int IntentsEmitted => _intentsEmitted;

        /// <summary>
        /// Number of requests where KV cache was hit.
        /// </summary>
        public int CacheHitCount => _cacheHitCount;

        /// <summary>
        /// Number of requests where KV cache was missed.
        /// </summary>
        public int CacheMissCount => _cacheMissCount;

        /// <summary>
        /// KV cache hit rate as a percentage (0-100).
        /// </summary>
        public float CacheHitRate
        {
            get
            {
                var cacheAttempts = CacheHitCount + CacheMissCount;
                return cacheAttempts > 0
                    ? (float)CacheHitCount / cacheAttempts * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Structured output success rate as a percentage (0-100).
        /// </summary>
        public float StructuredSuccessRate
        {
            get
            {
                var structuredAttempts = StructuredSuccessCount + StructuredFailureCount;
                return structuredAttempts > 0
                    ? (float)StructuredSuccessCount / structuredAttempts * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Overall pipeline success rate as a percentage (0-100).
        /// </summary>
        public float OverallSuccessRate
        {
            get
            {
                var successCount = StructuredSuccessCount + RegexFallbackCount + RegexDirectCount;
                return TotalRequests > 0
                    ? (float)successCount / TotalRequests * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Fallback rate as a percentage of structured attempts (0-100).
        /// </summary>
        public float FallbackRate
        {
            get
            {
                var structuredAttempts = StructuredSuccessCount + StructuredFailureCount;
                return structuredAttempts > 0
                    ? (float)RegexFallbackCount / structuredAttempts * 100f
                    : 0f;
            }
        }

        /// <summary>
        /// Records a successful structured output request.
        /// </summary>
        public void RecordStructuredSuccess()
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _structuredSuccessCount);
        }

        /// <summary>
        /// Records a failed structured output request.
        /// </summary>
        public void RecordStructuredFailure()
        {
            Interlocked.Increment(ref _structuredFailureCount);
        }

        /// <summary>
        /// Records a fallback to regex parsing.
        /// </summary>
        public void RecordFallbackToRegex()
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _regexFallbackCount);
        }

        /// <summary>
        /// Records a direct regex parsing request (no structured output attempted).
        /// </summary>
        public void RecordRegexDirect()
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _regexDirectCount);
        }

        /// <summary>
        /// Records a validation failure.
        /// </summary>
        public void RecordValidationFailure()
        {
            Interlocked.Increment(ref _validationFailureCount);
        }

        /// <summary>
        /// Records a retry attempt.
        /// </summary>
        public void RecordRetry()
        {
            Interlocked.Increment(ref _totalRetries);
        }

        /// <summary>
        /// Records successful mutation execution.
        /// </summary>
        /// <param name="count">Number of mutations executed.</param>
        public void RecordMutationsExecuted(int count)
        {
            Interlocked.Add(ref _mutationsExecuted, count);
        }

        /// <summary>
        /// Records successful intent emission.
        /// </summary>
        /// <param name="count">Number of intents emitted.</param>
        public void RecordIntentsEmitted(int count)
        {
            Interlocked.Add(ref _intentsEmitted, count);
        }

        /// <summary>
        /// Records a KV cache hit.
        /// </summary>
        public void RecordCacheHit()
        {
            Interlocked.Increment(ref _cacheHitCount);
        }

        /// <summary>
        /// Records a KV cache miss.
        /// </summary>
        public void RecordCacheMiss()
        {
            Interlocked.Increment(ref _cacheMissCount);
        }

        /// <summary>
        /// Records a cache result based on cached token count.
        /// </summary>
        /// <param name="cachedTokenCount">Number of cached tokens (0 = miss, > 0 = hit)</param>
        public void RecordCacheResult(int cachedTokenCount)
        {
            if (cachedTokenCount > 0)
            {
                RecordCacheHit();
            }
            else
            {
                RecordCacheMiss();
            }
        }

        /// <summary>
        /// Resets all metrics to zero.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _structuredSuccessCount, 0);
            Interlocked.Exchange(ref _structuredFailureCount, 0);
            Interlocked.Exchange(ref _regexFallbackCount, 0);
            Interlocked.Exchange(ref _regexDirectCount, 0);
            Interlocked.Exchange(ref _validationFailureCount, 0);
            Interlocked.Exchange(ref _totalRetries, 0);
            Interlocked.Exchange(ref _mutationsExecuted, 0);
            Interlocked.Exchange(ref _intentsEmitted, 0);
            Interlocked.Exchange(ref _cacheHitCount, 0);
            Interlocked.Exchange(ref _cacheMissCount, 0);
        }

        /// <inheritdoc/>
        /// <returns>A string representation of the pipeline metrics including request counts and success rates.</returns>
        public override string ToString()
        {
            return $"PipelineMetrics: {TotalRequests} requests, " +
                   $"Structured: {StructuredSuccessRate:F1}% success, " +
                   $"Fallback: {FallbackRate:F1}%, " +
                   $"KvCache: {CacheHitRate:F1}% hit, " +
                   $"Mutations: {MutationsExecuted}, Intents: {IntentsEmitted}";
        }
    }
}
