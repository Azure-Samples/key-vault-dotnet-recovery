using System.Configuration;
using System.Threading.Tasks;

namespace AzureKeyVaultRecoverySamples
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // run the vault recovery samples
            // soft delete flow with new vault
            Task.Run(() => KeyVaultRecoverySamples.DemonstrateRecoveryAndPurgeForNewVaultAsync()).ConfigureAwait(false).GetAwaiter().GetResult();

            // enabling soft delete on existing vault + soft delete flow 
            Task.Run(() => KeyVaultRecoverySamples.DemonstrateRecoveryAndPurgeForExistingVaultAsync()).ConfigureAwait(false).GetAwaiter().GetResult();

            // soft delete flow for a vault entity
            Task.Run(() => KeyVaultEntityRecoverySamples.DemonstrateRecoveryAndPurgeAsync()).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
