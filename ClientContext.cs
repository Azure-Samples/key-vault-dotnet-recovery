using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Represents the Azure context of the client running the samples - tenant, subscription, client id and credentials.
    /// </summary>
    public sealed class ClientContext
    {
        #region construction
        public static ClientContext Build(string tenantId, string clientSecret, string clientId, string objectId, string subscriptionId, string resourceGroupName, string location, string vaultName)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException(nameof(clientSecret));
            if (String.IsNullOrWhiteSpace(clientId)) throw new ArgumentException(nameof(clientId));
            if (String.IsNullOrWhiteSpace(objectId)) throw new ArgumentException(nameof(objectId));
            if (String.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException(nameof(subscriptionId));
            if (String.IsNullOrWhiteSpace(resourceGroupName)) throw new ArgumentException(nameof(resourceGroupName));

            return new ClientContext
            {
                TenantId = tenantId,
                ClientSecret = clientSecret,
                ClientId = clientId,
                ObjectId = objectId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                PreferredLocation = location ?? "southcentralus",
                VaultName = vaultName ?? "keyvaultsample"
            };
        }
        #endregion

        #region properties
        public string TenantId { get; set; }

        public string ClientSecret { get; set; }

        public string ClientId { get; set; }

        public string ObjectId { get; set; }

        public string SubscriptionId { get; set; }

        public string PreferredLocation { get; set; }

        public string VaultName { get; set; }

        public string ResourceGroupName { get; set; }
        #endregion

        #region authentication helpers
        /// <summary>
        /// Returns a task representing the attempt to log in to Azure public as the specified
        /// service principal, with the specified credential.
        /// </summary>
        /// <param name="certificateThumbprint"></param>
        /// <returns></returns>
        public AzureCredentials GetAzureCredentialsAsync(string clientId, string clientSecret, string tenantId, AzureEnvironment environment)
        {
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, environment); 
        }

        public AzureEnvironment Environment(string azureEnvironment)
        {
            if (string.Equals(azureEnvironment, "AzureChinaCloud", StringComparison.OrdinalIgnoreCase))
            {
                return AzureEnvironment.AzureChinaCloud;
            }
            else if (string.Equals(azureEnvironment, "AzureGermanCloud", StringComparison.OrdinalIgnoreCase))
            {
                return AzureEnvironment.AzureGermanCloud;
            }
            else if (string.Equals(azureEnvironment, "AzureUSGovernment", StringComparison.OrdinalIgnoreCase))
            {
                return AzureEnvironment.AzureUSGovernment;
            }
            else
            {
                return AzureEnvironment.AzureGlobalCloud;
            }
        }
        #endregion
    }
}
