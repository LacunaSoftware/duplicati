using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Duplicati.Library.ENotariado
{
    public class ApplicationEnrollRequest
    {

        [Required]
        public byte[] Certificate { get; set; }

        [Required]
        public string Description { get; set; }
    }

    public class ApplicationEnrollResponse
    {

        public Guid Id { get; set; }
    }

    public class ApplicationEnrollmentModel
    {

        public Guid Id { get; set; }

        public string CertificateThumbprint { get; set; }

        public string Description { get; set; }

        public DateTimeOffset DateCreated { get; set; }
    }
    public class ApplicationEnrollmentQueryResponse
    {

        public bool Found { get; set; }

        public ApplicationEnrollmentModel Enrollment { get; set; }
    }
    public class ApplicationEnrollmentStatusQueryResponse
    {

        public bool Approved { get; set; }
    }
}
