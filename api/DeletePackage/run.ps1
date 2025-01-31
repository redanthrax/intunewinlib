using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Import-Module "./Modules/Helpers.psm1"
$token = Get-StorageToken "downloads"
$uri = "http://127.0.0.1:10000/devstoreaccount1/downloads/$($Request.Body).intunewin?$token"
if($env:Production -eq 'true') {
    $uri = "https://$($env:StorageAccount).blob.core.windows.net/downloads/$($Request.Body).intunewin?$token"
}

Write-Output "Deleting file at $uri"
Invoke-RestMethod -Method 'DELETE' -Uri $uri