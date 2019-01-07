using System;
using System.Linq;
using System.Collections.Generic;
using Duplicati.Server.Serialization;
using System.IO;
using Duplicati.Library;
using Duplicati.Library.Localization.Short;

namespace Duplicati.Server.WebServer.RESTMethods
{
    public class Enotariado : IRESTMethodGET, IRESTMethodPOST, IRESTMethodDocumented
    {

        public void GET(string key, RequestInfo info)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "backup-list":
                    info.BodyWriter.OutputOK(Library.Enotariado.Main.GetStoredBackupNames());
                    return;

                case "app-enrollment":
                    var query = info.Request.QueryString;
                    var check = query.Contains("check");
                    var force = false;
                    var body1x1 = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAFiUAABYlAUlSJPAAAAAMSURBVBhXY/j//z8ABf4C/qc1gYQAAAAASUVORK5CYII=");
                    var body2x2 = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAFiUAABYlAUlSJPAAAAAVSURBVBhXY/z//z8DAwMTEDMwMAAAJAYDAbrboo8AAAAASUVORK5CYII=");
                    byte[] body = null;

                    if (check)
                    {
                        body = body1x1;
                    }
                    else
                    {
                        if (!query.Contains("id") || !query.Contains("ticket"))
                        {
                            info.ReportClientError("Missing Application ID or Access Ticket");
                            return;
                        }

                        var id = query["id"].Value;
                        var ticket = query["ticket"].Value;
                        if (query.Contains("force"))
                            force = Library.Utility.Utility.ParseBool(query["force"].Value, false);

                    

                        // if re-enroll is forced or
                        // if EnotariadoIsVerified == false, it means we are not enrolled or enrolled but not verified
                        //   either way, reset and start again
                        if (force || !Program.EnotariadoIsVerified)
                        {
                            Program.ResetEnotariadoConfig();
                            body = body1x1;
                            _ = Program.FinishPreApprovedEnrollmentEnotariado(id, ticket);
                        }
                        else
                        {
                            // already enrolled
                            body = body2x2;
                        }
                    }

                    info.Response.ContentType = "image/png";
                    info.Response.Body.Write(body, 0, body.Count());
                    info.Response.Status = System.Net.HttpStatusCode.OK;
                    info.Response.Send();

                    return;

                default:
                    info.ReportClientError(LC.L(@"No such action"), System.Net.HttpStatusCode.NotFound);
                    return;
            }
        }

        public void POST(string key, RequestInfo info)
        {
            var input = info.Request.Form;
            var enrolledErrorMessage = new { Message = "A aplicação não está cadastrada no Módulo Gerenciador do Backup e-notariado." };
            var failedVerification   = new { Message = "A aplicação não foi aprovada no Módulo Gerenciador do Backup e-notariado." };
            switch ((key ?? "").ToLowerInvariant())
            {
                case "verify":
                    if (Program.EnotariadoIsVerified)
                        info.OutputOK();
                    else
                        info.OutputError(item: failedVerification);
                    return;

                case "reset":
                    Program.ResetEnotariadoConfig();
                    info.OutputOK();
                    return;

                default:
                    info.ReportClientError(LC.L(@"No such action"), System.Net.HttpStatusCode.NotFound);
                    return;
            }
        }

        public string Description { get { return "Operações relacionadas ao e-notariado"; } }

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

