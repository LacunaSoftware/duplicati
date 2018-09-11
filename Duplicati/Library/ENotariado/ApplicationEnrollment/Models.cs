﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Lacuna.DataAnnotations;

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
    public class StartPublicKeyAuthenticationRequest
    {

        [RequiredNonDefault]
        public Guid ApplicationId { get; set; }

        [Required]
        public string CertificateThumbprint { get; set; }
    }

    public class StartPublicKeyAuthenticationResponse
    {

        public byte[] ToSignData { get; set; }

        public string DigestAlgorithmName { get; set; }

        public string DigestAlgorithmOid { get; set; }
    }
    public class CompletePublicKeyAuthenticationRequest
    {

        [Required]
        public byte[] Signature { get; set; }
    }
    public class CompletePublicKeyAuthenticationResponse
    {

        public string AppToken { get; set; }
    }
    public class SASRequestModel
    {
        public Guid AppKeyId { get; set; }
    }
    public class SASResponseModel
    {
        public string Token { get; set; }
        public Uri Uri { get; set; }
    }
}
