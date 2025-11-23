@echo off
echo Destek Sistemi Paketleme Aracý
echo ==============================

echo 1. Backend'i publish ediliyor...
cd source\repos\DestekAPI\DestekAPI
dotnet publish -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo Backend publish iþlemi baþarýsýz oldu!
    exit /b %ERRORLEVEL%
)

echo 2. Frontend paketleniyor...
cd ..\..\..\..\destek-frontend
npm install
if %ERRORLEVEL% NEQ 0 (
    echo NPM baðýmlýlýklarý yüklenemedi!
    exit /b %ERRORLEVEL%
)

npm run build
if %ERRORLEVEL% NEQ 0 (
    echo React build iþlemi baþarýsýz oldu!
    exit /b %ERRORLEVEL%
)

npm run dist
if %ERRORLEVEL% NEQ 0 (
    echo Electron paketleme iþlemi baþarýsýz oldu!
    exit /b %ERRORLEVEL%
)

echo ==============================
echo Paketleme iþlemi tamamlandý!
echo Uygulama dist klasöründe hazýr.
pause