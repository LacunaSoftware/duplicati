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
        public static readonly string ErrorMessage = "O registro da aplicação com o eNotariado não foi inicializado";
        public ENotariadoNotInitializedException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the enrollment to eNotariado servers fails
    /// </summary>
    public class FailedEnrollmentException : Exception
    {
        public static readonly string ErrorMessage = "Houve um erro ao se registrar com o servidor do eNotariado";
        public FailedEnrollmentException() : base(ErrorMessage)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a request to eNotariado servers fail
    /// </summary>
    public class FailedRequestException : Exception
    {
        public static readonly string ErrorMessage = "Houve um erro na comunicação com o servidor do eNotariado";
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
