﻿using System.Collections.Generic;
using System.Net;

namespace AzureKeyVaultRecoverySamples
{
    public static class SampleConstants
    {
        public static class ConfigKeys
        {
            public static readonly string TenantId = "TenantId";
            public static readonly string VaultName = "VaultName";
            public static readonly string VaultLocation = "VaultLocation";
            public static readonly string ResourceGroupName = "ResourceGroupName";
            public static readonly string SubscriptionId = "SubscriptionId";
            public static readonly string SPObjectId = "SPObjectId";
            public static readonly string SPCredentialCertificateThumbprint = "SPCredentialCertificateThumbprint";
            public static readonly string ApplicationId = "ApplicationId";
        }

        public static class RetryPolicies
        {
            /// <summary>
            /// status codes for retriable operations.
            /// </summary>
            public static HashSet<HttpStatusCode> SuccessStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent });

            public static HashSet<HttpStatusCode> SoftDeleteRetriableStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.Conflict, HttpStatusCode.NotFound });

            public static HashSet<HttpStatusCode> AbortStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.Forbidden });


            /// <summary>
            /// Number of seconds to wait after a first, failed attempt to execute a soft-delete-related operation (such as delete, recover, purge).
            /// </summary>
            private static int SoftDeleteInitialBackoff = 15;
            private static int SoftDeleteMaxAttempts = 3;

            /// <summary>
            /// Standard retry policy for soft-delete-related operations which attempt to modify transitioning entities.
            /// </summary>
            public static RetryPolicy DefaultSoftDeleteRetryPolicy = new RetryPolicy(
                SoftDeleteInitialBackoff,
                SoftDeleteMaxAttempts,
                SuccessStatusCodes,
                SoftDeleteRetriableStatusCodes,
                AbortStatusCodes);

            /// <summary>
            /// Standard retry policy for soft-delete-related operations which attempt to consume the outcome of an async operation.
            /// </summary>
            public static RetryPolicy WaitForAsyncDeletionRetryPolicy = new RetryPolicy(
                SoftDeleteInitialBackoff,
                SoftDeleteMaxAttempts,
                continueOn: new HashSet<HttpStatusCode> { HttpStatusCode.NotFound },                                            // keep spinning until entity is marked as deleted
                retryOn: new HashSet<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Conflict },   // retry on success and conflicts
                abortOn: AbortStatusCodes);
        }
    }
}
