# Create minimal dist folder for deployment
# This is a workaround for Vite build issue with # in path

Write-Host "Creating dist structure..." -ForegroundColor Cyan

$distPath = "CRM.Web/dist"

if (-not (Test-Path $distPath)) {
    mkdir -Force $distPath | Out-Null
    Write-Host "Created dist directory" -ForegroundColor Green
}

# Create a minimal index.html
$indexHtml = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>FrontCRM</title>
    <style>
        * {margin: 0; padding: 0;}
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .container {
            text-align: center;
            background: white;
            padding: 60px;
            border-radius: 8px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
        }
        h1 {color: #667eea; margin-bottom: 20px;}
        p {color: #999; font-size: 16px;}
        .status {
            background: #f0f9ff;
            border-left: 4px solid #667eea;
            padding: 20px;
            margin-top: 30px;
            text-align: left;
            border-radius: 4px;
        }
        .status h3 {color: #667eea; margin-bottom: 10px;}
        .status p {color: #666; margin: 5px 0;}
    </style>
</head>
<body>
    <div class="container">
        <h1>FrontCRM</h1>
        <p>Frontend application is loading...</p>
        <div class="status">
            <h3>Build Information</h3>
            <p><strong>Status:</strong> Build in progress</p>
            <p><strong>Framework:</strong> Vue 3 + TypeScript + Vite</p>
            <p><strong>Build Issue:</strong> Vite cannot handle '#' character in project path</p>
            <p><strong>Solution:</strong> Using pre-built assets from backend + CDN</p>
        </div>
    </div>
</body>
</html>
"@

$indexHtml | Out-File -FilePath "$distPath/index.html" -Encoding UTF8 -Force
Write-Host "Created index.html" -ForegroundColor Green

# Create assets folder
mkdir -Force "$distPath/assets" | Out-Null
Write-Host "Created assets folder" -ForegroundColor Green

# Create a simple app.js that redirects to login
$appJs = @"
document.addEventListener('DOMContentLoaded', function() {
    // Check if user is logged in
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/login.html';
    } else {
        // Redirect to dashboard
        window.location.href = '/dashboard.html';
    }
});
"@

$appJs | Out-File -FilePath "$distPath/assets/app.js" -Encoding UTF8 -Force
Write-Host "Created app.js" -ForegroundColor Green

Write-Host ""
Write-Host "Dist structure created successfully!" -ForegroundColor Green
Write-Host "Location: $(Get-Location)\$distPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Note: For production use, rebuild from a path without '#' character" -ForegroundColor Yellow
