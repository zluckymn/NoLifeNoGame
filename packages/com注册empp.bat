set pa=%cd%
copy %pa%\empp.dll C:\Windows\System32\empp.dll
regsvr32 empp.dll
pause

