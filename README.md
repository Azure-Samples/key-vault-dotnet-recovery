---
page_type: sample
languages:
- csharp
products:
- azure
description: "This repo contains sample code demonstrating the backup/restore and recoverable deletion functionality of Azure Key Vault using the Azure .Net SDK."
urlFragment: key-vault-dotnet-recovery
---

# .Net SDK samples for recovering and restoring Azure Key Vault entities 

This repo contains sample code demonstrating the backup/restore and recoverable deletion functionality of Azure Key Vault using the [Azure .Net SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/key-vault?view=azure-dotnet). The scenarios covered by these samples include:

* Backing up and restoring Key Vault secrets and keys
* Enabling recoverable deletion on creating a new vault
* Enabling recoverable deletion on an existing vault
* Recovering or permanently deleting deleted vaults
* Recovering or permanently deleting Key Vault secrets, keys, and certificates

The recoverable deletion functionality is also referred to as 'soft delete'; consequently, a permanent, irrecoverable deletion is referred to as 'purge'.

## Samples in this repo:

* Back up and restore Key Vault entities
* Enable soft delete
* Delete, recover and purge a vault
* Delete, recover and purge vault entities

## Getting Started

### Prerequisites

- OS: Windows
- SDKs:
    - Microsoft.Azure.Management.KeyVault.Fluent ver. 1.6.0+
    - KeyVault data SDK: Microsoft.Azure.KeyVault ver. 2.3.2+
- Azure:
    - a subscription, in which you have the KeyVaultContributor role
    - an Azure Active Directory application, created in the tenant associated with the subscription, and with access to KeyVault; please see [Accessing Key Vault from a native application](https://blogs.technet.microsoft.com/kv/2016/09/17/accessing-key-vault-from-a-native-application) for details.
    - the credentials of the AAD application, in the form of a client secret 
    

### Installation

- open the solution in Visual Studio - NuGet should resolve the necessary packages


### Quickstart
Follow these steps to get started with this sample:

1. git clone https://github.com/Azure-Samples/key-vault-dotnet-recovery.git
2. cd key-vault-dotnet-recovery
4. edit the app.config file, specifying the tenant, subscription, AD app id, object id and client secret
5. dotnet run AzureKeyVaultRecoverySamples.csproj


## Demo


## Resources

Please see the following links for additional information:

- [Azure Key Vault soft-delete overview](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-ovw-soft-delete)
- [How to use Key Vault soft-delete with PowerShell](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-soft-delete-powershell)
- [How to use Key Vault soft-delete with CLI](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-soft-delete-cli)

The following samples are also related:

- [Recovery scenario samples for Azure Key Vault using the Azure Python SDK](https://azure.microsoft.com/en-us/resources/samples/key-vault-recovery-python/)
