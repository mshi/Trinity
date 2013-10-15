
REM ***********************************************
REM **     You have change version number ?      **
REM **  Otherwise change before press Enter key  **
REM ***********************************************

PAUSE

REM Clean Directory
rd /S /Q "Sources\bin"
rd /S /Q "Sources\obj"
rd /S /Q "Sources\Logs"
del /F /S /Q "Sources\ItemRules\Log\*"

REM Clean Old Zip file
del Latest-Trinity.zip

REM Create Temp Directory and pull source inside
md Trinity
xcopy /E /Y "Sources\*.cs" "Trinity\"
xcopy /E /Y "Sources\*.dis" "Trinity\"
xcopy /E /Y "Sources\*.xaml" "Trinity\"
xcopy /E /Y "Sources\*.xml" "Trinity\"
xcopy /E /Y "Sources\*.xsd" "Trinity\"
xcopy /E /Y "Sources\*.txt" "Trinity\"

REM Copy to SVN
xcopy /E /Y "C:\UnifiedTrinity\UnifiedTrinity\Sources" "C:\db\svn\Trinity\trunk\Sources\"

REM Zip fresh directory
7za.exe a Latest-Trinity.zip Trinity\ -mx9

REM Clean Temp directory
rd /S /Q Trinity 
