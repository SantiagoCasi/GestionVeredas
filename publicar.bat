@echo off
rem Publica SistemaVeredas en MonsterASP (gestorveredas.runasp.net) usando Properties\PublishProfiles\MonsterASP.pubxml
cd /d "%~dp0"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "MSB="
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSB=%%i"
echo MSBuild: %MSB% > publicar.log
if "%MSB%"=="" (
  echo No se encontro MSBuild de Visual Studio >> publicar.log
  echo No se encontro MSBuild de Visual Studio
  pause
  exit /b 1
)
echo Publicando... (puede tardar un par de minutos)
"%MSB%" SistemaVeredas.csproj /restore /p:Configuration=Release /p:DeployOnBuild=true /p:PublishProfile=MonsterASP /v:minimal /nologo >> publicar.log 2>&1
echo EXITCODE=%ERRORLEVEL% >> publicar.log
echo Termino. Resultado en publicar.log
timeout /t 5
