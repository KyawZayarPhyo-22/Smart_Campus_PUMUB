[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$body = @{
    AccountStatus = "Active"
} | ConvertTo-Json

Write-Host "Sending PUT request to NewStudentAcc status update..."
try {
    $res = Invoke-WebRequest -Uri "https://localhost:7297/api/newstudentacc/1/status" -Method Put -Body $body -ContentType "application/json"
    Write-Host "Status Code: $($res.StatusCode)"
    Write-Host "Response Body: $($res.Content)"
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $bodyText = $reader.ReadToEnd()
        Write-Host "Error Response Body: $bodyText"
    }
}
