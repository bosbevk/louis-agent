// What louis-agent.dev and framework.louis-agent.dev stand on in Azure, so it can be made again from code (F170).
// Every resource is one of Microsoft's Azure Verified Modules, pinned to a version, but the apps' environment, a
// plain resource in apps-environment.bicep, since its module can't set its telemetry agent. That environment sits in
// the apps' own resource group on no network of ours (FX249), so no load balancer is billed for it. Every name is a
// parameter: our own instance's values are in main.bicepparam beside this file. The two apps and their domains are
// released by deploy.ps1, which reads this deployment's outputs. Each part is told what stands wherever a default
// differs, so a deploy over the running sites changes nothing but what a change means to; what-if shows it
// (deploy.ps1 infra).
//
//   az deployment group what-if -g <group> -f main.bicep -p main.bicepparam

@description('Where everything runs.')
param location string

@description('The log workspace the environment sends its app logs to.')
param workspaceName string

@description('How many days the workspace keeps its logs.')
param workspaceRetentionDays int

@description('Application Insights, where the environment\'s telemetry agent sends logs and traces.')
param insightsName string

@description('How many days Application Insights keeps its data.')
param insightsRetentionDays int

@description('The resource group the apps and their environment have to themselves (FX249), made by deploy.ps1 infra.')
param appsGroupName string

@description('The Container Apps environment the two apps run in, in the apps\' group.')
param environmentName string

@description('The name the environment knows the apps\' share by, which each app\'s yaml mounts.')
param shareLinkName string

@description('The registry the apps\' images are built in and pulled from.')
param registryName string

@description('The vault that holds the two apps\' secrets.')
param vaultName string

@description('The identity both apps read the vault with.')
param appIdentityName string

@description('The name of the apps\' identity\'s right to read the vault\'s secrets.')
param appVaultReadAssignmentName string

@description('Who fills the vault (deploy.ps1 secrets): a person, with the right to manage its secrets.')
param vaultAdminPrincipalId string

@description('The name of that person\'s right on the vault.')
param vaultAdminAssignmentName string

@description('The identity the deploy workflow releases the apps as, signed in through OIDC from main.')
param releasePrincipalId string

@description('The name of the release identity\'s right to list the vault\'s secrets, which a release names in the apps\' yaml.')
param releaseVaultReadAssignmentName string

@description('The name of the release identity\'s right to update the apps in the resource group.')
param releaseGroupAssignmentName string

@description('The name of the release identity\'s right to make and update the apps in the apps\' group.')
param releaseAppsGroupAssignmentName string

@description('The name of the apps\' environment\'s right to pull from the registry: a role assignment keeps its name for life, so a new environment takes a new one.')
param appsRegistryPullAssignmentName string

@description('The storage account that holds the apps\' file share.')
param storageName string

@description('The file share the apps keep their logs and saved content on, a folder each.')
param shareName string

@description('The share\'s size, in GiB.')
param shareQuotaGiB int

// Azure's own ids for the roles the file gives, the same in every subscription.
var acrPullRole = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var vaultSecretsUserRole = '4633458b-17de-408a-b874-0445c86b69e6'
var vaultAdministratorRole = '00482a5a-887f-4fb3-b363-3b7fe8e74483'
var vaultReaderRole = '21090545-7ca7-4776-b22c-e363652d74d2'
var contributorRole = 'b24988ac-6180-42a0-ab88-20f7382dd24c'


// The log workspace. Key-based sign-in stays on: the environment sends its logs with the workspace's shared key.
module workspace 'br/public:avm/res/operational-insights/workspace:0.16.2' = {
  name: 'workspace'
  params: {
    name: workspaceName
    location: location
    skuName: 'PerGB2018'
    dataRetention: workspaceRetentionDays
    forceCmkForQuery: false
    features: {
      disableLocalAuth: false
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    enableTelemetry: false
  }
}

// Application Insights on the workspace. Visitors' IP addresses stay masked, as they are today.
module insights 'br/public:avm/res/insights/component:0.8.0' = {
  name: 'insights'
  params: {
    name: insightsName
    location: location
    workspaceResourceId: workspace.outputs.resourceId
    kind: 'web'
    applicationType: 'web'
    retentionInDays: insightsRetentionDays
    disableIpMasking: false
    enableTelemetry: false
  }
}

// The apps' environment and its share link, in the apps' own group (FX249). The keys travel as secure parameters.
module appsEnvironment 'apps-environment.bicep' = {
  name: 'appsEnvironment'
  scope: resourceGroup(appsGroupName)
  dependsOn: [storage]
  params: {
    location: location
    environmentName: environmentName
    workspaceCustomerId: workspace.outputs.logAnalyticsWorkspaceId
    workspaceSharedKey: workspace.outputs.primarySharedKey
    insightsConnectionString: insights.outputs.connectionString
    shareLinkName: shareLinkName
    storageName: storageName
    storageKey: storageAccount.listKeys().keys[0].value
    shareName: shareName
  }
}

// The registry, on its Basic tier, with the environment's right to pull. No admin user and no anonymous pull: the
// environment signs in as itself. No network rules: the module's default would deny every address, and with it the
// environment's pulls, so the default action is Allow, which sends no rule set at all.
module registry 'br/public:avm/res/container-registry/registry:0.13.2' = {
  name: 'registry'
  params: {
    name: registryName
    location: location
    acrSku: 'Basic'
    acrAdminUserEnabled: false
    anonymousPullEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleSetDefaultAction: 'Allow'
    roleAssignmentMode: 'LegacyRegistryPermissions'
    zoneRedundancy: 'Disabled'
    exportPolicyStatus: 'enabled'
    azureADAuthenticationAsArmPolicyStatus: 'enabled'
    retentionPolicyStatus: 'disabled'
    retentionPolicyDays: 7
    roleAssignments: [
      {
        name: appsRegistryPullAssignmentName
        principalId: appsEnvironment.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: acrPullRole
      }
    ]
    enableTelemetry: false
  }
}

// The identity both apps read the vault with: deploy.ps1 names it in each app's secrets.
module appIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.6.0' = {
  name: 'appIdentity'
  params: {
    name: appIdentityName
    location: location
    isolationScope: 'None'
    enableTelemetry: false
  }
}

