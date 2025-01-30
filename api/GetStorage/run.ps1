using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Import-Module "./Modules/Helpers.psm1"

Write-Output "Get Storage request triggered"

$token = Get-StorageToken "uploads"
Write-Error "Token: $token"
#$file = "https://$($env:StorageAccount).blob.core.windows.net/uploads/$([guid]::NewGuid()).msi?$token"
$file = "http://127.0.0.1:10000/devstoreaccount1/uploads/$([guid]::NewGuid()).msi?$token"

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body = $file
})