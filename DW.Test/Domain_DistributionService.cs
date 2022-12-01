using DW.Domain.Entity;
using DW.Domain.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DW.Test
{
    /// <summary>
    /// Set user and pass to run tests
    /// </summary>
    [TestClass]
    public class Domain_DistributionService
    {
        string testUser
        {
            get { return ""; }
        }

        string testPass
        {
            get { return ""; }
        }

        [TestMethod]
        public void Login()
        {
            DistributionRepository repo = DistributionRepository.Instance;
            bool res = Task<bool>.Run(async () => { return await repo.Login(testUser, testPass); }).Result;
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Login_Roulette()
        {
            DistributionRepository repo = DistributionRepository.Instance;
            // login
            bool res = Task<bool>.Run(async () => { return await repo.Login(testUser, testPass); }).Result;
            Assert.IsTrue(res);
            // check we still logged in
            res = Task<bool>.Run(async () => { return await repo.IsLoggedIn(); }).Result;
            Assert.IsTrue(res);
            // logout
            res = repo.Logout();
            res = Task<bool>.Run(async () => { return await repo.IsLoggedIn(); }).Result;
            Assert.IsFalse(res);
            // login again
            res = Task<bool>.Run(async () => { return await repo.Login(testUser, testPass); }).Result;
            Assert.IsTrue(res);
            // check we still logged in
            res = Task<bool>.Run(async () => { return await repo.IsLoggedIn(); }).Result;
            Assert.IsTrue(res);
            // manually expire "hack"
            repo.CurrentUser.Token = "iamnotavalidtoken";
            // check we still logged in, obviously should not
            res = Task<bool>.Run(async () => { return await repo.IsLoggedIn(); }).Result;
            Assert.IsFalse(res);
        }

        [TestMethod]
        public void Distributors()
        {
            DistributionRepository repo = DistributionRepository.Instance;
            List<Distributor> distributors = new List<Distributor>(Task<IEnumerable<Distributor>>.Run(async () => { return await repo.Distributors(); }).Result);
            Assert.IsTrue(distributors.Count > 0);
        }

        [TestMethod]
        public void Products()
        {
            DistributionRepository repo = DistributionRepository.Instance;
            List<Product> products = new List<Product>(Task<IEnumerable<Product>>.Run(async () =>
            {
                return await repo.Products();
            }).Result);
            Assert.IsTrue(products.Count > 0);
        }

        [TestMethod]
        public void LabelPrinters()
        {
            DistributionRepository repo = DistributionRepository.Instance;
            List<string> printerList = new List<string>(repo.LabelPrinters());
        }

        [TestMethod]
        public void Register()
        {
            // please be carefull running tests currently...
            DistributionRepository repo = DistributionRepository.Instance;
            bool login = Task<bool>.Run(async () => { return await repo.Login(testUser, testPass); }).Result;
            Assert.IsTrue(login);
            Distributor dist = new Distributor
            {
                Id = 1337,
                Name = "UnitTest"
            };
            Product prd = new Product
            {
                Name = "Test product name",
                Hw = "Test HW",
                Sw = "Test SW"
            };
            Assert.IsTrue(repo.CreateOrder(dist, prd));

            Device dev = new Device
            {
                Product = prd,
                Uid = "FFFFFFFFFFFF"
            };
            RegisterDeviceResponse res = repo.RegisterDevice(dev).Result;
            Assert.IsFalse(res.Success);
            Assert.IsTrue(res.Error.IndexOf("500") != -1); // This can change as the server api is developed.
        }

        [TestMethod]
        public void formatUid()
        {
            Assert.AreEqual(43981, DistributionRepository.formatUid("ABCD"));
            Assert.AreEqual(2882382797, DistributionRepository.formatUid("ABCDABCD"));
            Assert.AreEqual(188899839028173, DistributionRepository.formatUid("ABCDABCDABCD"));
            Assert.AreEqual(48358358791212543, DistributionRepository.formatUid("ABCDABCDABCDFF"));
        }
    }
}
