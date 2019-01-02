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
using System.IO;
using NUnit.Framework;
using Duplicati.Library;
using System.Security.Cryptography.X509Certificates;

namespace Duplicati.UnitTest
{
    public class ENotariadoLibrary : BasicSetupHelper
    {
        [OneTimeSetUp]
        public override void PrepareSourceData()
        {
            base.PrepareSourceData();

            Directory.CreateDirectory(DATAFOLDER);
            Directory.CreateDirectory(TARGETFOLDER);
        }

        [Test]
        [Category("Cryptography")]
        public void CryptoUtils()
        {
            var storeLocation = StoreLocation.CurrentUser;
            byte[] randomBytes = new byte[50];
            Random rnd = new Random();

            // Creating certificate
            var cert = Library.ENotariado.CryptoUtils.CreateSelfSignedCertificate(storeLocation);

            // Retrieving certificate
            var cert2 = Library.ENotariado.CryptoUtils.GetCertificate(storeLocation, cert.Thumbprint);

            // Testing that both are the same
            Assert.AreEqual(cert, cert2);

            // Testing that both can sign and verify random bytes
            rnd.NextBytes(randomBytes);
            var signedData1 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(randomBytes, cert);
            var signedData2 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(randomBytes, cert2);

            var verify1 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(randomBytes, signedData1, cert);
            var verify2 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(randomBytes, signedData2, cert2);

            Assert.AreEqual(signedData1, signedData2);
            Assert.True(verify1);
            Assert.True(verify2);

            // Deleting certificate
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadWrite);
            try
            {
                store.Remove(cert);
            }
            finally
            {
                store.Close();
            }
        }
    }
}

