# Start Docker containers on remote server

$serverIP = "129.226.161.3"
$serverUser = "ubuntu"

Write-Host "Starting Docker containers on server..." -ForegroundColor Green
Write-Host ""

# SSH and start containers
ssh "${serverUser}@${serverIP}" "cd /home/ubuntu/frontcrm_deploy && docker-compose up -d && docker-compose ps"

Write-Host ""
Write-Host "Done! Access the application at:" -ForegroundColor Green
Write-Host "Frontend: http://$serverIP" -ForegroundColor Cyan
Write-Host "Backend: http://$serverIP:5000" -ForegroundColor Cyan
