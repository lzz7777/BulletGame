@echo off
for /f "delims=" %%i in ('git ls-files "./Assets/Resources/* SDFB.asset"') do echo ºöÂÔ:%%i & git update-index --assume-unchanged "%%i"
pause