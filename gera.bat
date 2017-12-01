@echo off
for /f "delims=" %%F in ('dir *-audio.mjr /b /od') do set file=%%F
del audio.opus /q
C:\"Program Files"\"Janus WebRTC Gateway"\mingw64\bin\janus-pp-rec %file% audio.opus


for /f "delims=" %%F in ('dir *-video.mjr /b /od') do set file=%%F
del video.webm /q
C:\"Program Files"\"Janus WebRTC Gateway"\mingw64\bin\janus-pp-rec %file% video.webm

del output.mkv /q
mkvmerge -o output.mkv video.webm audio.opus

pause