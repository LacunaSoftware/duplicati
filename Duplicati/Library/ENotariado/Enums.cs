using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.Library.ENotariado
{
    [Flags]
    public enum ENotariadoStatus
    {
        None = 0x0,
        Enrolled = 0x1,
        Verified = 0x2
    }
}
