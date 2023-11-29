$job = Start-Job -ScriptBlock {
    Add-Type -Path ".\IntuneWinLib\bin\Debug\net6.0\IntuneWinLib.dll"
    [IntuneWinLib.Intune]::CreatePackage("C:\Users\ggagnon\source\intunewinlib\TestApp", 
        "C:\Users\ggagnon\source\intunewinlib\TestApp\AESetup.msi", 
        "C:\Users\ggagnon\source\intunewinlib\Pub")
}

Wait-Job $job
Receive-Job $job