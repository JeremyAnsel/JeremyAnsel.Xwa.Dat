using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.Xwa.Dat
{
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "Reviewed")]
    public enum DatImageFormat
    {
        /// <summary>
        /// Format: 8-bit indexed colors and 1-bit alpha, rle compressed.
        /// Use: concourse or in-flight.
        /// </summary>
        Format7 = 7,

        /// <summary>
        /// Format: 8-bit indexed colors and 8-bit alpha, rle compressed.
        /// Use: concourse.
        /// </summary>
        Format23 = 23,

        /// <summary>
        /// Format: 8-bit indexed colors and 8-bit alpha.
        /// Use: in-flight.
        /// </summary>
        Format24 = 24,

        /// <summary>
        /// Format: 32-bit ARGB.
        /// Use: none.
        /// </summary>
        Format25 = 25,
    }
}
