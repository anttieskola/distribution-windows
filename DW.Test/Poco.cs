using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DW.Test
{
    internal class Poco
    {
        /// <summary>
        /// Compare do given poco objects contain equal property values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static bool AreEqual(object a, object b)
        {
            // Null(s) and types
            if (a == null && b == null)
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }
            if (a.GetType() != b.GetType())
            {
                return false;
            }
            // Public properties
            foreach (PropertyInfo propertyInfo in a.GetType().GetProperties())
            {
                if (propertyInfo.CanRead)
                {
                    // property values
                    object av = propertyInfo.GetValue(a, null);
                    object bv = propertyInfo.GetValue(b, null);
                    // check that they are primitives or string or decimal
                    if ((av.GetType().IsPrimitive && bv.GetType().IsPrimitive) ||
                        (av.GetType() == typeof(string) && bv.GetType() == typeof(string)) ||
                        (av.GetType() == typeof(decimal) && bv.GetType() == typeof(decimal)))
                    {
                        if (!object.Equals(av, bv))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // when they are not primitives i recursively call myself
                        if (!Poco.AreEqual(av, bv))
                        {
                            return false;
                        }
                    }

                }
            }
            return true;
        }
    }
}
