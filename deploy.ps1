# ========================================
# DEPLOYMENT SCRIPT FOR WINDOWS SERVER
# ASP.NET Core 8.0 Web Application
# ========================================
param(
    [string]$Environment = "Release",
    [string]$OutputPath = ".\artifacts\deploy",
    [switch]$PublishOnly = $false
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "🚀 DEPLOYMENT SCRIPT" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$ProjectFile = ".\WebApp\WebApp.csproj"
$PublishFolder = "$OutputPath\WebApp"

# Step 1: Clean
Write-Host "📦 Step 1: Cleaning..." -ForegroundColor Yellow
Remove-Item -Path $OutputPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Step 2: Restore
Write-Host "📥 Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectFile
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Restore failed!" -ForegroundColor Red
    exit 1 
}

# Step 3: Build
Write-Host "🔨 Step 3: Building..." -ForegroundColor Yellow
dotnet build $ProjectFile -c $Environment --no-restore
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1 
}

# Step 4: Publish (Self-contained)
Write-Host "📤 Step 4: Publishing (self-contained)..." -ForegroundColor Yellow
dotnet publish $ProjectFile -c $Environment -o $PublishFolder `
    --self-contained -r win-x64 `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

# Step 5: Create deployment archive
Write-Host "📦 Step 5: Creating deployment archive..." -ForegroundColor Yellow
$ZipPath = "$OutputPath\WebApp-$(Get-Date -Format 'yyyyMMdd-HHmmss').zip"
Compress-Archive -Path $PublishFolder -DestinationPath $ZipPath -Force
Write-Host "✅ Archive created: $ZipPath" -ForegroundColor Green
Write-Host ""

# Step 6: Display deployment info
Write-Host "=====================================" -ForegroundColor Green
Write-Host "✅ DEPLOYMENT READY!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Published files location:" -ForegroundColor Cyan
Write-Host "   $PublishFolder" -ForegroundColor White
Write-Host ""
Write-Host "📍 Deployment archive:" -ForegroundColor Cyan
Write-Host "   $ZipPath" -ForegroundColor White
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "🔧 DEPLOYMENT INSTRUCTIONS:" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1️⃣  On Target Server (Windows):" -ForegroundColor Yellow
Write-Host "   • Copy contents of: $PublishFolder" -ForegroundColor White
Write-Host "   • Paste to: C:\Apps\WebApp (or your target path)" -ForegroundColor White
Write-Host ""
Write-Host "2️⃣  Configure appsettings.json:" -ForegroundColor Yellow
Write-Host "   • Update connection strings if needed" -ForegroundColor White
Write-Host "   • Set environment to 'Production'" -ForegroundColor White
Write-Host ""
Write-Host "3️⃣  Create Windows Service (if IIS not used):" -ForegroundColor Yellow
Write-Host "   sc create WebApp binPath= 'C:\Apps\WebApp\WebApp.exe'" -ForegroundColor White
Write-Host "   sc start WebApp" -ForegroundColor White
Write-Host ""
Write-Host "4️⃣  Or configure IIS:" -ForegroundColor Yellow
Write-Host "   • Create new Application Pool (No Managed Code)" -ForegroundColor White
Write-Host "   • Create new Website pointing to published folder" -ForegroundColor White
Write-Host "   • Set binding to port 80/443" -ForegroundColor White
Write-Host ""
Write-Host "5️⃣  Setup SQL Server connection:" -ForegroundColor Yellow
Write-Host "   • Ensure SQL Server is accessible from web server" -ForegroundColor White
Write-Host "   • Connection string: Server=YOUR_SERVER;Database=DB_SAIT;..." -ForegroundColor White
Write-Host ""
Write-Host "6️⃣  Verify deployment:" -ForegroundColor Yellow
Write-Host "   • Navigate to: http://localhost/import/list" -ForegroundColor White
Write-Host "   • Check logs for any errors" -ForegroundColor White
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "📊 APPLICATION INFO:" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Framework: .NET 8.0" -ForegroundColor White
Write-Host "Runtime: win-x64 (Self-contained)" -ForegroundColor White
Write-Host "Configuration: $Environment" -ForegroundColor White
Write-Host "Database: SQL Server (localdb)\mssqllocaldb" -ForegroundColor White
Write-Host "Database Name: DB_SAIT" -ForegroundColor White
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "✅ READY FOR SERVER DEPLOYMENT!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
