
REM ***********************************************
REM **     You have change version number ?      **
REM **  Otherwise change before press Enter key  **
REM ***********************************************

PAUSE

REM Clean Directory
rd /S /Q "Sources\bin"
rd /S /Q "Sources\obj"

REM Clean Old Zip file
del Latest-GilesTrinity.zip

REM Create Temp Directory and pull source inside
md GilesTrinity
xcopy /E /Y "Sources\*.cs" "GilesTrinity\"
xcopy /E /Y "Sources\*.dis" "GilesTrinity\"
xcopy /E /Y "Sources\*.xaml" "GilesTrinity\"

REM Zip fresh directory
7za.exe a Latest-GilesTrinity.zip GilesTrinity\ -mx9

REM Clean Temp directory
rd /S /Q GilesTrinity 