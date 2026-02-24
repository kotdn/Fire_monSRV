@echo off
REM WebApp Build & Run Script

echo =================================
echo   WebApp — ASP.NET Core
echo =================================
echo.

echo Restoring NuGet packages...
dotnet restore

echo.
echo Running migrations...
dotnet ef migrations add InitialCreate --force 2>nul
dotnet ef database update

echo.
echo Starting application...
echo.
echo [Server running on http://localhost:5000]
echo [Default routes:]
echo   /account/login       — Login page
echo   /home                — Main page (user only)
echo   /admin               — Admin panel (admin only)
echo.
echo [Test credentials:]
echo   admin / adminpass    — Administrator account
echo   user / userpass      — Regular user account
echo.

dotnet run
