using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Rest.Azure;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
            var vaultName = sample.context.VaultName + "invault99";
            var secretName = "recoverysample";

            // retrieve the vault (or create, if it doesn't exist)
            var vault = await sample.CreateOrRetrieveVaultAsync(rgName, vaultName, enableSoftDelete: true, enablePurgeProtection: false);
            var vaultUri = vault.Properties.VaultUri;

            SecretClient SecretClient = sample.getDataClient(new Uri(vaultUri));

            Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vault.Location);

            try
            {
                // set a secret
                Console.Write("Setting a new value for secret '{0}'...", secretName);
                await SecretClient.SetSecretAsync(secretName, Guid.NewGuid().ToString());
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret creation...");
                Response<KeyVaultSecret> retrievedSecretResponse = await SecretClient.GetSecretAsync(secretName);
                Console.WriteLine("done.");

                // confirm recovery is possible
                Console.Write("Verifying the secret deletion is recoverable...");
                var recoveryLevel = retrievedSecretResponse.Value.Properties.RecoveryLevel;
                if (!recoveryLevel.ToLowerInvariant().Contains("Recoverable".ToLowerInvariant()))
                {
                    Console.WriteLine("failed; soft-delete is not enabled for this vault.");

                    return;
                }
                Console.WriteLine("done.");


                // delete secret
                Console.Write("Deleting secret...");
                DeleteSecretOperation deleteSecretOperation = await SecretClient.StartDeleteSecretAsync(secretName);

                // When deleting a secret asynchronously before you purge it, you can await the WaitForCompletionAsync method on the operation
                await deleteSecretOperation.WaitForCompletionAsync();
                Console.WriteLine("done.");

                // recover secret
                Console.Write("Recovering deleted secret...");
                RecoverDeletedSecretOperation recoverDeletedSecretOperation = await SecretClient.StartRecoverDeletedSecretAsync(secretName);
                await recoverDeletedSecretOperation.WaitForCompletionAsync();
                Console.WriteLine("done.");

                // confirm recovery
                Console.Write("Retrieving recovered secret...");
                await SecretClient.GetSecretAsync(secretName);
                Console.WriteLine("done.");

                // delete secret
                Console.Write("Deleting recorvered secret...");
                DeleteSecretOperation deleteRecoveredSecretOperation = await SecretClient.StartDeleteSecretAsync(secretName);
                await deleteRecoveredSecretOperation.WaitForCompletionAsync();
                Console.WriteLine("done.");

                // retrieve deleted secret
                Console.Write("Retrieving the deleted secret...");
                await SecretClient.GetDeletedSecretAsync(secretName);
                Console.WriteLine("done.");
            }
            catch (RequestFailedException ex)

            {
                Console.WriteLine("Unexpected KeyVault exception encountered: {0}", ex.Message);

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
            var vaultName = sample.context.VaultName + "backup99";
            var secretName = "backupsample";

            // retrieve the vault (or create, if it doesn't exist)
            var vault = await sample.CreateOrRetrieveVaultAsync(rgName, vaultName, enableSoftDelete: false, enablePurgeProtection: false);
            var vaultUri = vault.Properties.VaultUri;

            SecretClient SecretClient = sample.getDataClient(new Uri(vaultUri));

            Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vault.Location);

            try
            {
                // set a secret
                Console.Write("Setting a new value for secret '{0}'...", secretName);
                await SecretClient.SetSecretAsync(secretName, Guid.NewGuid().ToString());
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret creation...");
                await SecretClient.GetSecretAsync(secretName);
                Console.WriteLine("done.");

                // backup secret
                Console.Write("Backing up secret...");
                Response<byte[]> backupResponse = await SecretClient.BackupSecretAsync(secretName);
                Console.WriteLine("done.");

                // delete secret
                Console.Write("Deleting secret...");
                DeleteSecretOperation deleteSecretOperation = await SecretClient.StartDeleteSecretAsync(secretName);
                // When deleting a secret asynchronously before you purge it, you can await the WaitForCompletionAsync method on the operation
                await deleteSecretOperation.WaitForCompletionAsync();
                Console.WriteLine("done.");

                // restore secret
                Console.Write("Restoring secret from backup...");
                await SecretClient.RestoreSecretBackupAsync(backupResponse.Value);
                Console.WriteLine("done.");

                // confirm existence
                Console.Write("Verifying secret restoration...");
                await SecretClient.GetSecretAsync(secretName);
                Console.WriteLine("done.");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Unexpected KeyVault  exception encountered: {0}", ex.Message);

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
