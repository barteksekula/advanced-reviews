@ECHO OFF
SETLOCAL

dotnet run --project "%~dp0src\Headless.AppHost\Headless.AppHost.csproj" --launch-profile Headless.AppHost

EXIT /B %ERRORLEVEL%
