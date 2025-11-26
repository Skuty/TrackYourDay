@echo off
setlocal EnableDelayedExpansion

set "VERSION_TAG=%~1"

echo Welcome to TrackYourDay Application Updater!
echo Your Personal Data won't be deleted.

echo ----------------------------------------------------

echo Content of following directory will be deleted: %CD%

pause

echo Deleting all files and subdirectories except Updater.
for /F "delims=" %%A in ('dir /B /A-D') do (
    if not "%%A"=="UpdateApplication.bat" (
        del "%%A"
    )
)
for /D %%D in (*) do (
    rmdir /S /Q "%%D"
)
echo All files and subdirectories deleted except Updater.

echo ----------------------------------------------------

if "%VERSION_TAG%"=="" (
    echo Downloading latest stable Version from GitHub Releases.
    powershell -NoProfile -Command ^
        "$ErrorActionPreference = 'Stop';" ^
        "try {" ^
        "    $latestRelease = Invoke-RestMethod -Uri 'https://api.github.com/repos/Skuty/TrackYourDay/releases/latest' -Headers @{ 'User-Agent' = 'Mozilla/5.0' };" ^
        "    $zipAsset = $latestRelease.assets | Where-Object { $_.name -like 'TrackYourDay*.zip' };" ^
        "    $downloadUrl = $zipAsset.browser_download_url;" ^
        "    Invoke-WebRequest -Uri $downloadUrl -OutFile 'TrackYourDay_NewestRelease.zip';" ^
        "} catch {" ^
        "    Write-Host 'Error downloading release:' $_.Exception.Message;" ^
        "    exit 1;" ^
        "}"
) else (
    echo Downloading version %VERSION_TAG% from GitHub Releases.
    powershell -NoProfile -Command ^
        "$ErrorActionPreference = 'Stop';" ^
        "try {" ^
        "    $release = Invoke-RestMethod -Uri 'https://api.github.com/repos/Skuty/TrackYourDay/releases/tags/%VERSION_TAG%' -Headers @{ 'User-Agent' = 'Mozilla/5.0' };" ^
        "    $zipAsset = $release.assets | Where-Object { $_.name -like 'TrackYourDay*.zip' };" ^
        "    $downloadUrl = $zipAsset.browser_download_url;" ^
        "    Invoke-WebRequest -Uri $downloadUrl -OutFile 'TrackYourDay_NewestRelease.zip';" ^
        "} catch {" ^
        "    Write-Host 'Error downloading release %VERSION_TAG%:' $_.Exception.Message;" ^
        "    exit 1;" ^
        "}"
)

if %ERRORLEVEL% neq 0 (
    echo ----------------------------------------------------
    echo Download failed. Please try again later or download manually from:
    echo https://github.com/Skuty/TrackYourDay/releases
    echo ----------------------------------------------------
    pause
    exit /b 1
)

echo Extracting TrackYourDay_NewestRelease.zip.
powershell -Command "Expand-Archive -Path 'TrackYourDay_NewestRelease.zip' -DestinationPath . -Force"
echo Extraction completed.

echo ----------------------------------------------------

echo Deleting TrackYourDay_NewestRelease.zip
del TrackYourDay_NewestRelease.zip
echo Zip archive deleted.

echo ----------------------------------------------------

echo TrackYourDay Updated.

echo ----------------------------------------------------

echo Process completed successfully!

echo ----------------------------------------------------

echo Now TrackYourDay will be started.
start "" "%CD%\TrackYourDay.MAUI.exe"

pause