Param([string] $configuration = "Release")
$workingDirectory = Get-Location
$zip = "$workingDirectory\packages\7-Zip.CommandLine\18.1.0\tools\7za.exe"

# Set location to the Solution directory
(Get-Item $PSScriptRoot).Parent.FullName | Push-Location

# Version
[xml] $versionFile = Get-Content ".\build\version.props"
$versionNode = $versionFile.SelectSingleNode("Project/PropertyGroup/VersionPrefix")
$version = $versionNode.InnerText

[xml] $dependenciesFile = Get-Content ".\build\dependencies.props"
# CMS dependency
$cmsNode = $dependenciesFile.SelectSingleNode("Project/PropertyGroup/CmsVersion")
$CmsVersion = $cmsNode.InnerText
$cmsParts = $CmsVersion.Split(".")
$cmsMajor = [int]::Parse($cmsParts[0]) + 1
$cmsNextMajorVersion = ($cmsMajor.ToString() + ".0.0")

#cleanup all by dtk folder which is used by tests
Get-ChildItem -Path out\ -Exclude dtk | Remove-Item -Recurse -Force

#copy assets approval reviews
Copy-Item -Path src\Advanced.CMS.ApprovalReviews\ClientResources\ -Destination out\advanced-cms-approval-reviews\$version\ClientResources -recurse -Force
Copy-Item src\Advanced.CMS.ApprovalReviews\module.config out\advanced-cms-approval-reviews
((Get-Content -Path out\advanced-cms-approval-reviews\module.config -Raw).TrimEnd() -Replace '=""', "=`"$version`"" ) | Set-Content -Path out\advanced-cms-approval-reviews\module.config
Set-Location $workingDirectory\out\advanced-cms-approval-reviews
Start-Process -NoNewWindow -Wait -FilePath $zip -ArgumentList "a", "advanced-cms-approval-reviews.zip", "$version", "module.config"
Set-Location $workingDirectory

#copy assets external reviews
Copy-Item -Path src\Advanced.CMS.ExternalReviews\ClientResources\ -Destination out\advanced-cms-external-reviews\$version\ClientResources -recurse -Force
Copy-Item src\Advanced.CMS.ExternalReviews\module.config out\advanced-cms-external-reviews
((Get-Content -Path out\advanced-cms-external-reviews\module.config -Raw).TrimEnd() -Replace '=""', "=`"$version`"" ) | Set-Content -Path out\advanced-cms-external-reviews\module.config
Set-Location $workingDirectory\out\advanced-cms-external-reviews
Start-Process -NoNewWindow -Wait -FilePath $zip -ArgumentList "a", "advanced-cms-external-reviews.zip", "$version", "module.config"
Set-Location $workingDirectory

# Packaging public packages
dotnet pack -c $configuration /p:CheckEolTargetFramework=false /p:PackageVersion=$version /p:CmsVersion=$CmsVersion /p:CmsNextMajorVersion=$cmsNextMajorVersion Advanced.CMS.AdvancedReviews.sln
