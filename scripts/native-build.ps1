# Builds the native (C++) GDExtensions under native/ into native/bin/, with the CMake,
# Ninja and MSVC that come with the Visual Studio Build Tools (C++ workload). The first
# build downloads godot-cpp and takes a few minutes; later builds are incremental.
#
# Stop the server first: Windows locks a DLL while a process has it loaded.
param(
    [switch]$Clean
)

$root = Split-Path $PSScriptRoot -Parent
$build = Join-Path $root "native\build"
$installer = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
$vs = & (Join-Path $installer "vswhere.exe") -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath

if (-not $vs) {
    Write-Error "No Visual Studio install with the C++ tools. Install the Build Tools with the 'Desktop development with C++' workload."
    exit 1
}

$vcvars = Join-Path $vs "VC\Auxiliary\Build\vcvars64.bat"
$cmake = Join-Path $vs "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
$ninja = Join-Path $vs "Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe"

if ($Clean -and (Test-Path $build)) {
    Remove-Item -Recurse -Force $build
}

# The compiler only works in the environment vcvars64 sets up, and that lives in a cmd
# shell, so configure and build run in that same shell. vcvars finds vswhere on PATH.
$env:PATH = "$installer;$env:PATH"
$configure = "`"$cmake`" -S `"$root\native`" -B `"$build`" -G Ninja -DCMAKE_MAKE_PROGRAM=`"$ninja`" -DCMAKE_BUILD_TYPE=Release -DGODOTCPP_TARGET=template_release"
$compile = "`"$cmake`" --build `"$build`""
cmd /c "`"$vcvars`" >nul && $configure && $compile"
exit $LASTEXITCODE
