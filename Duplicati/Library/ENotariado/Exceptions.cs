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
    /// application id) about eNotariado has not been initialized yet.
    /// </summary>
    public class ENotariadoNotInitializedException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"The application is not enrolled in e-Notariado");
        public ENotariadoNotInitializedException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the enrollment to eNotariado servers fails
    /// </summary>
    public class FailedEnrollmentException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"There was an error while enrolling in e-Notariado");
        public FailedEnrollmentException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a request to eNotariado servers fail
    /// </summary>
    public class FailedRequestException : Exception
    {
        public static readonly string ErrorMessage = LC.L(@"There was an error in the connection with e-Notariado");
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
        public static readonly string ErrorMessage = LC.L(@"The application is not verified in e-Notariado");
        public ENotariadoNotVerifiedException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a certificate find operation fails
    /// </summary>
    public class CertificateNotFoundException : Exception
    {
        public CertificateNotFoundException()
        {
        }

        public CertificateNotFoundException(string message)
            : base(message)
        {
        }

        public CertificateNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
