using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DW.Domain.Entity
{
    [DataContract]
    public class Device
    {
        [DataMember]
        public Product Product { get; set; }
        [DataMember]
        public string Uid { get; set; }
        [DataMember]
        public DateTime TimeRegistered { get; set; }
    }
}
