using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Add-Type -Path ".\dependencies\IntuneWinLib.dll"

Write-Output "POST request - File Upload triggered"

if (-Not($Request.Body)) {
    return "Could not process file."
}

Write-Output "Downloading: $($Request.Body)"

Write-Output "Creating temp file for package"
if ($env:Production -eq "false") {
    $tempFile = New-TemporaryFile
}
else {
    Write-Output "Production env"
    $filename = "$([guid]::NewGuid().ToString()).tmp"
    $tempFile = New-Item -ItemType File -Path "C:\home\temp\$filename" -Force
}

$dir = [System.IO.Path]::GetDirectoryName($tempFile)
$folderGuid = "$dir\$([guid]::NewGuid().ToString())"
New-Item -Path $folderGuid -ItemType Directory
$fileName = "$([guid]::NewGuid()).msi"
$fullPath = "$folderGuid\$fileName"
(New-Object System.Net.WebClient).DownloadFile($Request.Body, $fullPath)
$destPath = "$dir\$([guid]::NewGuid())"
New-Item -Path $destPath -ItemType Directory

try {
    if($env:Production -eq "true") {
        $file = [IntuneWinLib.Intune]::CreatePackage($folderGuid, $fullPath, $destPath, "C:\home\temp\intunewin")
    } else {
        $file = [IntuneWinLib.Intune]::CreatePackage($folderGuid, $fullPath, $destPath)
    }
    Write-Output "Wrote package to $destPath"
    Write-Output "Package output complete"
}
catch {
    Write-Output "Error: $_"
}


#delete from storage
Invoke-RestMethod -Method 'DELETE' -Uri $Request.Body
$fname = Split-Path -Path $file -Leaf

#need to make new sas url
Write-Output "Getting SAS token"
$storageContext = New-AzStorageContext -StorageAccountName $env:StorageAccount -StorageAccountKey $env:StorageKey

$containerName = "uploads"
$startTime = Get-Date
$expiryTime = $startTime.AddHours(0.5)
$permissions = "rwd"

$opt = @{
    Name = $containerName
    Context = $storageContext
    Permission = $permissions
    ExpiryTime = $expiryTime
}

$sasToken = New-AzStorageContainerSASToken @opt
$sasUrl = "http://127.0.0.1:10000/devstoreaccount1/uploads/$($fname)?$sasToken"
if($env:Production -eq 'true') {
    $sasUrl = "https://$($env:StorageAccount).blob.core.windows.net/uploads/$($fname)?$sasToken"
}

Push-OutputBinding -Name response -Value ([HttpResponseContext]@{
    StatusCode = [System.Net.HttpStatusCode]::OK
    Body = $sasUrl
})

$fileContent = [System.IO.File]::ReadAllBytes($file)
$headers = @{
    "x-ms-blob-type" = "BlockBlob"
}

try {
    Write-Output "Uploading file to storage."
    Invoke-WebRequest -Uri $sasUrl -Method "PUT" -Headers $headers -Body $fileContent -TimeoutSec 300
    Write-Output "File uploaded"
}
catch {
    Write-Output "An error occured during upload: $_"
}

Write-Output "Cleaning up"
Remove-Item $fullPath -Recurse -Force
Remove-Item $tempFile -Force
if ($env:Production -eq "true") {
    Remove-Item "C:\home\temp" -Recurse -Force
}
