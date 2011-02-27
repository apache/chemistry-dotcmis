@echo off

rem This batch file creates a release.
rem It requires Cygwin.

set DOTCMISZIP=dotcmis-0.1.zip

echo Building...
cd DotCMIS
call build.bat
cd ..

echo Creating release directory...
rmdir /S /Q release
mkdir release

echo Copying readme, etc...
copy LICENSE release
copy NOTICE release
copy DEPENDENCIES release
copy README release

echo Copying binaries ...
copy DotCMIS\bin\Release\DotCMIS.dll release
copy DotCMIS\doc\DotCMISDoc.chm release

echo Copying source...
mkdir release\source
xcopy DotCMIS release\source /E
rmdir /S /Q release\source\bin
rmdir /S /Q release\source\obj
rmdir /S /Q release\source\doc

echo Creating release file...
del %DOTCMISZIP%
cd release
zip -r  ..\%DOTCMISZIP% *
cd ..

echo Signing release file...
gpg --armor --output %DOTCMISZIP%.asc --detach-sig %DOTCMISZIP%
gpg --print-md MD5 %DOTCMISZIP% > %DOTCMISZIP%.md5
gpg --print-md SHA512 %DOTCMISZIP% > %DOTCMISZIP%.sha

