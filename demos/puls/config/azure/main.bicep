// Puls's Azure build (F61.4), and since F109.2 the louis-agent demo's app beside it: everything in rg-puls-za that
// isn't an app revision, as one deployment that creates what is missing and updates what exists. deploy.ps1 infra runs it (-WhatIf shows Azure's own what-if first); this file
// is the record of what Azure holds. No secret value is here: the SQL admin password and the SSH key come in as
// parameters the verb reads from the git-ignored config files beside this folder.
//
// What it declares: the private network with the apps' and the services' subnets, the security group that lets only
// the apps' subnet reach the VM, the VM puls-services with its data disk and cloud-init (deploy/azure/vm/), the
// Container Apps environment puls-env on the network with its logs, telemetry and the app's file share, the pull
// right on the registry, and the SQL server with the free-offer database puls_accounts. What exists from CL19 and is
// kept is referenced, never redeclared: the registry, the Log Analytics workspace, the storage account.

targetScope = 'resourceGroup'

@description('Where everything is; the resource group\'s own region.')
param location string = resourceGroup().location

@description('The admin login of the SQL server; sa is not allowed on Azure SQL.')
param sqlAdminLogin string = 'pulsadmin'

@secure()
@description('The admin password of the SQL server, MSSQL_SA_PASSWORD in config/.env.sql (the verb reads it).')
param sqlAdminPassword string

@description('The public key that may log in to the VM as its admin user; ~/.ssh/id_ed25519.pub on the PC (the verb reads it).')
param sshPublicKey string

@description('The VM\'s admin user.')
param vmAdminUser string = 'puls'

@description('The VM\'s size: a B-series with 4 GiB is enough for Mongo, Chrome and SearXNG at this load.')
param vmSize string = 'Standard_B2s'

@description('One public IPv4 address that may SSH to the VM; empty, and no SSH rule exists at all (the default).')
param sshFromIp string = ''

// ---- What CL19 made and this build keeps ----

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: 'acrpulsza'
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: 'puls-logs'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: 'pulsstorage'
}

// The vault that holds every secret value (Louis's pick, 2026-11-08): deploy.ps1 secrets writes them here, and the
// app's secrets are references to them by URL, read with the identity below.
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: 'puls-keys'
}

// ---- The app's identity: what reads the vault ----

resource appIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'puls-app-identity'
  location: location
}

// Key Vault Secrets User on the vault; the role's id is Azure's fixed one.
resource vaultReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, appIdentity.id, 'KeyVaultSecretsUser')
  scope: vault
  properties: {
    principalId: appIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

// ---- The second app's identity (F109.2): the louis-agent demo's app beside Puls, in the same build, split later ----

resource codingAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'louis-agent-app-identity'
  location: location
}

resource codingVaultReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, codingAppIdentity.id, 'KeyVaultSecretsUser')
  scope: vault
  properties: {
    principalId: codingAppIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

// ---- The network: the apps on one subnet, the services on another ----

resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: 'puls-vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        // A Container Apps environment on a network wants a /23 of its own, delegated to it.
        name: 'apps'
        properties: {
          addressPrefix: '10.0.0.0/23'
          delegations: [
            {
              name: 'containerapps'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'services'
        properties: {
          addressPrefix: '10.0.2.0/24'
          networkSecurityGroup: {
            id: servicesSecurityGroup.id
          }
        }
      }
    ]
  }
}

// The VM answers the apps' subnet on the three service ports and nothing else; SSH only from one address, when asked.
resource servicesSecurityGroup 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: 'puls-services-nsg'
  location: location
  properties: {
    securityRules: concat(
      [
        {
          name: 'apps-to-services'
          properties: {
            priority: 100
            direction: 'Inbound'
            access: 'Allow'
            protocol: 'Tcp'
            sourceAddressPrefix: '10.0.0.0/23'
            sourcePortRange: '*'
            destinationAddressPrefix: '*'
            destinationPortRanges: ['27017', '9222', '8080']
          }
        }
        {
          name: 'deny-everything-else'
          properties: {
            priority: 4000
            direction: 'Inbound'
            access: 'Deny'
            protocol: '*'
            sourceAddressPrefix: '*'
            sourcePortRange: '*'
            destinationAddressPrefix: '*'
            destinationPortRange: '*'
          }
        }
      ],
      empty(sshFromIp) ? [] : [
        {
          name: 'ssh-from-louis'
          properties: {
            priority: 200
            direction: 'Inbound'
            access: 'Allow'
            protocol: 'Tcp'
            sourceAddressPrefix: '${sshFromIp}/32'
            sourcePortRange: '*'
            destinationAddressPrefix: '*'
            destinationPortRange: '22'
          }
        }
      ]
    )
  }
}

