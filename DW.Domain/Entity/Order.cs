using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DW.Domain.Entity
{
    [DataContract]
    public class Order
    {
        [DataMember]
        public Distributor Distributor { get; set; }
        [DataMember]
        public Product Product { get; set; }
        [DataMember]
        public DateTime? ManufacturingDate { get; set; }
        [DataMember]
        public ObservableCollection<Device> Devices { get; set; }
        [DataMember]
        public bool LabelPrint { get; set; }
        [DataMember]
        public string LabelPrinter { get; set; }
        [DataMember]
        public LabelTemplate LabelTemplate { get; set; }
    }
}

