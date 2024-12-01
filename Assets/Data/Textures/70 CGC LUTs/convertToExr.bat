@echo off
setlocal enabledelayedexpansion

for /r %%f in (*.cube) do (
    vertopal convert "%%f" --to exr
)
pause