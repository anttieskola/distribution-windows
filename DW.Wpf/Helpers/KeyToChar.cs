using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DW.Wpf.Helpers
{
    /// <summary>
    /// Helper for conversion
    /// http://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c
    /// </summary>
    internal class KeyToChar
    {
        /// <summary>
        /// Convert Key to char
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static char Get(Key key)
        {
            char ch = ' ';
            // convert key to virtual key
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            // keyboard state
            byte[] keyboardState = new byte[256];
            NativeMethods.GetKeyboardState(keyboardState);
            // scan code from virtual key
            uint scanCode = NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);
            // scan code to unicode
            int result = NativeMethods.ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }
}
