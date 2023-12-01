$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib\bin\Debug\net6.0\publish\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage(".\TestApp", 
        ".\TestApp\Konnekt.msi", 
        ".\Pub")
}

Wait-Job $job
Receive-Job $job