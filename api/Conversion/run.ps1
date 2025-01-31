using namespace System.Net

param([byte[]] $InputBlob, $TriggerMetadata)

Import-Module "./Modules/Helpers.psm1"

Write-Output "PowerShell Blob trigger: Name: $($TriggerMetadata.Name) Size: $($InputBlob.Length) bytes"
Write-Output "Importing dependencies."
Add-Type -Path ".\dependencies\Debug\net8.0\IntuneWinLib.dll"
Write-Output "Production ENV: $($env:Production)"
$dir = New-Item -ItemType Directory "C:\home\temp" -Force
if ($env:Production -eq "false") {
    $tempFile = New-TemporaryFile
    $dir = [System.IO.Path]::GetDirectoryName($tempFile)
}

$folderGuid = "$dir\$([guid]::NewGuid().ToString())"
New-Item -Path $folderGuid -ItemType Directory
Write-Output "File Name: $($TriggerMetadata.Name)"
$fileName = $TriggerMetadata.Name
$fullPath = "$folderGuid\$fileName"

Write-Output "Writing content to destination: $fullPath"
try {
    [System.IO.File]::WriteAllBytes($fullPath, $InputBlob)
}
catch {
    Write-Output "Error writing content: $_"
}

Write-Output "Setting up destination path."
$destPath = "$dir\$([guid]::NewGuid())"
New-Item -Path $destPath -ItemType Directory

Write-Output "Writing package to $fullPath"
try {
    if($env:Production -eq "true") {
        $file = [IntuneWinLib.Intune]::CreatePackage($folderGuid, $fullPath, $destPath, "C:\home\temp\intunewin")
    } else {
        Write-Output "Package Paths: $folderGuid, $fullPath, $destPath"
        $file = [IntuneWinLib.Intune]::CreatePackage($folderGuid, $fullPath, $destPath)
    }
    Write-Output "Wrote package to $destPath"
    Write-Output "Package output complete"
}
catch {
    Write-Output "Error: $_"
    return
}

$token = Get-StorageToken "uploads"
#Delete old upload
$uri = "http://127.0.0.1:10000/devstoreaccount1/uploads/$($TriggerMetadata.Name)?$token"
if($env:Production -eq 'true') {
    $uri = "https://$($env:StorageAccount).blob.core.windows.net/uploads/$($TriggerMetadata.Name)?$token"
}

Write-Output "Deleting uploaded file at $uri"
Invoke-RestMethod -Method 'DELETE' -Uri $uri
$token = Get-StorageToken "downloads"
$fname = Split-Path -Path $file -Leaf
$sasUrl = "http://127.0.0.1:10000/devstoreaccount1/downloads/$($fname)?$token"
if($env:Production -eq 'true') {
    $sasUrl = "https://$($env:StorageAccount).blob.core.windows.net/downloads/$($fname)?$token"
}

$fileContent = [System.IO.File]::ReadAllBytes($file)
$headers = @{
    "x-ms-blob-type" = "BlockBlob"
}

try {
    Write-Output "Uploading file to storage. $sasUrl"
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