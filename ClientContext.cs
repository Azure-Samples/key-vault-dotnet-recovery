using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Threading.Tasks;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Represents the Azure context of the client running the samples - tenant, subscription, client id and credentials.
    /// </summary>
    public sealed class ClientContext
    {
        private static ClientCredential _servicePrincipalCredential = null;

        #region construction
        public static ClientContext Build(string tenantId, string objectId, string appId, string subscriptionId, string resourceGroupName, string location, string vaultName)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(objectId)) throw new ArgumentException(nameof(objectId));
            if (String.IsNullOrWhiteSpace(appId)) throw new ArgumentException(nameof(appId));
            if (String.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException(nameof(subscriptionId));
            if (String.IsNullOrWhiteSpace(resourceGroupName)) throw new ArgumentException(nameof(resourceGroupName));

            return new ClientContext
            {
                TenantId = tenantId,
                ObjectId = objectId,
                ApplicationId = appId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                PreferredLocation = location ?? "southcentralus",
                VaultName = vaultName ?? "keyvaultsample"
            };
        }
        #endregion

        #region properties
        public string TenantId { get; set; }

        public string ObjectId { get; set; }

        public string ApplicationId { get; set; }

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
        public static Task<ServiceClientCredentials> GetServiceCredentialsAsync(string tenantId, string applicationId, string appSecret)
        {
            if (_servicePrincipalCredential == null)
            {
                _servicePrincipalCredential = new ClientCredential(applicationId, appSecret);
            }

            return ApplicationTokenProvider.LoginSilentAsync(
                tenantId,
                _servicePrincipalCredential,
                ActiveDirectoryServiceSettings.Azure,
                TokenCache.DefaultShared);
        }
        #endregion
    }
}
