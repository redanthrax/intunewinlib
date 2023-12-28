function Get-StorageToken {
    param($container)
    $storageContext = New-AzStorageContext -StorageAccountName $env:StorageAccount -StorageAccountKey $env:StorageKey

    $containerName = $container
    $startTime = Get-Date
    $expiryTime = $startTime.AddHours(0.5)
    $permissions = "rwd"

    $opt = @{
        Name = $containerName
        Context = $storageContext
        Permission = $permissions
        ExpiryTime = $expiryTime
    }

    $token = New-AzStorageContainerSASToken @opt

    # $file = "https://$($env:StorageAccount).blob.core.windows.net/uploads/$([guid]::NewGuid()).msi?$sasUri"

    return $token
}