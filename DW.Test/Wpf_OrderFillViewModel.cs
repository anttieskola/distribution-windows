using DW.Domain.Abstract;
using DW.Domain.Entity;
using DW.Wpf.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DW.Test
{
    [TestClass]
    public class Wpf_OrderFillViewModel
    {
        private Distributor testClient
        {
            get
            {
                return new Distributor { Id = 007, Name = "James Bond" };
            }
        }

        private Product testProductType
        {
            get
            {
                return new Product { Name = "Catcher SG-1", Hw = "HHWW001.02", Sw = "2001.12" };
            }
        }

        private string testDeviceUid
        {
            get
            {
                return "1234ABCD12342";
            }
        }

        private string testDeviceUidFromScanner
        {
            get
            {
                return "12:34:AB:CD:12:34:2";
            }
        }

        private string testLabelPrinter
        {
            get
            {
                return "Dymo 9001";
            }
        }

        private LabelTemplate testLabelTemplate
        {
            get
            {
                return new LabelTemplate { Name = "30x30 portrait", TemplateFile = "Some.Label" };
            }
        }

        private Order testOrder
        {
            get
            {
                return new Order
                {
                    Distributor = testClient,
                    Product = testProductType,
                    LabelPrint = false,
                    LabelPrinter = null,
                    LabelTemplate = null,
                    Devices = new ObservableCollection<Device>()
                };
            }
        }

        private Order testOrderLabels
        {
            get
            {
                return new Order
                {
                    Distributor = testClient,
                    Product = testProductType,
                    LabelPrint = true,
                    LabelPrinter = testLabelPrinter,
                    LabelTemplate = testLabelTemplate,
                    Devices = new ObservableCollection<Device>()
                };
            }
        }

        private IDistributionRepository mockRepo
        {
            get
            {
                Mock<IDistributionRepository> mock = new Mock<IDistributionRepository>();
                mock.Setup(m => m.CurrentOrder).Returns(testOrder);
                mock.Setup(m => m.RegisterDevice(It.IsAny<Device>())).Returns(Task.FromResult(new RegisterDeviceResponse { Success = true }));
                mock.Setup(m => m.PrintLabel(It.IsAny<Device>(), It.IsAny<LabelTemplate>())).Returns(Task.FromResult(new PrintLabelResponse { Success = false }));
                return mock.Object;
            }
        }

        private IDistributionRepository mockRepoLabels
        {
            get
            {
                Mock<IDistributionRepository> mock = new Mock<IDistributionRepository>();
                mock.Setup(m => m.CurrentOrder).Returns(testOrderLabels);
                mock.Setup(m => m.RegisterDevice(It.IsAny<Device>())).Returns(Task.FromResult(new RegisterDeviceResponse { Success = true }));
                mock.Setup(m => m.PrintLabel(It.IsAny<Device>(), It.IsAny<LabelTemplate>())).Returns(Task.FromResult(new PrintLabelResponse { Success = true }));
                return mock.Object;
            }
        }

        [TestMethod]
        public void NormalUseCase()
        {
            // create vm
            OrderFillViewModel vm = new OrderFillViewModel(mockRepo);
            Assert.IsTrue(Poco.AreEqual(testOrder.Distributor, vm.Order.Distributor));
            Assert.IsTrue(Poco.AreEqual(testOrder.Product, vm.Order.Product));
            Assert.IsFalse(vm.Order.LabelPrint);
            Assert.IsNull(vm.Order.LabelPrinter);
            Assert.IsNull(vm.Order.LabelTemplate);
            Assert.AreEqual(0, vm.Order.Devices.Count);
            Assert.AreEqual(OrderFillViewModel.StatusEnum.Ready, vm.Status);

            // scan something
            string scanCode = testDeviceUidFromScanner;
            foreach (var c in scanCode)
            {
                vm.InputChar(c).Wait();
            }

            // TODO: how we could test next states? We would need to control
            // when repository responds VM...

            // next it should be registering
            //Assert.AreEqual(OrderFillViewModel.StatusEnum.Registering, vm.Status);

            // next it should print now
            //Assert.AreEqual(OrderFillViewModel.StatusEnum.Printing, vm.Status);


            // next it should be ready to scan again
            Assert.AreEqual(OrderFillViewModel.StatusEnum.Ready, vm.Status);

            // we should have one item in devices now
            Assert.AreEqual(1, vm.Order.Devices.Count);
            Assert.AreEqual(testDeviceUid.Substring(0,12), vm.Order.Devices.First().Uid);
        }

        [TestMethod]
        public void NormalUseCaseLabels()
        {
            // create vm
            OrderFillViewModel vm = new OrderFillViewModel(mockRepoLabels);
            Assert.IsTrue(Poco.AreEqual(testOrder.Distributor, vm.Order.Distributor));
            Assert.IsTrue(Poco.AreEqual(testOrder.Product, vm.Order.Product));
            Assert.IsTrue(vm.Order.LabelPrint);
            Assert.AreEqual(testOrderLabels.LabelPrinter, vm.Order.LabelPrinter);
            Assert.IsTrue(Poco.AreEqual(testOrderLabels.LabelTemplate, vm.Order.LabelTemplate));
            Assert.AreEqual(0, vm.Order.Devices.Count);
            Assert.AreEqual(OrderFillViewModel.StatusEnum.Ready, vm.Status);

            // scan something
            string scanCode = testDeviceUidFromScanner;
            foreach (var c in scanCode)
            {
                vm.InputChar(c).Wait();
            }

            // TODO: how we could test next states? We would need to control
            // when repository responds VM...

            // next it should be registering
            //Assert.AreEqual(OrderFillViewModel.StatusEnum.Registering, vm.Status);

            // next it should print now
            //Assert.AreEqual(OrderFillViewModel.StatusEnum.Printing, vm.Status);


            // next it should be ready to scan again
            Assert.AreEqual(OrderFillViewModel.StatusEnum.Ready, vm.Status);

            // we should have one item in devices now
            Assert.AreEqual(1, vm.Order.Devices.Count);
            Assert.AreEqual(testDeviceUid.Substring(0,12), vm.Order.Devices.First().Uid);
        }

        [TestMethod]
        public void calculateChecksum()
        {
            int r1 = OrderFillViewModel
                .calculateChecksum("E7E9912E4E95"); // 6
            int r2 = OrderFillViewModel
                .calculateChecksum("D76971254195"); // 5
            int r3 = OrderFillViewModel
                .calculateChecksum("E2E9932E2E95"); // 1
            Assert.AreEqual(6, r1);
            Assert.AreEqual(5, r2);
            Assert.AreEqual(1, r3);
        }

        [TestMethod]
        public void parseHex()
        {
            string s1 = OrderFillViewModel
                .parseHex("qwAbCrty"); // AbC
            string s2 = OrderFillViewModel
                .parseHex("wp3kDp98np1i"); // 3D981
            Assert.AreEqual("AbC", s1);
            Assert.AreEqual("3D981", s2);
        }
    }
}