// The vault, on role-based access, with soft delete for 90 days. Purge protection stays off, as it is today: the
// module's default would turn it on, and a vault's purge protection can never be turned off again. Nothing else in
// Azure reads it at deploy time, and no address is denied.
module vault 'br/public:avm/res/key-vault/vault:0.14.3' = {
  name: 'vault'
  params: {
    name: vaultName
    location: location
    sku: 'standard'
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: false
    enableVaultForDeployment: false
    enableVaultForTemplateDeployment: false
    enableVaultForDiskEncryption: false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'None'
      defaultAction: 'Allow'
    }
    roleAssignments: [
      {
        name: appVaultReadAssignmentName
        principalId: appIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: vaultSecretsUserRole
      }
      {
        name: vaultAdminAssignmentName
        principalId: vaultAdminPrincipalId
        principalType: 'User'
        roleDefinitionIdOrName: vaultAdministratorRole
      }
      {
        name: releaseVaultReadAssignmentName
        principalId: releasePrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: vaultReaderRole
      }
    ]
    enableTelemetry: false
  }
}

// The release identity updates the apps in the group (deploy-sites.yml); it can't grant rights, so this file is
// deployed by a person.
module releaseGroupRight 'br/public:avm/res/authorization/role-assignment/rg-scope:0.1.1' = {
  name: 'releaseGroupRight'
  params: {
    name: releaseGroupAssignmentName
    principalId: releasePrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionIdOrName: contributorRole
    enableTelemetry: false
  }
}

// The same right on the apps' group (FX249), where a release makes and updates the two apps.
module releaseAppsGroupRight 'br/public:avm/res/authorization/role-assignment/rg-scope:0.1.1' = {
  name: 'releaseAppsGroupRight'
  scope: resourceGroup(appsGroupName)
  params: {
    name: releaseAppsGroupAssignmentName
    principalId: releasePrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionIdOrName: contributorRole
    enableTelemetry: false
  }
}

// The storage account and the apps' share. The environment mounts the share with the account's key, so key access
// stays on and no address is denied; the module's defaults would deny every address, turn on infrastructure
// encryption (which can't change on an account that stands) and replicate to a second region.
module storage 'br/public:avm/res/storage/storage-account:0.33.2' = {
  name: 'storage'
  params: {
    name: storageName
    location: location
    kind: 'StorageV2'
    skuName: 'Standard_LRS'
    accessTier: 'Hot'
    allowSharedKeyAccess: true
    defaultToOAuthAuthentication: false
    requireInfrastructureEncryption: false
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    dnsEndpointType: 'Standard'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    blobServices: {
      containerDeleteRetentionPolicyEnabled: true
      containerDeleteRetentionPolicyDays: 7
      deleteRetentionPolicyEnabled: true
      deleteRetentionPolicyDays: 7
      deleteRetentionPolicyAllowPermanentDelete: false
    }
    fileServices: {
      // The share's traffic stays encrypted in transit, as it is today; left out, the setting would be cleared.
      protocolSettings: {
        smb: {
          // The module's types are an older API version's, which doesn't list this setting; Azure takes it.
          #disable-next-line BCP037
          encryptionInTransit: {
            required: true
          }
        }
      }
      shareDeleteRetentionPolicy: {
        enabled: false
      }
      shares: [
        {
          name: shareName
          shareQuota: shareQuotaGiB
          accessTier: 'TransactionOptimized'
          enabledProtocols: 'SMB'
        }
      ]
    }
    enableTelemetry: false
  }
}

// The storage account as Azure holds it once its module has run, so the apps' environment can mount the share with
// its key.
resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' existing = {
  name: storageName
}

// What deploy.ps1 reads for every release, by the names it has always read them by: the environment the apps run in
// and its address for the apex's DNS record, the identity the apps read the vault with, and the vault's address.
output environmentId string = appsEnvironment.outputs.environmentId
output environmentStaticIp string = appsEnvironment.outputs.staticIp
output codingAppIdentityId string = appIdentity.outputs.resourceId
output vaultUri string = vault.outputs.uri
