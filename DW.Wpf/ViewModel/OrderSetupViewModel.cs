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
using System.Threading.Tasks;
using System.Windows;

namespace DW.Wpf.ViewModel
{
    public class OrderSetupViewModel : ViewModelBase
    {
        #region definitions
        /// <summary>
        /// Help message when no label printing enabled
        /// </summary>
        private static string HELP_MESSAGE = "Select distributor, product and select Next. Manufacturing date is optional.";
        /// <summary>
        /// Help message when label printing enabled
        /// </summary>
        private static string HELP_MESSAGE_PRINTING = "Select distributor, product, label printer, label template and select Next. Manufacturing date is optional.";
        #endregion

        #region bindable properties
        private string helpMessage;
        /// <summary>
        /// Help message
        /// </summary>
        public string HelpMessage
        {
            get { return this.helpMessage; }
            private set { this.helpMessage = value; RaisePropertyChanged("HelpMessage"); }
        }

        // Next is all the collections and selected value for each

        private ReadOnlyCollection<Distributor> distributors;
        public ReadOnlyCollection<Distributor> Distributors
        {
            get { return this.distributors; }
            private set { this.distributors = value; RaisePropertyChanged("Distributors"); }
        }

        private Distributor selectedDistributor;
        public Distributor SelectedDistributor
        {
            get { return this.selectedDistributor; }
            set
            {
                this.selectedDistributor = value;
                RaisePropertyChanged("SelectedDistributor");
                CreateCommand.RaiseCanExecuteChanged();
            }
        }

        private ReadOnlyCollection<Product> products;
        public ReadOnlyCollection<Product> Products
        {
            get { return this.products; }
            private set { this.products = value; RaisePropertyChanged("Products"); }
        }

        private Product selectedProduct;
        public Product SelectedProduct
        {
            get { return this.selectedProduct; }
            set
            {
                this.selectedProduct = value;
                RaisePropertyChanged("SelectedProduct");
                CreateCommand.RaiseCanExecuteChanged();
            }
        }

        private DateTime? manufacturingDate;
        /// <summary>
        /// Note this is optional
        /// </summary>
        public DateTime? ManufacturingDate
        {
            get { return this.manufacturingDate; }
            set { this.manufacturingDate = value; RaisePropertyChanged("ManufacturingDate"); }
        }

        private Visibility labelPrintingVisibility;
        public Visibility LabelPrintingVisibility
        {
            get { return this.labelPrintingVisibility; }
            private set { this.labelPrintingVisibility = value; RaisePropertyChanged("LabelPrintingVisibility"); }
        }

        private ReadOnlyCollection<string> labelPrinters;
        public ReadOnlyCollection<string> LabelPrinters
        {
            get { return this.labelPrinters; }
            private set { this.labelPrinters = value; RaisePropertyChanged("LabelPrinters"); }
        }

        private string selectedLabelPrinter;
        public string SelectedLabelPrinter
        {
            get { return this.selectedLabelPrinter; }
            set { this.selectedLabelPrinter = value; RaisePropertyChanged("SelectedLabelPrinter"); }
        }

        private ReadOnlyCollection<LabelTemplate> labelTemplates;
        public ReadOnlyCollection<LabelTemplate> LabelTemplates
        {
            get { return this.labelTemplates; }
            private set { this.labelTemplates = value; RaisePropertyChanged("LabelTemplates"); }
        }

        private LabelTemplate selectedLabelTemplate;
        public LabelTemplate SelectedLabelTemplate
        {
            get { return this.selectedLabelTemplate; }
            set
            {
                this.selectedLabelTemplate = value;
                RaisePropertyChanged("SelectedLabelTemplate");
                CreateCommand.RaiseCanExecuteChanged();
            }
        }

        private RelayCommand resetCommand;
        /// <summary>
        /// Reset command
        /// </summary>
        public RelayCommand ResetCommand
        {
            get { return this.resetCommand; }
            private set { this.resetCommand = value; RaisePropertyChanged("Resetcommand"); }
        }

        private RelayCommand createCommand;
        /// <summary>
        /// Create command
        /// </summary>
        public RelayCommand CreateCommand
        {
            get { return this.createCommand; }
            private set { this.createCommand = value; RaisePropertyChanged("CreateCommand"); }
        }
        #endregion

        #region fields
        private IDistributionRepository _repository;
        private bool _labelPrintingAvailable;
        #endregion

