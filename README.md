# Duplicati

## Building release for prod

- Create the file `changelog-news.txt` and list the changes made in the new version
- Have the private key of the updater under the directory defined in `UPDATER_KEYFILE` (defined in the `build-release.sh` file).
- Check both FTPS hostname and username
- Execute the `build-release.sh` script
- When it asks for you to sign the DLLs and EXEs, open a Dev Command Prompt, cd to `<repository_path>/Updates/build/<build_dir>` and sign them using `signtool.exe sign /n <CERT NAME> /tr http://timestamp.digicert.com *.dll *.exe`.
- Go back to `build-release.sh`, press enter and let it finish the release.

## Building Windows installer

- Make sure you have 7-Zip installed on `C:\Program Files\7-Zip`, if it is in another directory, edit the line 14 of the file `<repository_path>/Duplicati/Installer/Windows/build-msi.bat` with the correct directory.
- Open the  [Developer Command Prompt for Visual Studio](https://docs.microsoft.com/en-us/dotnet/framework/tools/developer-command-prompt-for-vs)
- Change directory to `<repository_path>/Installer/Windows`
- Execute `build-msi.bat <zip file path>`, where `<zip file path>` is the latest zip file, compressed when building the release as explained above, can be found on either the cdn server or under the `<repo path>/Updates/build/<build_dir>` directory
- Sign the installers using the command described above.
