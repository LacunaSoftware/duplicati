using System;
using System.Linq;
using System.Collections.Generic;
using Duplicati.Server.Serialization;
using System.IO;
using Duplicati.Library.ENotariado;
using Duplicati.Library.Localization.Short;

namespace Duplicati.Server.WebServer.RESTMethods
{
    public class ENotariado : IRESTMethodGET, IRESTMethodPOST, IRESTMethodDocumented
    {

        public void GET(string key, RequestInfo info)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "backup-list":
                    info.BodyWriter.OutputOK(ENotariadoConnection.GetStoredBackupNames());
                    return;

                case "backup-password":
                    info.BodyWriter.OutputOK(new { Password = ENotariadoConnection.GetBackupPassword().GetAwaiter().GetResult() });
                    return;

                default:
                    info.ReportClientError(LC.L(@"No such action"), System.Net.HttpStatusCode.NotFound);
                    return;
            }
        }

        public void POST(string key, RequestInfo info)
        {
            var input = info.Request.Form;
            var enrolledErrorMessage = new { Message = "Houve um erro na comunicação com o e-Notariado. Tente novamente mais tarde." };
            var verifiedErrorMessage = new { Message = "O agente ainda não foi cadastrado no e-Notariado." };
            var failedVerification   = new { Message = "A aplicação ainda não foi cadastrada no Portal Backup e-Notariado." };
            switch ((key ?? "").ToLowerInvariant())
            {
                case "verify":
                    var resultVerify = Program.VerifyENotariado().GetAwaiter().GetResult();

                    if (resultVerify)
                        info.OutputOK();
                    else
                        info.OutputError(item: failedVerification);
                    return;

                case "reset":
                    var result = Program.ResetENotariado().GetAwaiter().GetResult();
                    if (result == (ENotariadoStatus.Verified | ENotariadoStatus.Enrolled))
                        info.OutputOK();
                    else if (result == (ENotariadoStatus.Enrolled))
                        info.OutputError(item: verifiedErrorMessage);
                    else if (result == (ENotariadoStatus.None))
                        info.OutputError(item: enrolledErrorMessage);
                    return;

                default:
                    info.ReportClientError(LC.L(@"No such action"), System.Net.HttpStatusCode.NotFound);
                    return;
            }
        }

        public string Description { get { return "Operações relacionadas a e-Notariado"; } }

        public IEnumerable<KeyValuePair<string, Type>> Types
        {
            get
            {
                return new KeyValuePair<string, Type>[] {
                };
            }
        }
    }
}

