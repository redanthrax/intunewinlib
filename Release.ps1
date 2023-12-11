$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(
        #Path to package folder
        ".\App", 
        #Path to MSI
        ".\App\Konnekt.msi", 
        #Path to Output Folder
        ".\PublishApp")
}

Wait-Job $job
Receive-Job $job