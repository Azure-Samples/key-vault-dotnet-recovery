using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Contains samples illustrating enabling recoverable deletion for Azure key vaults,
    /// as well as exercising the recovery and purge functionality, respectively.
    /// </summary>
    public sealed class KeyVaultRecoverySamples : KeyVaultSampleBase
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
        public KeyVaultRecoverySamples(string tenantId, string objectId, string appId, string appCredX5T, string subscriptionId, string resourceGroupName, string vaultLocation, string vaultName)
            : base(tenantId, objectId, appId, appCredX5T, subscriptionId, resourceGroupName, vaultLocation, vaultName)
        { }

        /// <summary>
        /// Builds a vault recovery sample object from configuration.
        /// </summary>
        public KeyVaultRecoverySamples()
            : base()
        { }

        #region samples
        /// <summary>
        /// Demonstrates how to enable soft delete on an existing vault, and then proceeds to delete, recover and purge the vault.
        /// Assumes the caller has the KeyVaultContributor role in the subscription.
        /// </summary>
        /// <returns>Task representing this functionality.</returns>
        public static async Task DemonstrateRecoveryAndPurgeForNewVaultAsync()
        {
            // instantiate the samples object
            var sample = new KeyVaultRecoverySamples();

            var rgName = sample.context.ResourceGroupName;

            // derive a unique vault name for this sample
            var vaultName = sample.context.VaultName + "new";

            DeletedVaultInner deletedVault = null;

            try
            {
                var vaultParameters = sample.CreateVaultParameters(rgName, vaultName, sample.context.PreferredLocation, enableSoftDelete: true, enablePurgeProtection: false);
                Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vaultParameters.Location);

                // create new soft-delete-enabled vault
                Console.Write("Creating vault...");
                var vault = await sample.ManagementClient.Vaults.CreateOrUpdateAsync(rgName, vaultName, vaultParameters).ConfigureAwait(false);
                Console.WriteLine("done.");

                // wait for the DNS record to propagate; verify properties
                Console.Write("Waiting for DNS propagation..");
                Thread.Sleep(10 * 1000);
                Console.WriteLine("done.");

                Console.Write("Retrieving newly created vault...");
                var retrievedVault = await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete vault
                Console.Write("Deleting vault...");
                await sample.ManagementClient.Vaults.DeleteAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm the existence of the deleted vault
                Console.Write("Retrieving deleted vault...");
                deletedVault = await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, retrievedVault.Location).ConfigureAwait(false);
                Console.WriteLine("done; '{0}' deleted on: {1}, scheduled for purge on: {2}", deletedVault.Id, deletedVault.Properties.DeletionDate, deletedVault.Properties.ScheduledPurgeDate);

                // recover; set the creation mode as 'recovery' in the vault parameters
                Console.Write("Recovering deleted vault...");
                vaultParameters.Properties.CreateMode = CreateMode.Recover;
                await sample.ManagementClient.Vaults.CreateOrUpdateAsync(rgName, vaultName, vaultParameters).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm recovery
                Console.Write("Verifying the existence of recovered vault...");
                var recoveredVault = await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete vault
                Console.Write("Deleting vault...");
                await sample.ManagementClient.Vaults.DeleteAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // purge vault
                Console.Write("Purging vault...");
                deletedVault = await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, recoveredVault.Location).ConfigureAwait(false);
                await sample.ManagementClient.Vaults.PurgeDeletedAsync(vaultName, recoveredVault.Location).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (Exception e)
            {
                Console.WriteLine("unexpected exception encountered running the test: {message}", e.Message);
                throw;
            }

            // verify purge
            try
            {
                Console.Write("Verifying vault deletion succeeded...");
                await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName);
            }
            catch (Exception e)
            {
                // no op; expected
                VerifyExpectedARMException(e, HttpStatusCode.NotFound);
                Console.WriteLine("done.");
            }

            try
            {
                Console.Write("Verifying vault purging succeeded...");
                await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, deletedVault.Properties.Location).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // no op; expected
                VerifyExpectedARMException(e, HttpStatusCode.NotFound);
                Console.WriteLine("done.");
            }
        }

        /// <summary>
        /// Demonstrates how to enable soft delete on an existing vault, and then proceeds to delete, recover and purge the vault.
        /// Assumes the caller has the KeyVaultContributor role in the subscription.
        /// </summary>
        /// <returns>Task representing this functionality.</returns>
        public static async Task DemonstrateRecoveryAndPurgeForExistingVaultAsync()
        {
            // instantiate the samples object
            var sample = new KeyVaultRecoverySamples();

            var rgName = sample.context.ResourceGroupName;

            // derive a unique vault name for this sample
            var vaultName = sample.context.VaultName + "existing";

            DeletedVaultInner deletedVault = null;

            try
            {
                var vaultParameters = sample.CreateVaultParameters(rgName, vaultName, sample.context.PreferredLocation, enableSoftDelete: false, enablePurgeProtection: false);
                Console.WriteLine("Operating with vault name '{0}' in resource group '{1}' and location '{2}'", vaultName, rgName, vaultParameters.Location);

                // create new vault, not enabled for soft delete
                Console.Write("Creating vault...");
                var vault = await sample.ManagementClient.Vaults.CreateOrUpdateAsync(rgName, vaultName, vaultParameters).ConfigureAwait(false);
                Console.WriteLine("done.");

                // wait for the DNS record to propagate; verify properties
                Console.Write("Waiting for DNS propagation..");
                Thread.Sleep(10 * 1000);
                Console.WriteLine("done.");

                Console.Write("Retrieving newly created vault...");
                var retrievedVault = await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // enable soft delete on existing vault
                Console.Write("Enabling soft delete on existing vault...");
                await sample.EnableRecoveryOptionsOnExistingVaultAsync(rgName, vaultName, enablePurgeProtection: false).ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete vault
                Console.Write("Deleting vault...");
                await sample.ManagementClient.Vaults.DeleteAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm the existence of the deleted vault
                Console.Write("Retrieving deleted vault...");
                deletedVault = await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, retrievedVault.Location).ConfigureAwait(false);
                Console.WriteLine("done; '{0}' deleted on: {1}, scheduled for purge on: {2}", deletedVault.Id, deletedVault.Properties.DeletionDate, deletedVault.Properties.ScheduledPurgeDate);

                // recover; set the creation mode as 'recovery' in the vault parameters
                Console.Write("Recovering deleted vault...");
                vaultParameters.Properties.CreateMode = CreateMode.Recover;
                await sample.ManagementClient.Vaults.CreateOrUpdateAsync(rgName, vaultName, vaultParameters).ConfigureAwait(false);
                Console.WriteLine("done.");

                // confirm recovery
                Console.Write("Verifying the existence of recovered vault...");
                var recoveredVault = await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // delete vault
                Console.Write("Deleting vault...");
                await sample.ManagementClient.Vaults.DeleteAsync(rgName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");

                // purge vault
                Console.Write("Purging vault...");
                deletedVault = await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, recoveredVault.Location).ConfigureAwait(false);
                await sample.ManagementClient.Vaults.PurgeDeletedAsync(vaultName, recoveredVault.Location).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (Exception e)
            {
                Console.WriteLine("unexpected exception encountered running the test: {0}", e.Message);
                throw;
            }

            // verify purge
            try
            {
                Console.Write("Verifying vault deletion succeeded...");
                await sample.ManagementClient.Vaults.GetAsync(rgName, vaultName);
            }
            catch (Exception e)
            {
                // no op; expected
                VerifyExpectedARMException(e, HttpStatusCode.NotFound);
                Console.WriteLine("done.");
            }

            try
            {
                Console.Write("Verifying vault purging succeeded...");
                await sample.ManagementClient.Vaults.GetDeletedAsync(vaultName, deletedVault.Properties.Location).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // no op; expected
                VerifyExpectedARMException(e, HttpStatusCode.NotFound);
                Console.WriteLine("done.");
            }
        }
        #endregion
    }
}
