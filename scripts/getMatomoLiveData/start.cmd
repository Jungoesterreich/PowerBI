@echo off
setlocal

REM === Set working directory ===
cd /d C:\itxwork\getMatomoLiveData

REM === Optional: logs folder ===
if not exist logs (
    mkdir logs
)

REM === Start all scripts minimized with log output ===
start /min cmd /C "node getlivedata-jungoe.js >> logs\jungoe.log 2>&1"
start /min cmd /C "node getlivedata-lesen.js >> logs\lesen.log 2>&1"
start /min cmd /C "node getlivedata-msdigi.js >> logs\msdigi.log 2>&1"
start /min cmd /C "node getlivedata-spdigi.js >> logs\spdigi.log 2>&1"
start /min cmd /C "node getlivedata-luxdigi.js >> logs\luxdigi.log 2>&1"
start /min cmd /C "node getlivedata-joedigi.js >> logs\joedigi.log 2>&1"
start /min cmd /C "node getlivedata-topicdigi.js >> logs\topicdigi.log 2>&1"

endlocal
