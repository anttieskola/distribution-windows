using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Wpf.Messages
{
    /// <summary>
    /// Message to send when we want view to be changed.
    /// See VMLocator for the string definitions for views.
    /// Reason not used currently.
    /// </summary>
    public class ViewChange
    {
        public string To { get; set; }
        public bool HideWarnings { get; set; }
    }
}
