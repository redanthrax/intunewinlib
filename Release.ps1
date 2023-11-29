$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib\bin\Release\net6.0\publish\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(".\TestApp", 
        ".\TestApp\AESetup.msi", 
        ".\Pub")
}

Wait-Job $job
Receive-Job $job