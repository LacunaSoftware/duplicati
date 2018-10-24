using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WindowsServiceManager
{
    class Program
    {
        private static readonly string ServiceName = Duplicati.WindowsService.ServiceControl.SERVICE_NAME;
        private static readonly double TimeoutMilliSeconds = 30000;

        [STAThread]
        public static int Main(string[] args)
        {
            return Duplicati.Library.AutoUpdater.UpdaterManager.RunFromMostRecent(typeof(Program).GetMethod("RealMain"), args, Duplicati.Library.AutoUpdater.AutoUpdateStrategy.Never);
        }

        public static void RealMain(string[] args)
        {
            var stop = args != null && args.Any(x => string.Equals("stop", x, StringComparison.OrdinalIgnoreCase));
            var restart = args != null && args.Any(x => string.Equals("restart", x, StringComparison.OrdinalIgnoreCase));
            var start = args != null && args.Any(x => string.Equals("start", x, StringComparison.OrdinalIgnoreCase));

            ServiceController service = new ServiceController(ServiceName);
            TimeSpan timeout = TimeSpan.FromMilliseconds(TimeoutMilliSeconds);

            if (restart || stop)
            {
                int millisec1 = Environment.TickCount;
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                if (restart)
                {
                    // count the rest of the timeout
                    int millisec2 = Environment.TickCount;
                    timeout = TimeSpan.FromMilliseconds(333333 - (millisec2 - millisec1));

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }                
            }
            else if (start)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
        }
    }
}