// ---- The VM: Mongo, the browser and SearXNG in Docker, on a private address only ----

resource vmNic 'Microsoft.Network/networkInterfaces@2024-01-01' = {
  name: 'puls-services-nic'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'private'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: '${vnet.id}/subnets/services'
          }
          // No public IP: nothing on this VM is reachable from the internet, and files go up through run-command.
        }
      }
    ]
  }
}

resource vm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: 'puls-services'
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: 'puls-services'
      adminUsername: vmAdminUser
      // The first boot runs deploy/azure/vm/cloud-init.yaml: Docker, the data disk at /data, the services unit.
      customData: loadFileAsBase64('../../deploy/azure/vm/cloud-init.yaml')
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${vmAdminUser}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'StandardSSD_LRS'
        }
      }
      dataDisks: [
        {
          // LUN 0 is what cloud-init formats as XFS and mounts at /data: Mongo's files outlive the VM.
          lun: 0
          name: 'puls-services-data'
          createOption: 'Empty'
          diskSizeGB: 32
          managedDisk: {
            storageAccountType: 'StandardSSD_LRS'
          }
        }
      ]
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: vmNic.id
        }
      ]
    }
  }
}

// ---- The apps' environment, on the network, with its logs and traces ----

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'puls-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: 'puls-env'
  location: location
  identity: {
    // The environment pulls the app's image from the registry as itself (the yaml's `identity: system-environment`).
    type: 'SystemAssigned'
  }
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: '${vnet.id}/subnets/apps'
      internal: false
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    // The environment's OpenTelemetry agent (F52): every app gets the collector's address, traces and logs go to
    // Application Insights; the yaml sets only OTEL_SERVICE_NAME.
    appInsightsConfiguration: {
      connectionString: insights.properties.ConnectionString
    }
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: ['appInsights']
      }
      logsConfiguration: {
        destinations: ['appInsights']
      }
    }
  }
}

// The app's file share from CL19 (config/, logs/, data/), on the new environment under the name the yaml mounts.
resource appStorage 'Microsoft.App/managedEnvironments/storages@2024-10-02-preview' = {
  parent: environment
  name: 'puls-app-vol'
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: 'puls-app-workspace'
      accessMode: 'ReadWrite'
    }
  }
}

// The second app's file share (F109.2), made here since nothing older made it: config/, logs/ and data/ for the
// louis-agent demo's app, on the same storage account, mounted on the environment under its own name.
resource storageFiles 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource codingShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: storageFiles
  name: 'louis-agent-workspace'
  properties: {
    shareQuota: 5
  }
}

resource codingAppStorage 'Microsoft.App/managedEnvironments/storages@2024-10-02-preview' = {
  parent: environment
  name: 'louis-agent-vol'
  dependsOn: [codingShare]
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: 'louis-agent-workspace'
      accessMode: 'ReadWrite'
    }
  }
}

// AcrPull for the environment on the registry; the role's id is Azure's fixed one for AcrPull.
resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, environment.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: environment.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

// ---- The accounts' database: Azure SQL's free offer ----

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'puls-sql-za'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// "Allow Azure services": the apps reach the server on its public endpoint over TLS (a private endpoint costs money).
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// The free offer: serverless General Purpose, 32 GB, 100,000 vCore-seconds a month, paused when idle (the first
// call after a pause fails with error 40613, which EF's retry absorbs) and paused for the month when the free
// seconds run out, never billed.
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'puls_accounts'
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    zoneRedundant: false
  }
}

// ---- What the verbs read back, never copied by hand ----

@description('The VM\'s private address, which the app\'s URLs for Mongo, the browser and SearXNG use.')
output vmPrivateIp string = vmNic.properties.ipConfigurations[0].properties.privateIPAddress

@description('The environment the app is created in.')
output environmentId string = environment.id

@description('The environment\'s public address: the A record for mypuls.co.za.')
output environmentStaticIp string = environment.properties.staticIp

@description('The environment\'s domain, under which the app\'s own address sits.')
output environmentDefaultDomain string = environment.properties.defaultDomain

@description('The SQL server\'s address, for the accounts\' connection string.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The identity the app reads the vault with; the yaml names it.')
output appIdentityId string = appIdentity.id

@description('The identity the second app, the louis-agent demo\'s, reads the vault with (F109.2); its yaml names it.')
output codingAppIdentityId string = codingAppIdentity.id

@description('The vault the app\'s secrets reference, by URL.')
output vaultUri string = vault.properties.vaultUri
