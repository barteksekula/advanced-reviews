@ECHO OFF
SETLOCAL

SET PATH=.\.ci\tools\;%PATH%

REM Set Release or Debug configuration.
IF "%1"=="Release" (set CONFIGURATION=Release) ELSE (set CONFIGURATION=Debug)
ECHO Testing in %CONFIGURATION%

ECHO Running c# tests
CALL dotnet test test\Advanced.CMS.AdvancedReviews.IntegrationTests\Advanced.CMS.AdvancedReviews.IntegrationTests.csproj -c %CONFIGURATION% /p:CheckEolTargetFramework=false
IF %errorlevel% NEQ 0 EXIT /B %errorlevel%

ECHO Running e2e tests
CALL dotnet test test\Advanced.CMS.AdvancedReviews.E2ETests\Advanced.CMS.AdvancedReviews.E2ETests.csproj -c %CONFIGURATION% /p:CheckEolTargetFramework=false
IF %errorlevel% NEQ 0 EXIT /B %errorlevel%

EXIT /B %ERRORLEVEL%
