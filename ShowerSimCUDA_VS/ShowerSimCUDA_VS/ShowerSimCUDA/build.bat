@echo off
setlocal
where nvcc >nul 2>nul
if errorlevel 1 (
    echo nvcc was not found. Install NVIDIA CUDA Toolkit or add it to PATH.
    exit /b 1
)
if not exist x64\Release mkdir x64\Release
nvcc -std=c++17 -O3 -lineinfo src\main.cu -o x64\Release\ShowerSimCUDA.exe
endlocal
