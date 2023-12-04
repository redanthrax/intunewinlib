using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Add-Type -Path ".\dependencies\IntuneWinLib.dll"

Write-Output "POST request - File Upload triggered"

if (-Not($Request.Body)) {
    Write-Output "No file contents"
    return "No File Contents"
}

$fileName = $Request.Body["fileName"]
$fileContents = $Request.Body["fileContents"]
$bytes = [Convert]::FromBase64String($fileContents)

Write-Output "Creating temp file to for package"
$tempFile = New-TemporaryFile
$dir = [System.IO.Path]::GetDirectoryName($tempFile)
$folderGuid = "$dir\$([guid]::NewGuid().ToString())"
New-Item -Path $folderGuid -ItemType Directory
$fullPath = "$folderGuid\$fileName"
[io.file]::WriteAllBytes($fullPath, $bytes)
$destPath = "$dir\$([guid]::NewGuid())"
New-Item -Path $destPath -ItemType Directory

try {
    $file = [IntuneWinLib.Intune]::CreatePackage($folderGuid, $fullPath, $destPath)
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