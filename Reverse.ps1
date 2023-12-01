$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib\bin\Debug\net6.0\publish\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::ExtractPackage(".\Pub\Konnekt.intunewin", ".\Pub")
}

Wait-Job $job
Receive-Job $job