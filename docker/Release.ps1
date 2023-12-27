#Make sure the IntuneWinLib.dll is in the same folder as this script.
$job = Start-Job -ScriptBlock {
    #Full path to package folder
    $PackageFolder = ""
    #Full MSI Filename
    $Package = ""
    #Destination for the package ouput
    $Destination = ""
    Add-Type -Path ".\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(
        $PackageFolder, 
        "$PackageFolder\$Package", 
        $Destination)
}

$startTime = Get-Date
Wait-Job $job
$endTime = Get-Date
$duration = $endTime - $startTime
Write-Host "Conversion completed in $($duration.TotalSeconds) seconds."
Receive-Job $job