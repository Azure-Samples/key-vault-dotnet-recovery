using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Rest.Azure;

namespace AzureKeyVaultRecoverySamples
{
    public sealed class KeyVaultEntityRecoverySamples : KeyVaultSampleBase
    {
        /// <summary>
        /// Builds a vault recovery sample object with the specified parameters.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="objectId">Object id of the Service Principal used to run the sample.</param>
        /// <param name="appId">AD application id.</param>
        /// <param name="appCredX5T">Thumbprint of the certificate set as the credential for the AD application.</param>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="vaultLocation">Location of the vault.</param>
        /// <param name="vaultName">Vault name.</param>
        public KeyVaultEntityRecoverySamples(string tenantId, string objectId, string appId, string appCredX5T, string subscriptionId, string resourceGroupName, string vaultLocation, string vaultName)
            : base(tenantId, objectId, appId, appCredX5T, subscriptionId, resourceGroupName, vaultLocation, vaultName)
        { }

        /// <summary>
        /// Builds a vault recovery sample object from configuration.
        /// </summary>
        public KeyVaultEntityRecoverySamples()
            : base()
        { }

        #region samples
        /// <summary>
        /// Demonstrates how to enable soft delete on an existing vault, and then proceeds to delete, recover and purge the vault.
        /// Assumes the caller has the KeyVaultContributor role in the subscription.
        /// </summary>
        /// <returns>Task representing this functionality.</returns>
        public static async Task DemonstrateRecoveryAndPurgeAsync()
        {
            // instantiate the samples object
            var sample = new KeyVaultEntityRecoverySamples();

            var rgName = sample.context.ResourceGroupName;

            // derive a unique vault name for this sample
            var vaultName = sample.context.VaultName + "invault";
            var secretName = "recoverysample";

            // retrieve the vault (or create, if it doesn't exist)
            var vault = await sample.CreateOrRetrieveVaultAsync(rgName, vaultName, enableSoftDelete: true, enablePurgeProtection: false);
            var vaultUri = vault.Properties.VaultUri;
            Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vault.Location);

            try
            {
                // set a secret
                Console.Write("Setting a new value for secret '{0}'...", secretName);
                var secretResponse = await sample.DataClient.SetSecretWithHttpMessagesAsync(vaultUri, secretName, Guid.NewGuid().ToString()).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret creation...");
                var retrievedSecretResponse = await sample.DataClient.GetSecretWithHttpMessagesAsync(vaultUri, secretName, secretVersion: String.Empty).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm recovery is possible
                Console.Write("Verifying the secret deletion is recoverable...");
                var recoveryLevel = retrievedSecretResponse.Body.Attributes.RecoveryLevel;
                if (!recoveryLevel.ToLowerInvariant().Contains("Recoverable".ToLowerInvariant()))
                {
                    Console.WriteLine("failed; soft-delete is not enabled for this vault.");

                    return;
                }
                Console.WriteLine("done.");

                // delete secret
                Console.Write("Deleting secret...");
                await sample.DataClient.DeleteSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // retrieve deleted secret; recoverable deletion is an asynchronous operation, during which the secret
                // is not accessible, either as an active entity or a deleted one. Polling for up to 45s should be sufficient.
                Console.Write("Retrieving the deleted secret...");
                AzureOperationResponse<DeletedSecretBundle> deletedSecretResponse = null;
                await RetryHttpRequestAsync(
                    async () => { return deletedSecretResponse = await sample.DataClient.GetDeletedSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false); }, 
                    "get deleted secret", 
                    SampleConstants.RetryPolicies.DefaultSoftDeleteRetryPolicy)
                    .ConfigureAwait(false);
                Console.WriteLine("done.");

                // recover secret
                Console.Write("Recovering deleted secret...");
                var recoveredSecretResponse = await sample.DataClient.RecoverDeletedSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm recovery
                Console.Write("Retrieving recovered secret...");
                await RetryHttpRequestAsync(
                    async () => { return retrievedSecretResponse = await sample.DataClient.GetSecretWithHttpMessagesAsync(vaultUri, secretName, secretVersion: String.Empty).ConfigureAwait(false); },
                    "recover deleted secret",
                    SampleConstants.RetryPolicies.DefaultSoftDeleteRetryPolicy)
                    .ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete secret
                Console.Write("Deleting secret (pass #2)...");
                await sample.DataClient.DeleteSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // retrieve deleted secret
                Console.Write("Retrieving the deleted secret (pass #2)...");
                await RetryHttpRequestAsync(
                    async () => { return deletedSecretResponse = await sample.DataClient.GetDeletedSecretWithHttpMessagesAsync(vaultUri, secretName); },
                    "get deleted secret",
                    SampleConstants.RetryPolicies.DefaultSoftDeleteRetryPolicy)
                    .ConfigureAwait(false);
                Console.WriteLine("done.");

