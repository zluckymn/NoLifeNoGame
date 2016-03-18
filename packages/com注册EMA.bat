set pa=%cd%
copy %pa%\EMA_API.dll C:\Windows\System32\EMA_API.dll
regsvr32 EMA_API.dll
pause
