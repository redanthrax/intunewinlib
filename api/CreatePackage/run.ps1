using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Add-Type -Path ".\dependencies\IntuneWinLib.dll"

Write-Output "POST request - File Upload triggered"
$fileContent = $Request.Body
$tempFile = New-TemporaryFile
$writeDir = [System.IO.Path]::GetDirectoryName($tempFile.FullName)
Remove-item $tempFile
$file = "$writeDir\\intune.msi"
[io.file]::WriteAllBytes($file, $fileContent)
#$dir = [System.IO.Path]::GetDirectoryName($file)
$dir = "C:\\Users\\ggagnon\\AppData\\Local\\Temp"

Write-Output $dir
Write-Output $file

[IntuneWinLib.Intune]::CreatePackage($dir, $file, $dir)
Write-Output "Package output complete"