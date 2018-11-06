using Duplicati.Library.Localization.Short;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    /// <summary>
    /// Exception thrown when the data (certificate thumbprint and
    /// application id) about e-notariado has not been initialized yet.
    /// </summary>
    public class ENotariadoNotInitializedException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"The application is not enrolled in e-notariado");
        public ENotariadoNotInitializedException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the enrollment to e-notariado servers fails
    /// </summary>
    public class FailedEnrollmentException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"There was an error while enrolling in e-notariado");
        public FailedEnrollmentException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a request to e-notariado servers fail
    /// </summary>
    public class FailedRequestException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"There was an error in the connection with e-notariado");
        public string Details;
        
        public FailedRequestException(string message) : base(ErrorMessage + " - " + message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a certificate find operation fails
    /// </summary>
    public class ENotariadoNotVerifiedException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"The application is not verified in e-notariado");
        public ENotariadoNotVerifiedException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a certificate find operation fails
    /// </summary>
    public class CertificateNotFoundException : Exception
    {
        public static readonly string ErrorMessage = LC.L("Certificate with thumbprint {0} not found.");

        public CertificateNotFoundException(string certThumbprint)
            : base(string.Format(ErrorMessage, certThumbprint))
        {
        }
    }
}
