@echo off

rem
rem    Licensed to the Apache Software Foundation (ASF) under one
rem    or more contributor license agreements.  See the NOTICE file
rem    distributed with this work for additional information
rem    regarding copyright ownership.  The ASF licenses this file
rem    to you under the Apache License, Version 2.0 (the
rem    "License"); you may not use this file except in compliance
rem    with the License.  You may obtain a copy of the License at
rem
rem      http://www.apache.org/licenses/LICENSE-2.0
rem
rem    Unless required by applicable law or agreed to in writing,
rem    software distributed under the License is distributed on an
rem    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
rem    KIND, either express or implied.  See the License for the
rem    specific language governing permissions and limitations
rem    under the License.
rem

rem This batch file creates a release.
rem It requires Cygwin.

set DOTCMISVERSION=0.3
set DOTCMISZIP=chemistry-dotcmis-%DOTCMISVERSION%.zip
set DOTCMISRC=RC1

set CYGWIN=ntea

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
mkdir release\src
xcopy DotCMIS release\src /E
rmdir /S /Q release\src\bin
rmdir /S /Q release\src\obj
rmdir /S /Q release\src\doc
chmod -R a+rwx release

echo Creating release file...
rmdir /S /Q artifacts
mkdir artifacts

cd release
zip -r  ../artifacts/%DOTCMISZIP% *
cd ..

echo Signing release file...
cd artifacts
gpg --armor --output %DOTCMISZIP%.asc --detach-sig %DOTCMISZIP%
gpg --print-md MD5 %DOTCMISZIP% > %DOTCMISZIP%.md5
gpg --print-md SHA512 %DOTCMISZIP% > %DOTCMISZIP%.sha
gpg --print-md MD5 %DOTCMISZIP%.asc > %DOTCMISZIP%.asc.md5
gpg --print-md SHA512 %DOTCMISZIP%.asc > %DOTCMISZIP%.asc.sha
cd ..

echo Creating RC tag
rem svn copy https://svn.apache.org/repos/asf/chemistry/dotcmis/trunk https://svn.apache.org/repos/asf/chemistry/dotcmis/tags/chemistry-dotcmis-%DOTCMISVERSION%-%DOTCMISRC%

