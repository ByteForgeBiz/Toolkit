@ECHO OFF
setlocal enabledelayedexpansion

:: If no argument was passed, set the default configuration to Debug
set cfg=%1
if "!cfg!"=="" set cfg=Debug

echo "  ___      _ _    _ _                  "
echo " | _ )_  _(_) |__| (_)_ _  __ _        "
echo " | _ \ || | | / _` | | ' \/ _` |_ _ _  "
echo " |___/\_,_|_|_\__,_|_|_||_\__, (_)_)_) "
echo "                          |___/        "
echo.
echo Configuration: !cfg!
echo.

:: Find all solution files in the current directory
set SOLUTION_COUNT=0
set SOLUTIONS=
for %%f in ("%~dp0*.sln") do (
    set /a SOLUTION_COUNT+=1
    set SOLUTIONS=!SOLUTIONS! "%%f"
    echo Found solution: %%~nxf
)

if !SOLUTION_COUNT! equ 0 (
    echo No solution files found in the current directory.
    echo Please ensure there are .sln files in the same folder as this script.
    exit /b 1
)

echo.
echo Found !SOLUTION_COUNT! solution(s) to build.
echo.

echo Cleaning `obj` and `bin` folders...
for /r /d %%i in (obj) do if exist "%%i"       rmdir /s /q "%%i"       2> nul
for /r /d %%i in (bin) do if exist "%%i\!cfg!" rmdir /s /q "%%i\!cfg!" 2> nul

:: Create Logs directory if it doesn't exist
if not exist "%~dp0Logs" mkdir "%~dp0Logs"

echo Building solutions...
set BUILD_FAILED=0
for %%s in (!SOLUTIONS!) do (
    echo.
    echo ========================================
    echo Building: %%~nxs
    echo ========================================
    if not exist "%~dp0Logs\Build" mkdir "%~dp0Logs\Build"
    call :BuildSolution %%s > "%~dp0Logs\Build\%%~ns.log" 2>&1
    if !ERRORLEVEL! neq 0 (
        echo Build failed for: %%~nxs
        set BUILD_FAILED=1
    ) else (
        echo Build succeeded for: %%~nxs
    )
)

echo Cleaning `obj` folder...
for /r /d %%i in (obj) do if exist "%%i"       rmdir /s /q "%%i"       2> nul

if !BUILD_FAILED! equ 1 (
    echo.
    echo "  ___      _ _    _     ___     _ _        _ _  "
    echo " | _ )_  _(_) |__| |   | __|_ _(_) |___ __| | | "
    echo " | _ \ || | | / _` |   | _/ _` | | / -_) _` |_| "
    echo " |___/\_,_|_|_\__,_|   |_|\__,_|_|_\___\__,_(_) "
    echo.
    echo One or more builds failed. Check the log files in "%~dp0Logs\" for details.
    exit /b 1
)

echo.
echo All solutions built successfully!
set cfg=
exit /b 0

:BuildSolution
set PROJECT_FILE=%1
if "%PROJECT_FILE%"=="" (
    echo No project file specified.
    exit /b 1
)

echo Searching for MSBuild...

:: Prefer vswhere when available - it handles newer Visual Studio layouts like 18/2026
set VSWHERE_PATH=
for /f "delims=" %%i in ('where vswhere 2^>nul') do (
    if not defined VSWHERE_PATH set "VSWHERE_PATH=%%i"
)