        /// <summary>
        /// injection constructor
        /// </summary>
        /// <param name="service"></param>
        public OrderSetupViewModel(IDistributionRepository service)
        {
            // injected repository
            _repository = service;

            // commands
            createCommand = new RelayCommand(create, canCreate);
            resetCommand = new RelayCommand(reset, () => { return true; });

            // design mode check
            if (IsInDesignMode)
            {
                designInit();
            }
            else
            {
                // check is label printing available and set help message
                _labelPrintingAvailable = _repository.IsLabelPrintingAvailable;
                if (_labelPrintingAvailable)
                {
                    LabelPrintingVisibility = Visibility.Visible;
                    HelpMessage = HELP_MESSAGE_PRINTING;
                }
                else
                {
                    LabelPrintingVisibility = Visibility.Collapsed;
                    HelpMessage = HELP_MESSAGE;
                }

                // load data in paraller
                // distributors
                Task.Run(async () =>
                {
                    Distributors = new ReadOnlyCollection<Distributor>(new List<Distributor>(await _repository.Distributors()));
                });
                // products
                Task.Run(async () =>
                {
                    Products = new ReadOnlyCollection<Product>(new List<Product>(await _repository.Products()));
                });
                // label printers
                LabelPrinters = new ReadOnlyCollection<string>(new List<string>(_repository.LabelPrinters()));
                // label templates
                Task.Run(async () =>
                {
                    LabelTemplates = new ReadOnlyCollection<LabelTemplate>(new List<LabelTemplate>(await _repository.LabelTemplates()));
                });
            }
        }

        /// <summary>
        /// Create canExecute
        /// </summary>
        /// <returns></returns>
        private bool canCreate()
        {
            // check all required for order is selected
            // this depends on state of label printing
            if (_labelPrintingAvailable)
            {
                if (SelectedDistributor != null &&
                    SelectedProduct != null &&
                    SelectedLabelPrinter != null &&
                    SelectedLabelTemplate != null)
                {
                    return true;
                }
            }
            else
            {
                if (SelectedDistributor != null &&
                    SelectedProduct != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// CreateCommand execute
        /// </summary>
        private void create()
        {
            // create order and switch view
            if (_labelPrintingAvailable)
            {
                // with label printing
                if (_repository.CreateOrder(SelectedDistributor, SelectedProduct, SelectedLabelPrinter, SelectedLabelTemplate, ManufacturingDate))
                {
                    Messenger.Default.Send<ViewChange>(new ViewChange { To = VMLocator.OrderFillViewKey });
                }
            }
            else
            {
                // without label printing
                if (_repository.CreateOrder(SelectedDistributor, SelectedProduct, ManufacturingDate))
                {
                    Messenger.Default.Send<ViewChange>(new ViewChange { To = VMLocator.OrderFillViewKey });
                }
            }
        }

        /// <summary>
        /// Resetcommand execute
        /// </summary>
        private void reset()
        {
            SelectedDistributor = null;
            SelectedProduct = null;
            ManufacturingDate = null;
            SelectedLabelPrinter = null;
            SelectedLabelTemplate = null;
        }

        #region design mode init
        private void designInit()
        {
            helpMessage = HELP_MESSAGE_PRINTING;

            distributors = new ReadOnlyCollection<Distributor>(new List<Distributor>
                {
                    new Distributor { Id = 1, Name = "Suomen Lämpömittari Oy"},
                    new Distributor { Id = 2, Name = "TFA"},
                    new Distributor { Id = 3, Name = "Holux"},
                    new Distributor { Id = 4, Name = "Ceruus Oy"},
                    new Distributor { Id = 5, Name = "\U00005728\U00005BB6\U0000529E\U0000516C"}
                });
            selectedDistributor = Distributors.Single(x => x.Id == 4);

            products = new ReadOnlyCollection<Product>(new List<Product>
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
            });
            selectedProduct = products.Single(x => x.Name == "Catcher Universe");

            labelPrinters = new ReadOnlyCollection<string>(new List<string> { "Dymo 9001 Color 3D Laser Phaser" });
            selectedLabelPrinter = labelPrinters.First();

            labelTemplates = new ReadOnlyCollection<LabelTemplate>(new List<LabelTemplate>
                {
                    new LabelTemplate { Name = "32mm x 57mm portrait of famous men who walked in the moon.", TemplateFile = "doesnotexist"}
                });
            selectedLabelTemplate = labelTemplates.First();
        }
        #endregion
    }
}
