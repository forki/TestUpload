".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion" -Version 4.3.0
".nuget\NuGet.exe" "Install" "Octokit" "-OutputDirectory" "packages" -Version 0.15.0
packages\FAKE\tools\FAKE.exe build.fsx