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
        public ENotariadoNotInitializedException()
        {
        }

        public ENotariadoNotInitializedException(string message)
            : base(message)
        {
        }

        public ENotariadoNotInitializedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the enrollment to eNotariado servers fails
    /// </summary>
    public class FailedEnrollmentException : Exception
    {
        public FailedEnrollmentException()
        {
        }

        public FailedEnrollmentException(string message)
            : base(message)
        {
        }

        public FailedEnrollmentException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a request to eNotariado servers fail
    /// </summary>
    public class FailedRequestException : Exception
    {
        public FailedRequestException()
        {
        }

        public FailedRequestException(string message)
            : base(message)
        {
        }

        public FailedRequestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a certificate find operation fails
    /// </summary>
    public class ENotariadoNotVerifiedException : Exception
    {
        public ENotariadoNotVerifiedException()
        {
        }

        public ENotariadoNotVerifiedException(string message)
            : base(message)
        {
        }

        public ENotariadoNotVerifiedException(string message, Exception inner)
            : base(message, inner)
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
