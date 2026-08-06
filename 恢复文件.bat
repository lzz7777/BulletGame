@echo off
for /f "delims=" %%i in ('git ls-files "./Assets/Resources/* SDF.asset"') do echo »Ö¸´:%%i & git update-index --no-assume-unchanged "%%i"
for /f "delims=" %%i in ('git ls-files "./Assets/Resources/* SDFB.asset"') do echo »Ö¸´:%%i & git update-index --no-assume-unchanged "%%i"
pause