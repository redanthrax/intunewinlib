using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Import-Module "./Modules/Helpers.psm1"
$token = Get-StorageToken "downloads"
$uri = "https://$($env:StorageAccount).blob.core.windows.net/downloads/$($Request.Body).intunewin?$token"
Write-Output "Deleting file at $uri"
Invoke-RestMethod -Method 'DELETE' -Uri $uri