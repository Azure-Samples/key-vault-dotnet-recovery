using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;

namespace AzureKeyVaultRecoverySamples
{
    /// <summary>
    /// Represents the Azure context of the client running the samples - tenant, subscription, client id and credentials.
    /// </summary>
    public sealed class ClientContext
    {
        private static ClientAssertionCertificate _servicePrincipalCredential = null;

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
        /// service principal, with the specified certificate credential.
        /// </summary>
        /// <param name="certificateThumbprint"></param>
        /// <returns></returns>
        public static Task<ServiceClientCredentials> GetServiceCredentialsAsync( string tenantId, string applicationId, string certificateThumbprint )
        {
            if (_servicePrincipalCredential == null)
            {
                _servicePrincipalCredential = new ClientAssertionCertificate(applicationId, GetCertificateByThumbprint(certificateThumbprint));
            }

            return ApplicationTokenProvider.LoginSilentAsync(
                tenantId,
                _servicePrincipalCredential, 
                ActiveDirectoryServiceSettings.Azure,
                TokenCache.DefaultShared);
        }

        /// <summary>
        /// Generic ADAL Authentication callback
        /// </summary>
        public static async Task<string> AcquireTokenAsync(string authority, string resource, string scope)
        {
            if (_servicePrincipalCredential == null)
            {
                // ugh;read directly from config
                var appId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.ApplicationId];
                var spCredsX5T = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.SPCredentialCertificateThumbprint];

                _servicePrincipalCredential = new ClientAssertionCertificate(appId, GetCertificateByThumbprint(spCredsX5T));
            }

            AuthenticationContext ctx = new AuthenticationContext(authority, false, TokenCache.DefaultShared);
            AuthenticationResult result = await ctx.AcquireTokenAsync(resource, _servicePrincipalCredential).ConfigureAwait(false);

            return result.AccessToken;
        }


        /// <summary>
        /// Retrieves and returns a certificate from the current user store
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            X509Certificate2 certificate = null;

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (found.Count > 0)
                    certificate = found[0];
            }
            catch (Exception)
            {
                Console.WriteLine(String.Format("Failed to retrieve certificate with thumbprint '{0}' from the current user store", thumbprint));

                throw;
            }
            finally
            {
                store.Close();
            }

            return certificate;
        }

        /// <summary>
        /// Generic authentication callback for a specific tenant
        /// </summary>
        /// <param name="tenantId">Identifier of tenant where authentication takes place.</param>
        /// <returns>Authentication callback.</returns>
        /// <remarks>Consider moving this class out from Controllers.Core into a separate top-level lib.</remarks>
        public static Func<Task<string>> GetAuthenticationCallback(string authority, string resource, string scope)
        {
            return () => { return AcquireTokenAsync(authority, resource, scope); };
        }

        #endregion
    }
}
