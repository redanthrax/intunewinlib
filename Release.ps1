$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib\bin\Release\net6.0\publish\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(".\TestApp", 
        ".\TestApp\RemoteDesktop.msi", 
        ".\Pub")
}

Wait-Job $job
Receive-Job $job