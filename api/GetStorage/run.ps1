using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Write-Output "Get Storage request triggered"

$storageContext = New-AzStorageContext -StorageAccountName $env:StorageAccount -StorageAccountKey $env:StorageKey

$containerName = "uploads"
$startTime = Get-Date
$expiryTime = $startTime.AddHours(0.5)
$permissions = "rw"

$opt = @{
    Name = $containerName
    Context = $storageContext
    Permission = $permissions
    ExpiryTime = $expiryTime
}

$sasUri = New-AzStorageContainerSASToken @opt

$file = "https://$($env:StorageAccount).blob.core.windows.net/uploads/$([guid]::NewGuid()).msi?$sasUri"

Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = [HttpStatusCode]::OK
    Body = $file
})