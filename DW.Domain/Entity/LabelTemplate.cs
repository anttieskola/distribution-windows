using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DW.Domain.Entity
{
    [DataContract]
    public class LabelTemplate
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String TemplateFile { get; set; }
    }
}
