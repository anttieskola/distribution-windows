using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Domain.Helpers
{
    /// <summary>
    /// Simple tracer for debugging
    /// TODO: Print filename, line number, method here. I tried already stackframe/trace but did
    /// not succeed.
    /// </summary>
    public class Tracer
    {
        [Conditional("DEBUG")]
        public static void Error(string format, params object[] args)
        {
            Error(String.Format(format, args));
        }

        [Conditional("DEBUG")]
        public static void Error(string msg)
        {
            Debug.WriteLine(msg);
        }
    }
}
