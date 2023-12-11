$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(
        #Path to package folder
        ".\TestApp", 
        #Path to MSI
        ".\TestApp\Konnekt.msi", 
        #Path to Output Folder
        ".\Pub")
}

Wait-Job $job
Receive-Job $job