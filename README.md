---
services: key-vault
platforms: dotnet
author: dragosav
---

# .Net SDK samples for recovering and restoring Azure Key Vault entities 

This repo contains sample code demonstrating the backup/restore and recoverable deletion functionality of Azure Key Vault using the [Azure .Net SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/key-vault?view=azure-dotnet). The scenarios covered by these samples include:

* Backing up and restoring Key Vault secrets and keys
* Enabling recoverable deletion on creating a new vault
* Enabling recoverable deletion on an existing vault
* Recovering or permanently deleting deleted vaults
* Recovering or permanently deleting Key Vault secrets, keys, and certificates

The recoverable deletion functionality is also referred to as "soft delete"; consequently, a permanent, irrecoverable deletion is referred to as 'purge'.

## Samples in this repo:

* Back up and restore Key Vault entities
* Enable soft delete
* Delete, recover and purge a vault
* Delete, recover and purge vault entities

## Getting Started

### Prerequisites

(ideally very short, if any)

- OS
- Library version
- ...

### Installation

(ideally very short)

- npm install [package name]
- mvn install
- ...

### Quickstart
(Add steps to get up and running quickly)

1. git clone [repository clone url]
2. cd [respository name]
3. ...


## Demo

A demo app is included to show how to use the project.

To run the demo, follow these steps:

(Add steps to start up the demo)

1.
2.
3.

## Resources

(Any additional resources or related projects)

- Link to supporting information
- Link to similar sample
- ...
