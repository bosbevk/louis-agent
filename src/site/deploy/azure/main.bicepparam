// Our own instance of main.bicep: the names the two sites' resources already have in rg-puls-za, which were made
// beside Puls and keep its name, since Azure can't rename a resource (F170, Louis's pick 2). Another business or a
// spawned site writes a file like this one with names of its own.
using 'main.bicep'

param location = 'southafricanorth'

param workspaceName = 'puls-logs'
param workspaceRetentionDays = 30

param insightsName = 'puls-insights'
param insightsRetentionDays = 90

param environmentName = 'puls-env'

param registryName = 'acrpulsza'
param registryPullAssignmentName = '3b785df2-c303-5865-adb0-94ee624325e1'

param vaultName = 'puls-keys'
param appIdentityName = 'louis-agent-app-identity'
param appVaultReadAssignmentName = '6138e869-f58e-597f-9b5e-ad5c2a1157d1'

// Louis, who fills the vault, and github-puls-deploy, the identity deploy-sites.yml releases as.
param vaultAdminPrincipalId = 'e48029d5-81f8-4cdd-8006-e9a8b4650276'
param vaultAdminAssignmentName = '3a2229ca-c39f-4001-84ed-16d24b6ccc6c'
param releasePrincipalId = '293d31f9-e8a6-408f-ab09-7a01a9c31e16'
param releaseVaultReadAssignmentName = '3e128fb0-5a4f-42d7-b3ce-87d85d4fdd0a'
param releaseGroupAssignmentName = 'd16c7301-259f-4696-b0b1-31fe8f7fc142'

param storageName = 'pulsstorage'
param shareName = 'louis-agent-workspace'
param shareQuotaGiB = 5
