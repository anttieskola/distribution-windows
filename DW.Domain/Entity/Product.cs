using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DW.Domain.Entity
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Hw { get; set; }
        [DataMember]
        public string Sw { get; set; }
    }
}
