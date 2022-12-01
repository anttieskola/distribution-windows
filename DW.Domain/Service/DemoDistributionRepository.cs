using DW.Domain.Abstract;
using DW.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DW.Domain.Service
{
    /// <summary>
    /// demo implementation
    /// </summary>
    public class DemoDistributionRepository : IDistributionRepository
    {
        #region lazy singleton
        private static readonly Lazy<DemoDistributionRepository> lazy =
            new Lazy<DemoDistributionRepository>(() => new DemoDistributionRepository());
        public static DemoDistributionRepository Instance
        {
            get
            {
                return lazy.Value;
            }
        }
        public DemoDistributionRepository()
        {
            currentUser = new User();
        }
        #endregion

        #region user functionality
        private User currentUser;
        public User CurrentUser
        {
            get { return this.currentUser; }
        }

        public async Task<bool> IsLoggedIn()
        {
            await sleepAndRandomBool();
            if (String.IsNullOrEmpty(CurrentUser.Token))
            {
                return false;
            }
            return true;
        }

        public async Task<bool> Login(string userName, string passWord)
        {
            if (await sleepAndRandomBool())
            {
                CurrentUser.UserName = userName;
                CurrentUser.Token = "32M4M23M4O23N4NO23D92FNONSDFBGB2IB34O234230FSODNVOSDN3224434";
                CurrentUser.Error = "";
                return true;
            }
            else
            {
                CurrentUser.Error = "No network access.";
                CurrentUser.Token = "";
                return false;
            }
        }

        public bool Logout()
        {
            currentUser = new User();
            return true;
        }
        #endregion

        #region order functionality
        private Order currentOrder;
        public Order CurrentOrder
        {
            get { return this.currentOrder; }
            private set { this.currentOrder = value; }
        }

        public bool IsLabelPrintingAvailable
        {
            get
            {
                return true;
            }
        }

        public bool CreateOrder(Distributor customer, Product product, DateTime? manufacturingDate = null)
        {
            if (CurrentOrder == null)
            {
                CurrentOrder = new Order
                {
                    Distributor = customer,
                    Product = product,
                    ManufacturingDate = manufacturingDate,
                    LabelPrint = false,
                    LabelPrinter = null,
                    LabelTemplate = null,
                    Devices = new ObservableCollection<Device>()
                };
                return true;
            }
            return false;
        }

        public bool CreateOrder(Distributor customer, Product product, string labelPrinter, LabelTemplate labelTemplate, DateTime? manufacturingDate = null)
        {
            if (CurrentOrder == null)
            {
                CurrentOrder = new Order
                {
                    Distributor = customer,
                    Product = product,
                    ManufacturingDate = manufacturingDate,
                    LabelPrint = true,
                    LabelPrinter = labelPrinter,
                    LabelTemplate = labelTemplate,
                    Devices = new ObservableCollection<Device>()
                };
                return true;
            }
            return false;
        }

        public bool SaveOrder()
        {
            if (CurrentOrder == null)
            {
                return false;
            }
            CurrentOrder = null;
            return true;
        }
        
        public async Task<RegisterDeviceResponse> RegisterDevice(Device device)
        {
            await sleepAndRandomBool();
            return new RegisterDeviceResponse { Success = true };
        }

        public async Task<PrintLabelResponse> PrintLabel(Device device, LabelTemplate labelTemplate)
        {
            await sleepAndRandomBool();
            if (CurrentOrder.LabelPrint)
            {
                return new PrintLabelResponse { Success = true };
            }
            return new PrintLabelResponse { Success = false };
        }
        #endregion

        #region collections
        public async Task<IEnumerable<Distributor>> Distributors()
        {
            await sleepAndRandomBool();
            return new List<Distributor>
            {
                new Distributor { Id = 1, Name = "Suomen Lämpömittari Oy"},
                new Distributor { Id = 2, Name = "TFA"},
                new Distributor { Id = 3, Name = "Holux"},
                new Distributor { Id = 4, Name = "Ceruus Oy"},
                new Distributor { Id = 5, Name = "\U00005728\U00005BB6\U0000529E\U0000516C"}
            };
        }

        public async Task<IEnumerable<Product>> Products()
        {
            await sleepAndRandomBool();
            return new List<Product>
            {
                new Product {
                    Name = "Catcher SG-1",
                    Hw = "HW 1.0",
                    Sw = "SW 1.0"
                },
                new Product {
                    Name = "Catcher Atlantis",
                    Hw = "HW 1.1",
                    Sw = "SW 1.3"
                },
                new Product {
                    Name = "Catcher Universe",
                    Hw = "HW 2.0",
                    Sw = "SW 1.3"
                }
            };
        }

        public IEnumerable<string> LabelPrinters()
        {
            return new List<string> { "Dymo Writer 9001 Laser Phaser 4-D Year Original 1959 Model with complete overhaul done in 2013"};
        }

        public async Task<IEnumerable<LabelTemplate>> LabelTemplates()
        {
            await sleepAndRandomBool();
            return new List<LabelTemplate>
            {
                new LabelTemplate { Name = "32mm x 57mm portrait of famous actor that starred in many many many oscar winning films in the last century.", TemplateFile = "Example.labels"}
            };
        }
        #endregion

        #region state management
        public bool LoadState()
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null))
            {
                string file = "demo-user.dat";
                try
                {
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(file, FileMode.Open, iso))
                    {
                        DataContractSerializer dcs = new DataContractSerializer(typeof(User));
                        currentUser = (User) dcs.ReadObject(stream);
                        return true;
                    }
                } catch (FileNotFoundException)
                {
                    // no user dat written yet
                }
            }
            return false;
        }

        public void SaveState()
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null))
            {
                string file = "demo-user.dat";
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(file, FileMode.Create, iso))
                {
                    DataContractSerializer dcs = new DataContractSerializer(typeof(User));
                    dcs.WriteObject(stream, CurrentUser);
                }
            }
        }
        #endregion

        #region helpers
        /// <summary>
        /// helper for random delays and success
        /// </summary>
        /// <returns>true or false, depends how you roll</returns>
        private async Task<bool> sleepAndRandomBool()
        {
            Random r = new Random();
            await Task.Run(() => { Thread.Sleep(r.Next(2000)); });
            if (r.Next(10) > 5)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
