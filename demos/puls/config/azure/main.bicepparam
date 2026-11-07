// The parameters of Puls's Azure build (F61.4). The two values that are secrets or personal come from the environment
// variables deploy.ps1 infra sets from the git-ignored files, so this file holds no value of either:
//   PULS_SQL_ADMIN_PASSWORD from config/.env.sql (MSSQL_SA_PASSWORD), PULS_SSH_PUBLIC_KEY from ~/.ssh/id_ed25519.pub,
//   PULS_SSH_FROM_IP, empty unless `deploy.ps1 infra -Ip` opens SSH from one address.
using './main.bicep'

param sqlAdminPassword = readEnvironmentVariable('PULS_SQL_ADMIN_PASSWORD')
param sshPublicKey = readEnvironmentVariable('PULS_SSH_PUBLIC_KEY')
param sshFromIp = readEnvironmentVariable('PULS_SSH_FROM_IP', '')
