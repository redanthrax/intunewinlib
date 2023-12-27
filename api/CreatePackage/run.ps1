using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Add-Type -Path ".\dependencies\IntuneWinLib.dll"

Write-Output "POST request - File Upload triggered"

if (-Not($Request.Body)) {
    return "Could not process file."
}

Write-Output "Downloading: $($Request.Body)"

Write-Output "Creating temp file to for package"
if ($env:Production -eq "false") {
    $tempFile = New-TemporaryFile
}
else {
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

$fname = Split-Path -Path $file -Leaf

$headers = @{
    "Content-Type" = "application/octet-stream"
    "Content-Disposition" = "attachment; filename=`"$fname`""
}

Push-OutputBinding -Name response -Value ([HttpResponseContext]@{
    StatusCode = [System.Net.HttpStatusCode]::OK
    Headers = $headers
    Body = [io.file]::ReadAllBytes($file)
})

Write-Output "Cleaning up"
Remove-Item $fullPath -Recurse -Force
Remove-Item $fname -Recurse -Force
Remove-Item $tempFile -Force
if ($env:Production -eq "true") {
    Remove-Item "C:\home\temp" -Recurse -Force
}
Invoke-RestMethod -Method 'DELETE' -Uri $Request.Body