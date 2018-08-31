using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    public class ENotariadoNotInitialized : Exception
    {
        public ENotariadoNotInitialized()
        {
        }

        public ENotariadoNotInitialized(string message)
            : base(message)
        {
        }

        public ENotariadoNotInitialized(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

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
}
