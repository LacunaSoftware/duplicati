# Duplicati

### Building installer of custom version on windows 

- On Visual Studio, set the Solution Configurations to build for Release and build the solution.
- Make sure you have 7-Zip installed on `C:\Program Files\7-Zip`, if it is in another directory, edit the line 14 of the file `<repository folder>/Duplicati/Installer/Windows/build-msi.bat` with the correct directory.
- Copy the `<repository folder>/Duplicati/Server/webroot` folder into `<repository folder>/Duplicati/GUI/Duplicati.GUI.TrayIcon/bin/Release`
- Open the directory `<repository folder>/Duplicati/GUI/Duplicati.GUI.TrayIcon/bin/Release`
- Compress everything into a `.zip` file.
- Open the  [Developer Command Prompt for Visual Studio](https://docs.microsoft.com/en-us/dotnet/framework/tools/developer-command-prompt-for-vs)
- Change directory to `<repository folder>/Installer/Windows`
- Execute `build-msi.bat <zip file path>`
