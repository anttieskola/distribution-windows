using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DW.Wpf.Helpers
{
    public class UidConverter : IValueConverter
    {
        /// <summary>
        /// Returns substring of given value.
        /// The substring starting from given start index with max length of 4 characters.
        /// If the string is shorter than given substring empty string is returned.
        /// </summary>
        /// <param name="value">complete string</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">start index</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string uid = value as string; // input string
            string param = parameter as string; // start index
            int startIndex;
            // parse and check
            if (int.TryParse(param, out startIndex) && uid != null)
            {
                // pickup substring dependending on length of complete string
                if (uid.Length >= (startIndex + 4)) 
                {
                    return uid.Substring(startIndex, 4);
                }
                else if (uid.Length > startIndex)
                {
                    return uid.Substring(startIndex);
                }
            }
            return "";
        }

        /// <summary>
        /// not implemented as this conversion only used shown data in the view
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