                // purge secret
                Console.Write("Purging deleted secret...");
                await sample.DataClient.PurgeDeletedSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (KeyVaultErrorException kvee)
            {
                Console.WriteLine("Unexpected KeyVault exception encountered: {0}", kvee.Message);

                throw;
            }
            catch (CloudException ce)
            {
                Console.WriteLine("Unexpected ARM exception encountered: {0}", ce.Message);

                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception encountered: {0}", e.Message);

                throw;
            }
        }

        /// <summary>
        /// Demonstrates how to back up and restore a secret.
        /// </summary>
        /// <returns>Task representing this functionality.</returns>
        public static async Task DemonstrateBackupAndRestoreAsync()
        {
            // instantiate the samples object
            var sample = new KeyVaultEntityRecoverySamples();

            var rgName = sample.context.ResourceGroupName;

            // derive a unique vault name for this sample
            var vaultName = sample.context.VaultName + "backuprestore";
            var secretName = "backupsample";

            // retrieve the vault (or create, if it doesn't exist)
            var vault = await sample.CreateOrRetrieveVaultAsync(rgName, vaultName, enableSoftDelete: false, enablePurgeProtection: false);
            var vaultUri = vault.Properties.VaultUri;
            Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vault.Location);

            try
            {
                // set a secret
                Console.Write("Setting a new value for secret '{0}'...", secretName);
                var secretResponse = await sample.DataClient.SetSecretWithHttpMessagesAsync(vaultUri, secretName, Guid.NewGuid().ToString()).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret creation...");
                var retrievedSecretResponse = await sample.DataClient.GetSecretWithHttpMessagesAsync(vaultUri, secretName, secretVersion: String.Empty).ConfigureAwait(false);
                Console.WriteLine("done.");

                // backup secret
                Console.Write("Backing up secret...");
                var backupResponse = await sample.DataClient.BackupSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete secret
                Console.Write("Deleting secret...");
                await sample.DataClient.DeleteSecretWithHttpMessagesAsync(vaultUri, secretName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // restore secret
                Console.Write("Restoring secret from backup...");
                byte[] secretBackup = backupResponse.Body.Value;
                var restoreResponse = await sample.DataClient.RestoreSecretWithHttpMessagesAsync(vaultUri, secretBackup).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret restoration...");
                retrievedSecretResponse = await sample.DataClient.GetSecretWithHttpMessagesAsync(vaultUri, secretName, secretVersion: String.Empty).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (KeyVaultErrorException kvee)
            {
                Console.WriteLine("Unexpected KeyVault exception encountered: {0}", kvee.Message);

                throw;
            }
            catch (CloudException ce)
            {
                Console.WriteLine("Unexpected ARM exception encountered: {0}", ce.Message);

                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception encountered: {0}", e.Message);

                throw;
            }
        }
        #endregion

        #region helpers
        private async Task<VaultInner> CreateOrRetrieveVaultAsync(string resourceGroupName, string vaultName, bool enableSoftDelete, bool enablePurgeProtection)
        {
            VaultInner vault = null;

            try
            {
                // check whether the vault exists
                Console.Write("Checking the existence of the vault...");
                vault = await ManagementClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (CloudException ce)
            {
                if (ce.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Unexpected exception encountered retrieving the vault: {0}", ce.Message);
                    throw;
                }

                // create a new vault
                var vaultParameters = CreateVaultParameters(resourceGroupName, vaultName, context.PreferredLocation, enableSoftDelete, enablePurgeProtection);

                // create new soft-delete-enabled vault
                Console.Write("Vault does not exist; creating...");
                vault = await ManagementClient.Vaults.CreateOrUpdateAsync(resourceGroupName, vaultName, vaultParameters).ConfigureAwait(false);
                Console.WriteLine("done.");

                // wait for the DNS record to propagate; verify properties
                Console.Write("Waiting for DNS propagation..");
                Thread.Sleep(10 * 1000);
                Console.WriteLine("done.");

                Console.Write("Retrieving newly created vault...");
                vault = await ManagementClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");
            }

            return vault;
        }
        #endregion
    }
}
