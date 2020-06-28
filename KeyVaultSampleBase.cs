using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Base class for KeyVault recovery samples.
    /// </summary>
    public class KeyVaultSampleBase
    {
        /// <summary>
        /// Represents the client context - Azure tenant, subscription, identity etc.
        /// </summary>
        protected ClientContext context;

        /// <summary>
        /// KeyVault management (Control Plane) client instance.
        /// </summary>
        public KeyVaultManagementClient ManagementClient { get; private set; }

        /// <summary>
        /// KeyVault secret (Data Plane) client instance.
        /// </summary>
        protected SecretClient SecretClient { get; set; }

        /// <summary>
        /// Use client secret credential to authenticating the secret client.
        /// </summary>
        protected ClientSecretCredential ClientSecretCredential { get; set; }

        /// <summary>
        /// Builds a sample object from the specified parameters.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="clientSecret">Representing the vault secret.</param>
        /// <param name="clientId">AAD application id.</param>
        /// <param name="objectId">AAD object id.</param>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="vaultLocation">Vault location.</param>
        /// <param name="vaultName">Vault name.</param>
        /// <param name="azureEnvironment">Azure authority hosts.</param>
        public KeyVaultSampleBase(string tenantId, string clientSecret, string clientId, string objectId, string subscriptionId, string resourceGroupName, string vaultLocation, string vaultName, string azureEnvironment)
        {
            InstantiateSample(tenantId, clientSecret, clientId, objectId, subscriptionId, resourceGroupName, vaultLocation, vaultName, azureEnvironment);
        }

        /// <summary>
        /// Builds a sample object from configuration.
        /// </summary>
        public KeyVaultSampleBase()
        {
            // retrieve parameters from configuration
            var tenantId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.TenantId];
            var objectId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.SPObjectId];
            var clientSecret = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.SPSecret];
            var clientId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.ApplicationId];
            var subscriptionId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.SubscriptionId];
            var resourceGroupName = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.ResourceGroupName];
            var vaultLocation = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultLocation];
            var vaultName = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultName];
            var azureEnvironment = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.AzureEnvironment];

            InstantiateSample(tenantId, clientSecret, clientId, objectId, subscriptionId, resourceGroupName, vaultLocation, vaultName, azureEnvironment);
        }

        private void InstantiateSample(string tenantId, string clientSecret, string clientId, string objectId, string subscriptionId, string resourceGroupName, string vaultLocation, string vaultName, string azureEnvironment)
        {           
            context = ClientContext.Build(tenantId, clientSecret, clientId, objectId, subscriptionId, resourceGroupName, vaultLocation, vaultName);

            var environment = context.Environment(azureEnvironment);
            // log in with as the specified service principal
            var credential = Task.Run(() => context.GetAzureCredentialsAsync(clientId, clientSecret, tenantId, environment)).ConfigureAwait(false).GetAwaiter().GetResult();
            
            var restClient = RestClient.Configure()
                .WithEnvironment(credential.Environment)
                .WithCredentials(credential)
                .Build();

            ManagementClient = new KeyVaultManagementClient(restClient);
            ManagementClient.SubscriptionId = subscriptionId;

            //Get current Authority Host for Azure
            TokenCredentialOptions options = new TokenCredentialOptions();
            options.AuthorityHost = new Uri(environment.AuthenticationEndpoint);

            ClientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
        }

        #region utilities
        /// <summary>
        /// Creates a vault with the specified parameters and coordinates.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="vaultName"></param>
        /// <param name="vaultLocation"></param>
        /// <param name="enableSoftDelete"></param>
        /// <param name="enablePurgeProtection"></param>
        /// <returns></returns>
        protected VaultCreateOrUpdateParameters CreateVaultParameters(string resourceGroupName, string vaultName, string vaultLocation, bool enableSoftDelete, bool enablePurgeProtection)
        {
            var properties = new VaultProperties
            {
                TenantId = Guid.Parse(context.TenantId),
                Sku = new Sku(),
                AccessPolicies = new List<AccessPolicyEntry>(),
                EnabledForDeployment = false,
                EnabledForDiskEncryption = false,
                EnabledForTemplateDeployment = false,
                EnableSoftDelete = enableSoftDelete ? (bool?)enableSoftDelete : null,
                CreateMode = CreateMode.Default
            };



            // add an access control entry for the test SP
            properties.AccessPolicies.Add(new AccessPolicyEntry
            {
                TenantId = properties.TenantId,
                ObjectId = context.ObjectId,
                Permissions = new Permissions
                {
                    Secrets = new SecretPermissions[] { SecretPermissions.Get, SecretPermissions.Set,SecretPermissions.List,
                        SecretPermissions.Delete,SecretPermissions.Recover,SecretPermissions.Backup,SecretPermissions.Restore,SecretPermissions.Purge}
                }
            });

            return new VaultCreateOrUpdateParameters(vaultLocation, properties);
        }

        /// <summary>
        /// Enables soft delete on a pre-existing vault.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        public async Task EnableRecoveryOptionsOnExistingVaultAsync(string resourceGroupName, string vaultName, bool enablePurgeProtection)
        {
            var vault = await ManagementClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);

            // First check if there is anything to do. The recovery levels are as follows:
            // - no protection: soft delete = false
            // - recoverable deletion: soft delete = true, purge protection = false
            // - recoverable deletion, purge protected: soft delete = true, purge protection = true
            //
            // The protection level can be strengthened, but never weakened; we will throw on an attempt to lower it.
            // 
            if (vault.Properties.EnableSoftDelete.HasValue
                && vault.Properties.EnableSoftDelete.Value)
            {
                Console.WriteLine("The required recovery protection level is already enabled on vault {0}.", vaultName);

                return;
            }

            vault.Properties.EnableSoftDelete = true;

            // prepare the update operation on the vault
            var updateParameters = new VaultCreateOrUpdateParameters
            {
                Location = vault.Location,
                Properties = vault.Properties,
                Tags = vault.Tags
            };

            try
            {
                vault = await ManagementClient.Vaults.CreateOrUpdateAsync(resourceGroupName, vaultName, updateParameters).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to update vault {0} in resource group {1}: {2}", vaultName, resourceGroupName, e.Message);
            }
        }

        /// <summary>
        /// Verifies the specified exception is a CloudException, and its status code matches the expected value.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="expectedStatusCode"></param>
        protected static void VerifyExpectedARMException(Exception e, HttpStatusCode expectedStatusCode)
        {
            // verify that the exception is a CloudError one
            var armException = e as Microsoft.Rest.Azure.CloudException;
            if (armException == null)
            {
                Console.WriteLine("Unexpected exception encountered running sample: {0}", e.Message);
                throw e;
            }

            // verify that the exception has the expected status code
            if (armException.Response.StatusCode != expectedStatusCode)
            {
                Console.WriteLine("Encountered unexpected ARM exception; expected status code: {0}, actual: {1}", armException.Response.StatusCode, expectedStatusCode);
                throw e;
            }
        }

        /// <summary>
        /// Retries the specified function, representing an http request, according to the specified policy.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="functionName"></param>
        /// <param name="initialBackoff"></param>
        /// <param name="numAttempts"></param>
        /// <param name="continueOn"></param>
        /// <param name="retryOn"></param>
        /// <param name="abortOn"></param>
        /// <returns></returns>
        public async static Task<HttpOperationResponse> RetryHttpRequestAsync(
            Func<Task<HttpOperationResponse>> function,
            string functionName,
            int initialBackoff,
            int numAttempts,
            HashSet<HttpStatusCode> continueOn,
            HashSet<HttpStatusCode> retryOn,
            HashSet<HttpStatusCode> abortOn = null)
        {
            HttpOperationResponse response = null;

            for (int idx = 0, backoff = initialBackoff; idx < numAttempts; idx++, backoff <<= 1)
            {
                try
                {
                    response = await function().ConfigureAwait(false);

                    break;
                }
                catch (Microsoft.Azure.KeyVault.Models.KeyVaultErrorException kvee)
                {
                    var statusCode = kvee.Response.StatusCode;

                    Console.Write("attempt #{0} to {1} returned: {2};", idx, functionName, statusCode);
                    if (continueOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is expected, continuing..", statusCode);
                        break;
                    }
                    else if (retryOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is retriable, retrying after {1}s..", statusCode, backoff);
                        Thread.Sleep(TimeSpan.FromSeconds(backoff));

                        continue;
                    }
                    else if (abortOn != null && abortOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is designated 'abort', terminating..", statusCode);

                        string message = String.Format("status code {0} is designated as 'abort'; terminating request", statusCode);
                        throw new InvalidOperationException(message);
                    }
                    else
                    {
                        Console.WriteLine("handling of {0} is unspecified; retrying after {1}s..", statusCode, backoff);
                        Thread.Sleep(TimeSpan.FromSeconds(backoff));
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Retries the specified function according to the specified retry policy.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="functionName"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public static Task<HttpOperationResponse> RetryHttpRequestAsync(
            Func<Task<HttpOperationResponse>> function,
            string functionName,
            RetryPolicy policy)
        {
            if (policy != null)
                return RetryHttpRequestAsync(function, functionName, policy.InitialBackoff, policy.MaxAttempts, policy.ContinueOn, policy.RetryOn, policy.AbortOn);
            else
                return function();
        }
        #endregion
    }
}
