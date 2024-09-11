@echo off
setlocal EnableDelayedExpansion

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

echo Downloading newest Version from GitHub Releases.
powershell -NoProfile -Command ^
    "$latestRelease = Invoke-RestMethod -Uri 'https://api.github.com/repos/Skuty/TrackYourDay/releases/latest' -Headers @{ 'User-Agent' = 'Mozilla/5.0' };" ^
    "$zipAsset = $latestRelease.assets | Where-Object { $_.name -like 'TrackYourDay*.zip' };" ^
    "$downloadUrl = $zipAsset.browser_download_url;" ^
    "Invoke-WebRequest -Uri $downloadUrl -OutFile 'TrackYourDay_NewestRelease.zip';"

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

echo TrackYourDay will be added to Autostart.

pause

set startupFolder="%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set trackYourDayShortcut=%startupFolder%\TrackYourDay.lnk

powershell -Command ^
    "$WScriptShell = New-Object -ComObject WScript.Shell;" ^
    "$Shortcut = $WScriptShell.CreateShortcut('%trackYourDayShortcut%');" ^
    "$Shortcut.TargetPath = '%CD%\TrackYourDay.exe';" ^
    "$Shortcut.Save();"

echo TrackYourDay has been added to Autostart.

echo ----------------------------------------------------

echo Process completed successfully!

echo ----------------------------------------------------

echo Now TrackYourDay will be started.
start "" "%CD%\TrackYourDay.MAUI.exe"

pause