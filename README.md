---
page_type: sample
languages:
- csharp
products:
- azure
- azure-storage
- dotnet
description: "This repo contains sample code demonstrating the backup/restore and recoverable deletion functionality of Azure Key Vault using the Azure .NET SDK."
urlFragment: net-sdk-samples
---

# How to recover and restore Azure Key Vault entities with .NET 

## Prerequisites

To complete this tutorial:

* Install .NET Core 3.1 version for [Linux] or [Windows]

If you don't have an Azure subscription, create a [free account] before you begin.

### Create an App registration using the Azure Portal

1.  Go to the [Azure Portal] and log in using your Azure account. 
2.  Search for and select **Azure Active Directory** > **Manage** > **App registrations**. 
3.  Select **New registration**.  
4.  Enter a name for your App registrations, then click **Register**.
5.  Under **Overview** select **Application (client) ID**, **Directory (tenant) ID**, and **Object ID** copy to text editor for later use.
6.  Under **Manage** > **Certificates & secrets** > **New client secret**, filter **Description** and click **Add**.
7.  Copy preview created secret value to text editor for later use.

## Run the application
First, clone the repository on your machine:

```bash
git clone https://github.com/Azure-Samples/key-vault-dotnet-recovery.git
```

Then, switch to the project folder to edit the app.config file, specifying the required parameters.
```bash
cd key-vault-dotnet-recovery
```
Finally, run the application with the `dotnet run` command.

```console
dotnet run
```

## This sample shows how to do following operations of Azure Key Vault
1. Back up and restore Key Vault entities.
2. Enable soft delete.
3. Delete, recover and purge a vault.
4. Delete, recover and purge vault entities.

The following samples are also related:

- [Recovery scenario samples for Azure Key Vault using the Azure Python SDK]

## More information

The [Azure Key Vault documentation] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

This project has adopted the [Microsoft Open Source Code of Conduct].
For more information see the [Code of Conduct FAQ] or contact [opencode@microsoft.com] with any additional questions or comments.

<!-- LINKS -->
[Linux]: https://dotnet.microsoft.com/download
[Windows]: https://dotnet.microsoft.com/download
[free account]: https://azure.microsoft.com/free/?WT.mc_id=A261C142F
[Azure Portal]: https://portal.azure.com
[Recovery scenario samples for Azure Key Vault using the Azure Python SDK]: https://azure.microsoft.com/resources/samples/key-vault-recovery-python
[Azure Key Vault documentation]: https://docs.microsoft.com/azure/key-vault/general/basic-concepts
[Microsoft Open Source Code of Conduct]: https://opensource.microsoft.com/codeofconduct
[Code of Conduct FAQ]: https://opensource.microsoft.com/codeofconduct/faq
[opencode@microsoft.com]: mailto:opencode@microsoft.com
