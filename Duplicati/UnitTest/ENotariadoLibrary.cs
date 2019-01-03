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
        private static StoreLocation TestStoreLocation = StoreLocation.CurrentUser;
        private static X509Certificate2 Certificate;

        [OneTimeSetUp]
        public void Setup()
        {
            ProgressWriteLine("Issuing self-signed certificate");
            Certificate = Library.ENotariado.CryptoUtils.CreateSelfSignedCertificate(TestStoreLocation);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            ProgressWriteLine("Removing certificate from store");
            X509Store store = new X509Store(StoreName.My, TestStoreLocation);
            store.Open(OpenFlags.ReadWrite);
            try
            {
                store.Remove(Certificate);
            }
            finally
            {
                store.Close();
            }
            ProgressWriteLine("Asserting certificate cannot be found on store anymore");
            Assert.Throws<Library.ENotariado.CertificateNotFoundException>(() => { Library.ENotariado.CryptoUtils.GetCertificate(TestStoreLocation, Certificate.Thumbprint); });

        }

        [Test]
        [Category("Cryptography")]
        public void CryptoUtils()
        {
            byte[] randomBytes = new byte[50];
            Random rnd = new Random();

            ProgressWriteLine("Retrieving self-signed certificate from store location");
            var retrievedCertificate = Library.ENotariado.CryptoUtils.GetCertificate(TestStoreLocation, Certificate.Thumbprint);

            ProgressWriteLine("Asserting both certificates are equal");
            Assert.AreEqual(Certificate, retrievedCertificate);

            ProgressWriteLine("Generating random bytes and putting them into an array");
            rnd.NextBytes(randomBytes);

            ProgressWriteLine("Signing byte array with both certificates");
            var signature1 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(Certificate, randomBytes);
            var signature2 = Library.ENotariado.CryptoUtils.SignDataWithCertificate(retrievedCertificate, randomBytes);

            ProgressWriteLine("Asserting both signatures are equal");
            Assert.AreEqual(signature1, signature2);

            ProgressWriteLine("Verifying signature with both certificates");
            var verify1 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(Certificate, randomBytes, signature1);
            var verify2 = Library.ENotariado.CryptoUtils.VerifyDataWithCertificate(retrievedCertificate, randomBytes, signature2);

            ProgressWriteLine("Asserting both signatures are verified");
            Assert.IsTrue(verify1);
            Assert.IsTrue(verify2);
        }

        [Test]
        [Category("Connection")]
        public void Connection()
        {
            ProgressWriteLine("Resetting all properties from ENotariadoConnection");
            Library.ENotariado.ENotariadoConnection.ResetData();

            ProgressWriteLine("Asserting IDs are equal to Guid.Empty");
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.ApplicationId, Guid.Empty);
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.SubscriptionId, Guid.Empty);

            ProgressWriteLine("Asserting AzureAccountName equals to 24x 0");
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.AzureAccountName, "000000000000000000000000");

            ProgressWriteLine("Asserting IsVerified is False");
            Assert.IsFalse(Library.ENotariado.ENotariadoConnection.IsVerified);

            var applicationId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();

            ProgressWriteLine("Initializing ENotariadoConnection with random GUIDs");
            Library.ENotariado.ENotariadoConnection.Init(applicationId, Certificate, true, subscriptionId);
            
            ProgressWriteLine("Asserting IDs are equal to the ones generated");
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.ApplicationId, applicationId);
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.SubscriptionId, subscriptionId);

            ProgressWriteLine("Asserting AzureAccountName equals to parsed subscription id");
            Assert.AreEqual(Library.ENotariado.ENotariadoConnection.AzureAccountName, subscriptionId.ToString().Replace("-", "").Substring(0, 24));

            ProgressWriteLine("Asserting IsVerified is True");
            Assert.IsTrue(Library.ENotariado.ENotariadoConnection.IsVerified);
        }
    }
}
