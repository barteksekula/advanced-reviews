@ECHO OFF
SETLOCAL

SET CONFIGURATION=Debug

IF "%1"=="Release" (SET CONFIGURATION=Release)

dotnet pack -c %CONFIGURATION% /p:CheckEolTargetFramework=false Advanced.CMS.AdvancedReviews.sln

EXIT /B %errorlevel%
