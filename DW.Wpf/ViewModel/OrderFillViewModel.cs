using DW.Domain.Abstract;
using DW.Domain.Entity;
using DW.Wpf.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DW.Wpf.ViewModel
{
    public class OrderFillViewModel : ViewModelBase
    {
        #region Constants
        /// <summary>
        /// Length of the device unique identifier (last number is checksum)
        /// </summary>
        public static int UID_LENGTH = 13;
        /// <summary>
        /// State of filling
        /// </summary>
        public enum StatusEnum
        {
            Ready,
            Registering,
            Printing
        }
        #endregion

        #region bindable properties
        private Order order;
        /// <summary>
        /// Current order, includes client, devicetype, label template....
        /// </summary>
        public Order Order
        {
            get { return this.order; }
            private set { this.order = value; RaisePropertyChanged("Order"); }
        }

        private Visibility manufacturingDateVisibility;
        public Visibility ManufacturingDateVisibility
        {
            get { return this.manufacturingDateVisibility; }
            private set { this.manufacturingDateVisibility = value; RaisePropertyChanged("ManufacturingDateVisibility"); }
        }

        private RelayCommand saveCommand;
        /// <summary>
        /// Back to order setup for new one
        /// </summary>
        public RelayCommand SaveCommand
        {
            get { return this.saveCommand; }
            private set { this.saveCommand = value; RaisePropertyChanged("SaveCommand"); }
        }

        private bool manualInputEnabled;
        /// <summary>
        /// Manual typing of uid enabled?
        /// </summary>
        public bool ManualInputEnabled
        {
            get { return this.manualInputEnabled; }
            set
            {
                this.manualInputEnabled = value;
                RaisePropertyChanged("ManualInputEnabled");
                RaisePropertyChanged("ScannerInputVisibility");
                RaisePropertyChanged("ManualInputVisibility");
                setStatus(StatusEnum.Ready).Wait();
                if (value)
                {
                    showManualInputWarning();
                }
            }
        }

        /// <summary>
        /// Show scanner input?
        /// </summary>
        public Visibility ScannerInputVisibility
        {
            get 
            {
                return ManualInputEnabled ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Show manual input?
        /// </summary>
        public Visibility ManualInputVisibility
        {
            get 
            {
                return ManualInputEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Visibility labelPrintingVisibility;
        /// <summary>
        /// Show label printing details?
        /// </summary>
        public Visibility LabelPrintingVisibility
        {
            get { return this.labelPrintingVisibility; }
            private set { this.labelPrintingVisibility = value; RaisePropertyChanged("LabelPrintingVisibility"); }
        }

        private string uid;
        /// <summary>
        /// UID label
        /// </summary>
        public string Uid
        {
            get { return this.uid; }
            private set { this.uid = value; RaisePropertyChanged("Uid"); }
        }

        private string manualUid;
        /// <summary>
        /// Uid textbox for manual input
        /// </summary>
        public string ManualUid
        {
            get { return this.manualUid; }
            set { this.manualUid = value; RaisePropertyChanged("ManualUid"); }
        }

        private RelayCommand manualUidRegisterCommand;
        /// <summary>
        /// Register command for manual input
        /// </summary>
        public RelayCommand ManualUidRegisterCommand
        {
            get { return this.manualUidRegisterCommand; }
            private set { this.manualUidRegisterCommand = value; RaisePropertyChanged("ManualUidRegisterCommand"); }
        }

        private StatusEnum status;
        /// <summary>
        /// State of filling in enumeration form
        /// </summary>
        public StatusEnum Status
        {
            get { return status; }
        }

        private string statusText;
        /// <summary>
        /// State of filling in human readable form
        /// </summary>
        public string StatusText
        {
            get { return this.statusText; }
            private set { this.statusText = value; RaisePropertyChanged("StatusText"); }
        }

        private SolidColorBrush statusColor;
        /// <summary>
        /// Status Foreground color
        /// </summary>
        public SolidColorBrush StatusColor
        {
            get { return this.statusColor; }
            private set { this.statusColor = value; RaisePropertyChanged("StatusColor"); }
        }

        private SolidColorBrush statusBackgroundColor;
        /// <summary>
        /// Status BG color
        /// </summary>
        public SolidColorBrush StatusBackgroundColor
        {
            get { return this.statusBackgroundColor; }
            private set { this.statusBackgroundColor = value; RaisePropertyChanged("StatusBackgroundColor"); }
        }

        private string helpText;
        /// <summary>
        /// Informative help text to show user
        /// </summary>
        public string HelpText
        {
            get { return this.helpText; }
            private set { this.helpText = value; RaisePropertyChanged("HelpText"); }
        }
        #endregion

        #region fields
        private IDistributionRepository _repository;
        private Device _device;
        #endregion

        /// <summary>
        /// injection constructor
        /// </summary>
        public OrderFillViewModel(IDistributionRepository repository)
        {
            _repository = repository;
            saveCommand = new RelayCommand(save);
            manualInputEnabled = false;
            manualUidRegisterCommand = new RelayCommand(registerDeviceManual, canRegisterDeviceManual);

            if (IsInDesignMode)
            {
                designInit();
            }
            else
            {
                // Set current order information
                order = _repository.CurrentOrder;
                if (order.ManufacturingDate != null)
                {
                    ManufacturingDateVisibility = Visibility.Visible;
                }
                else
                {
                    ManufacturingDateVisibility = Visibility.Collapsed;
                }
                if (order.LabelPrint)
                {
                    LabelPrintingVisibility = Visibility.Visible;
                }
                else
                {
                    LabelPrintingVisibility = Visibility.Collapsed;
                }
                // Prepare for scan input
                setStatus(StatusEnum.Ready).Wait();
            }
        }

        /// <summary>
        /// Set status, note ReadyToScan is not allowed
        /// to use repository as it is called synchronously.
        /// </summary>
        /// <param name="newStatus"></param>
        private async Task setStatus(StatusEnum newStatus)
        {
            // Update UI binds
            status = newStatus;
            RaisePropertyChanged("Status"); // manual notify
            StatusColor = new SolidColorBrush(Colors.White); // using same foreground color for status in all states
            switch (Status)
            {
                case StatusEnum.Ready:
                    _device = new Device { Product = Order.Product };
                    Uid = "";
                    StatusBackgroundColor = new SolidColorBrush(Colors.DarkGreen);
                    if (manualInputEnabled)
                    {
                        StatusText = "Type UID";
                        HelpText = "Type the device UID into textbox and select register.";
                    }
                    else
                    {
                        StatusText = "Ready to scan";
                        HelpText = "Scan device UID using handheld scanner";
                    }
                    break;
                case StatusEnum.Registering:
                    StatusBackgroundColor = new SolidColorBrush(Colors.DarkRed);
                    StatusText = "Registering";
                    HelpText = "Please wait a moment";
                    break;
                case StatusEnum.Printing:
                    StatusBackgroundColor = new SolidColorBrush(Colors.DarkOrange);
                    StatusText = "Printing";
                    HelpText = "Please wait a moment";
                    break;
            }

            // Do action
            switch (Status)
            {
                case StatusEnum.Registering:
                    await registerDevice();
                    break;
                case StatusEnum.Printing:
                    await printLabel();
                    break;
            }
        }

        /// <summary>
        /// Register scanned device
        /// </summary>
        /// <returns></returns>
        private async Task registerDevice()
        {
            // first check device is not already registered in current order
            if (!Order.Devices.Any(d => d.Uid == _device.Uid))
            {
                // register
                RegisterDeviceResponse res = await _repository.RegisterDevice(_device);
                // handle response and change status
                if (res.Success)
                {
                    _device.TimeRegistered = DateTime.Now;
                    Order.Devices.Add(_device);
                    if (Order.LabelPrint)
                    {
                        // order is printing labels
                        await setStatus(StatusEnum.Printing);
                    }
                    else
                    {
                        // no labels printed
                        setStatus(StatusEnum.Ready).Wait();
                    }
                }
                else
                {
                    // error registering device
                    StatusBackgroundColor = new SolidColorBrush(Colors.DarkRed);
                    MessageBox.Show("Error registering device.\n" + res.Error, "Error", MessageBoxButton.OK);
                    StatusBackgroundColor = new SolidColorBrush(Colors.DarkGreen);
                    setStatus(StatusEnum.Ready).Wait();
                }
            }
            else
            {
                // device in order already
                if (Order.LabelPrint)
                {
                    // order setup to print labels, ask for reprint
                    if (askForRePrint())
                    {
                        await setStatus(StatusEnum.Printing);
                    }
                    else
                    {
                        setStatus(StatusEnum.Ready).Wait();
                    }
                }
                else
                {
                    // inform user
                    MessageBox.Show("Device already registered in this session.", "Warning", MessageBoxButton.OK);
                    setStatus(StatusEnum.Ready).Wait();
                }
            }
        }

        /// <summary>
        /// Print registered devices label
        /// </summary>
        /// <returns></returns>
        private async Task printLabel()
        {
            // print
            PrintLabelResponse res = await _repository.PrintLabel(_device, Order.LabelTemplate);
            // handle response and change status
            if (res.Success)
            {
                await setStatus(StatusEnum.Ready);
            }
            else
            {
                // handle error
                throw new SystemException("Achtung!");
            }
        }

        /// <summary>
        /// Ask does user want to re-print label?
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private bool askForRePrint()
        {
            MessageBoxResult res = MessageBox.Show("Device already registered, do you want to print label again?", "Notice", MessageBoxButton.YesNo);
            switch (res)
            {
                case MessageBoxResult.Yes:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Register manually typed device
        /// </summary>
        private async void registerDeviceManual()
        {
            if (ManualUid == null)
            {
                return;
            }

            if (ManualUid.Length == 0)
            {
                return;
            }

            string hexUid = parseHex(ManualUid);
            if (hexUid.Length == 12)
            {
                _device.Uid = hexUid;
                ManualUid = "";
                await setStatus(StatusEnum.Registering);
            }
            else
            {
                string msg = String.Format("Can't parse valid 12 character length UID from given input.\n Input: {0}\n Parse: {1}", ManualUid, hexUid);
                MessageBox.Show(msg, "Error", MessageBoxButton.OK);
            }
        }

        private bool canRegisterDeviceManual()
        {
            if (Status == StatusEnum.Ready && ManualInputEnabled)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Feed characters from handheld scanner or keyboard to this
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public async Task InputChar(char c)
        {
            // only act in scan state and when manual input disabled
            if (Status == StatusEnum.Ready && !ManualInputEnabled)
            {
                // backspace
                if (c == '\b' && Uid.Length > 0)
                {
                    Uid = Uid.Substring(0, Uid.Length - 1);
                }
                // add valid characters
                else
                {
                    Uid += parseHex(Char.ToUpper(c).ToString());
                }

                // Is input done?
                if (Uid.Length == UID_LENGTH)
                {
                    // full code scanned, lets check the checksum
                    if (checkUid())
                    {
                        _device.Uid = Uid.Substring(0, UID_LENGTH - 1);
                        await setStatus(StatusEnum.Registering);
                    }
                    else
                    {
                        // invalid checksum in the uid
                        StatusBackgroundColor = new SolidColorBrush(Colors.DarkRed);
                        string errorMsg = String.Format("Scanned code '{0}' has invalid checksum '{1}'. Please try rescanning code.",
                            Uid.Substring(0, 12), Uid.Substring(12));
                        MessageBox.Show(String.Format(errorMsg, Uid), "Error", MessageBoxButton.OK);
                        StatusBackgroundColor = new SolidColorBrush(Colors.DarkGreen);
                        Uid = "";
                    }
                }
                else if (Uid.Length > UID_LENGTH)
                {
                    // should not happen
                    throw new Exception("InputChar UID length exceeded defined value!");
                }
            }
        }

        /// <summary>
        /// Verify Uid is valid
        /// </summary>
        /// <param name="Uid"></param>
        /// <returns></returns>
        internal bool checkUid()
        {
            // check uid is set properly
            if (Uid == null || Uid.Length != 13)
            {
                return false;
            }

            // calculated sum
            int calculatedChecksum = calculateChecksum(Uid.Substring(0, 12));

            // provided sum
            int checksum = Convert.ToInt32(Uid.Substring(12, 1), 16);

            // check if same
            if (calculatedChecksum == checksum)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ask confirmation, save order and go to setup view
        /// </summary>
        private void save()
        {
            if (_repository.SaveOrder())
            {
                Messenger.Default.Send<ViewChange>(new ViewChange
                {
                    To = VMLocator.OrderSetupViewKey
                });
            }
        }

        #region static helpers
        /// <summary>
        /// warning dialog
        /// </summary>
        private void showManualInputWarning()
        {
            MessageBox.Show("Manual input mode. Please disconnect any QR code readers before continuing.", "Warning", MessageBoxButton.OK);
        }

        /// <summary>
        /// calculate checksum of given 12 hex catcher id
        /// </summary>
        /// <param name="id">12 hex catcher id</param>
        /// <returns>checksum</returns>
        internal static int calculateChecksum(string id)
        {
            // check id is set properly
            if (id == null || id.Length != 12)
            {
                throw new ArgumentException("id must be length of 12");
            }

            // sum of hex values
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                sum += Convert.ToInt32(id.Substring(i, 1), 16);
            }
            // modulo
            int mod = sum % 16;
            return mod;
        }
        /// <summary>
        /// parse hex values of given string
        /// </summary>
        /// <param name="input">any string</param>
        /// <returns>[0-9ABCDEFabcdef]</returns>
        internal static string parseHex(string input)
        {
            Regex pat = new Regex(@"[0-9ABCDEFabcdef]+");
            MatchCollection mc = pat.Matches(input);
            StringBuilder sb = new StringBuilder();
            foreach (var m in mc)
            {
                sb.Append(m);
            }
            return sb.ToString();
        }
        #endregion

        #region design initialization
        private void designInit()
        {
            statusColor = new SolidColorBrush(Colors.White);
            statusBackgroundColor = new SolidColorBrush(Colors.Green);
            labelPrintingVisibility = Visibility.Visible;
            order = new Order();
            order.Distributor = new Distributor { Id = 5, Name = "\U00005728\U00005BB6\U0000529E\U0000516C" };
            order.Product = new Product { Name = "Catcher SG-1", Hw = "1000.11", Sw = "2123.1211" };
            order.LabelPrint = true;
            order.LabelPrinter = "Dymo 450";
            order.LabelTemplate = new LabelTemplate { Name = "50mm x 100mm color", TemplateFile = "not.label" };
            order.ManufacturingDate = DateTime.Now;
            ManufacturingDateVisibility = Visibility.Visible;
            order.Devices = new ObservableCollection<Device>(new List<Device> 
                {
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product},
                    new Device { Uid = "1234ABCD1234", TimeRegistered = DateTime.Now, Product = order.Product}
                });
            status = StatusEnum.Ready;
            statusText = "Ready to scan next";
            helpText = "Scan QR code with scanner.";
            uid = "0123456789ABCD";
        }
        #endregion
    }
}
