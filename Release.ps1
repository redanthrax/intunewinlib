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

Wait-Job $job
Receive-Job $job