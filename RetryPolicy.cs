using System.Collections.Generic;
using System.Net;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Models a policy for retrying an http request, based on expected response status codes.
    /// </summary>
    public sealed class RetryPolicy
    {
        public RetryPolicy(int initialBackoff, int numAttempts, HashSet<HttpStatusCode> continueOn, HashSet<HttpStatusCode> retryOn, HashSet<HttpStatusCode> abortOn = null)
        {
            InitialBackoff = initialBackoff;
            MaxAttempts = numAttempts;
            ContinueOn = continueOn;
            RetryOn = retryOn;
            AbortOn = abortOn;
        }

        public int InitialBackoff { get; set; }

        public int MaxAttempts { get; set; }

        public HashSet<HttpStatusCode> ContinueOn { get; set; }

        public HashSet<HttpStatusCode> RetryOn { get; set; }

        public HashSet<HttpStatusCode> AbortOn { get; set; }
    }
}
