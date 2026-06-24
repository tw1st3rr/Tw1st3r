@echo off
set "SKUA_PLUGINS=%APPDATA%\Skua\plugins"
set "DLL=bin\Release\net10.0-windows\Tw1st3rPlugin.dll"
set "JSON=class_recommendations.json"

if not exist "%DLL%" ( echo Build first. & pause & exit /b 1 )
if not exist "%SKUA_PLUGINS%" mkdir "%SKUA_PLUGINS%"

copy /Y "%DLL%" "%SKUA_PLUGINS%\Tw1st3rPlugin.dll"
copy /Y "%JSON%" "%SKUA_PLUGINS%\class_recommendations.json"
echo.
echo Installed Tw1st3r Plugin + recommendations to:
echo   %SKUA_PLUGINS%
pause