if defined VSWHERE_PATH (
    echo Found vswhere: !VSWHERE_PATH!
    for /f "usebackq delims=" %%i in (`"!VSWHERE_PATH!" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not defined MSBUILD_PATH set MSBUILD_PATH=%%i
    )
    if defined MSBUILD_PATH goto :found_msbuild
)

:: Try to find Visual Studio 2026 installation
set VS2026_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files\Microsoft Visual Studio\18\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set VS2026_PATH=C:\Program Files\Microsoft Visual Studio\18\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find Visual Studio 2022 installation
set VS2022_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set VS2022_PATH=C:\Program Files\Microsoft Visual Studio\2022\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find Visual Studio 2019 installation
set VS2019_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe" (
        set VS2019_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\%%e\MSBuild\Current\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find Visual Studio 2017 installation
set VS2017_PATH=
for %%e in (Enterprise Professional Community) do (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\%%e\MSBuild\15.0\Bin\MSBuild.exe" (
        set VS2017_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\%%e\MSBuild\15.0\Bin\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find older MSBuild from .NET Framework
set NETFX_MSBUILD=
for %%v in (4.0.30319 14.0 12.0) do (
    if exist "C:\Windows\Microsoft.NET\Framework\v%%v\MSBuild.exe" (
        set NETFX_MSBUILD=C:\Windows\Microsoft.NET\Framework\v%%v\MSBuild.exe
        goto :found_msbuild
    )
)

:: Try to find MSBuild in the path
where msbuild >nul 2>&1
if %ERRORLEVEL% equ 0 (
    set MSBUILD_PATH=msbuild
    goto :found_msbuild
)

echo ERROR: MSBuild not found. Please install Visual Studio or .NET Framework SDK.
exit /b 1

:found_msbuild
if defined MSBUILD_PATH (
    echo Found MSBuild via vswhere
) else if defined VS2026_PATH (
    echo Found MSBuild from Visual Studio 2026
    set MSBUILD_PATH=!VS2026_PATH!
) else if defined VS2022_PATH (
    echo Found MSBuild from Visual Studio 2022
    set MSBUILD_PATH=!VS2022_PATH!
) else if defined VS2019_PATH (
    echo Found MSBuild from Visual Studio 2019
    set MSBUILD_PATH=!VS2019_PATH!
) else if defined VS2017_PATH (
    echo Found MSBuild from Visual Studio 2017
    set MSBUILD_PATH=!VS2017_PATH!
) else if defined NETFX_MSBUILD (
    echo Found MSBuild from .NET Framework
    set MSBUILD_PATH=!NETFX_MSBUILD!
) else (
    echo Found MSBuild in PATH
)

echo Using MSBuild: !MSBUILD_PATH!
set "MSBUILD_EXE=!MSBUILD_PATH!"
set "MSBUILD_CONFIGURATION=!cfg!"

echo Running NuGet restore...
set "MSBUILD_MODE=Restore"
call :RunMSBuild
if !ERRORLEVEL! neq 0 (
    echo Restore failed.
    exit /b !ERRORLEVEL!
)

echo.
echo Building AnyCPU version...
set "MSBUILD_MODE=Build"
call :RunMSBuild
if %ERRORLEVEL% neq 0 (
    echo Build failed for AnyCPU.
    exit /b %ERRORLEVEL%
)

echo.
echo Build complete successfully.
exit /b 0

:RunMSBuild
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$msbuild = $env:MSBUILD_EXE; $project = $env:PROJECT_FILE; $cfg = $env:MSBUILD_CONFIGURATION; $mode = $env:MSBUILD_MODE; $args = New-Object System.Collections.Generic.List[string]; $args.Add(('\"{0}\"' -f $project)); if ($mode -eq 'Restore') { $args.Add('/t:Restore') }; $args.Add('/p:Configuration=' + $cfg); $args.Add('/p:Platform=\"Any CPU\"'); $args.Add('/p:RuntimeIdentifiers=win'); if ($mode -eq 'Build') { $args.Add('/v:m') }; $psi = New-Object System.Diagnostics.ProcessStartInfo; $psi.FileName = $msbuild; $psi.Arguments = [string]::Join(' ', $args); $psi.UseShellExecute = $false; $envVars = $psi.EnvironmentVariables; foreach ($key in @($envVars.Keys)) { if ($key -ieq 'PATH') { $envVars.Remove($key) } }; $pathValue = [System.Environment]::GetEnvironmentVariable('PATH'); if (-not $pathValue) { $pathValue = [System.Environment]::GetEnvironmentVariable('Path') }; if ($pathValue) { $envVars['PATH'] = $pathValue }; $process = [System.Diagnostics.Process]::Start($psi); $process.WaitForExit(); exit $process.ExitCode"
exit /b %ERRORLEVEL%
