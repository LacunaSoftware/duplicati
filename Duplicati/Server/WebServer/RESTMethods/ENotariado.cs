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
            var enrolledErrorMessage = new { Message = LC.L(@"The application is not enrolled. An unexpected error happened") };
            var verifiedErrorMessage = new { Message = LC.L(@"The application is enrolled but not verified in e-Notariado servers") };
            switch ((key ?? "").ToLowerInvariant())
            {
                case "verify":
                    var result = Program.InitializeENotariado().GetAwaiter().GetResult();
                    if ((result & ENotariadoStatus.Verified) == ENotariadoStatus.Verified)
                        Program.ENotariadoIsVerified = true;

                    if (result == (ENotariadoStatus.Verified | ENotariadoStatus.Enrolled))
                        info.OutputOK();
                    else if (result == (ENotariadoStatus.Enrolled))
                        info.OutputError(item: verifiedErrorMessage);
                    else if (result == (ENotariadoStatus.None))
                        info.OutputError(item: enrolledErrorMessage);
                    return;

                case "reset":
                    result = Program.ResetENotariado().GetAwaiter().GetResult();
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

