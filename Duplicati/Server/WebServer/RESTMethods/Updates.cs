//  Copyright (C) 2015, The Duplicati Team
//  http://www.duplicati.com, info@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using Duplicati.Library.ENotariado;
using Duplicati.Library.Localization.Short;
using System;

namespace Duplicati.Server.WebServer.RESTMethods
{
    public class Updates : IRESTMethodPOST
    {
        public void POST(string key, RequestInfo info)
        {

            if (!info.Request.RemoteEndPoint.Address.Equals(System.Net.IPAddress.Parse("127.0.0.1"))) {
                info.ReportClientError("Operações de atualizações só podem ser executadas na máquina em que o Módulo Agente está instalado.");
                return;
            }

            switch ((key ?? "").ToLowerInvariant())
            {
                case "check":
                    Program.UpdatePoller.CheckNow();
                    info.OutputOK();
                    return;

                case "install":
                    Program.UpdatePoller.InstallUpdate();
                    info.OutputOK();
                    return;

                case "activate":
                    if (Program.WorkThread.CurrentTask != null || Program.WorkThread.CurrentTasks.Count != 0)
                    {
                        info.ReportClientError("Não é possível atualizar enquanto uma tarefa está sendo executada ou agendada");
                    }
                    else
                    {
                        if (Library.Utility.Utility.IsClientWindows)
                        {
                            var resultService = ServiceManager.Restart();
                            if (resultService)
                                info.OutputOK();
                            else
                                info.OutputError();
                            return;
                        }
                        info.OutputError(item: new { Message = "Atualmente não é possível reiniciar a aplicação em outro SO além de Windows" });
                    }
                    return;
                
                default:
                    info.ReportClientError(LC.L(@"No such action"), System.Net.HttpStatusCode.NotFound);
                    return;
            }
        }
    }
}

