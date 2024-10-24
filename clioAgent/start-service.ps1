# Define the service name and path to the executable
$serviceName = "clioAgentService"
$exePath = $exePath = Join-Path (Get-Location) "clioAgent.exe"
$displayName = "Clio Agent awesome service"
$description = "Call ATF Team"

# Check if the service already exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($service -eq $null) {
    # If service doesn't exist, create it
    New-Service -Name $serviceName `
                -BinaryPathName "`"$exePath`"" `
                -DisplayName $displayName `
                -Description $description `
                -StartupType Automatic
    Write-Host "Service '$serviceName' created successfully."
} else {
    Write-Host "Service '$serviceName' already exists."
}

# Start the service
Start-Service -Name $serviceName

# Check the status of the service
$service = Get-Service -Name $serviceName
if ($service.Status -eq 'Running') {
    Write-Host "Service '$serviceName' started successfully."
} else {
    Write-Host "Failed to start service '$serviceName'."
}
