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

            ProgressWriteLine("Issuing self-signed certificate");
            var cert = Library.ENotariado.CryptoUtils.CreateSelfSignedCertificate(storeLocation);

            ProgressWriteLine("Retrieving self-signed certificate from store location");
            var cert2 = Library.ENotariado.CryptoUtils.GetCertificate(storeLocation, cert.Thumbprint);

            ProgressWriteLine("Asserting both certificates are equal");
            Assert.AreEqual(cert, cert2);

            ProgressWriteLine("Generating random bytes and putting them into an array");
            rnd.NextBytes(randomBytes);

            ProgressWriteLine("Signing byte array with both certificates");
            var signature1 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(randomBytes, cert);
            var signature2 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(randomBytes, cert2);

            ProgressWriteLine("Asserting both signatures are equal");
            Assert.AreEqual(signature1, signature2);

            ProgressWriteLine("Verifying signature with both certificates");
            var verify1 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(randomBytes, signature1, cert);
            var verify2 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(randomBytes, signature2, cert2);

            ProgressWriteLine("Asserting both signatures are verified");
            Assert.True(verify1);
            Assert.True(verify2);

            ProgressWriteLine("Removing certificate from store");
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

