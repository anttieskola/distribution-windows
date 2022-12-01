using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DW.Domain.Entity
{
    [DataContract]
    public class RegisterDeviceResponse
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public string Error { get; set; }
    }

}
