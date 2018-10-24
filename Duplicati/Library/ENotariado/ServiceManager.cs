using System;

namespace Duplicati.Library.ENotariado
{
    public static class ServiceManager
    {
        private static string LOGTAG = "ENotariadoServiceManager";
        public static bool Restart()
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var exec = System.IO.Path.Combine(path, "Gerenciador e-Notariado.exe");
            var cmdargs = " restart";

            if (!System.IO.File.Exists(exec))
            {
                Logging.Log.WriteErrorMessage(LOGTAG, "RestartService", null, string.Format("File not found {0}", exec));
                return false;
            }

            try
            {
                Logging.Log.WriteInformationMessage(LOGTAG, "RestartService", "Restarting service BackupENotariado");                    
                Logging.Log.WriteInformationMessage(LOGTAG, "RestartService", string.Format("Starting process {0} with cmd args {1}", exec, cmdargs));

                var pr = new System.Diagnostics.ProcessStartInfo(exec, cmdargs)
                {
                    WorkingDirectory = path,
                    Verb = "runas",
                    UseShellExecute = true                  
                };

                System.Diagnostics.Process.Start(pr);
            }
            catch (Exception ex)
            {
                Logging.Log.WriteErrorMessage(LOGTAG, "RestartService", null, string.Format("Process has failed with error message: {0}", ex));
                return false;
            }

            return true;
        }
    }
}
