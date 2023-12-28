using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

Import-Module "./Modules/Helpers.psm1"

$token = Get-StorageToken "downloads"
$file = "https://$($env:StorageAccount).blob.core.windows.net/downloads/$($Request.Body).intunewin?$token"

#check if the file is ready
try {
    Write-Output "Checking for $file"
    $headers = @{
        "x-ms-blob-type" = "BlockBlob"
    }

    $response = Invoke-WebRequest -Uri $file -Method "HEAD" -Headers $headers
    Write-Output $response
    if($response.StatusCode -eq 200) {
        Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
            StatusCode = [HttpStatusCode]::OK
            Body = $file
        })
    }
    else {
        Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
            StatusCode = [HttpStatusCode]::NotFound
        })
    }
}
catch {
    "Error: $_"
    Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
        StatusCode = [HttpStatusCode]::NotFound
    })
}