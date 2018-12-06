@echo off

rmdir /s /q Duplicati
del /q Duplicati.msi
del /q Duplicati-32bit.msi

IF NOT EXIST "%1" (
	echo File not found, please supply a zip file with the build as the first argument
	goto EXIT
)

call "%VS140COMNTOOLS%vsvars32.bat"

"C:\Program Files\7-Zip\7z.exe" x -oDuplicati %1

copy Microsoft_VC141_CRT_x64.msm "Duplicati/Microsoft_VC141_CRT_x64.msm"
copy Microsoft_VC141_CRT_x86.msm "Duplicati/Microsoft_VC141_CRT_x86.msm"
copy Microsoft_VC141_MFC_x64.msm "Duplicati/Microsoft_VC141_MFC_x64.msm"
copy Microsoft_VC141_MFC_x86.msm "Duplicati/Microsoft_VC141_MFC_x86.msm"

rmdir /s /q obj
rmdir /s /q bin

copy UpgradeData.wxi UpgradeData.wxi.orig
UpdateVersion.exe Duplicati\Duplicati.GUI.TrayIcon.exe UpgradeData.wxi

msbuild /property:Configuration=Release /property:Platform=x64
move "bin\x64\Release\Backup e-Notariado.msi" "backup-enotariado.msi"

msbuild /property:Configuration=Release /property:Platform=x86
move "bin\x86\Release\Backup e-Notariado.msi" "backup-enotariado_32bit.msi"

copy UpgradeData.wxi.orig UpgradeData.wxi
del UpgradeData.wxi.orig

rmdir /s /q Duplicati

:EXIT