using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Divan.Test.Linq
{
    /// <summary>
    /// Unit tests for the Lucene part in Divan. Operates in a separate CouchDB database called divan_lucene_unit_tests.
    /// Requires a working Couchdb-Lucene installation according to Couchdb-Lucene's documentation.
    /// Run from command line using something like:
    /// 	nunit-console2 --labels -run=Divan.Test.Lucene src/bin/Debug/Divan.dll
    /// </summary>
    [TestFixture]
    public class CouchLinqTest
    {
        private class Car : CouchDocument
        {
            public string Make;
            public string Model;
            public int HorsePowers;

            public Car()
            {
                // This constructor is needed by Divan
            }

            public Car(string make, string model, int hps)
            {
                Make = make;
                Model = model;
                HorsePowers = hps;
            }
            #region CouchDocument Members

            public override void WriteJson(JsonWriter writer)
            {
                // This will write id and rev
                base.WriteJson(writer);

                writer.WritePropertyName("docType");
                writer.WriteValue("car");
                writer.WritePropertyName("Make");
                writer.WriteValue(Make);
                writer.WritePropertyName("Model");
                writer.WriteValue(Model);
                writer.WritePropertyName("Hps");
                writer.WriteValue(HorsePowers);
            }

            public override void ReadJson(JObject obj)
            {
                // This will read id and rev
                base.ReadJson(obj);

                Make = obj["Make"].Value<string>();
                Model = obj["Model"].Value<string>();
                HorsePowers = obj["Hps"].Value<int>();
            }

            #endregion
        }
        
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            var host = ConfigurationManager.AppSettings["CouchHost"] ?? "localhost";
            var port = Convert.ToInt32(ConfigurationManager.AppSettings["CouchPort"] ?? "5984");
            server = new CouchServer(host, port);
            db = server.GetNewDatabase(DbName);
            Car car = null;
            
            for (int i = 0; i < 10; i++)
            {
                car = new Car("Saab", "93", 170 + i);
                db.SaveDocument(car);
            }
            tempView = db.NewTempView("test", "test", "if (doc.docType && doc.docType == 'car') emit(doc.Hps, doc);");
        }

        [Test]
        public void ShouldHandleRangeQuery()
        {
            var linqCars = tempView.LinqQuery<Car>();

            var fastCars = new List<Car>(from c in linqCars where c.HorsePowers >= 175 select c);

            Assert.AreEqual(5, fastCars.Count());
        }

        [Test]
        public void ShouldHandleOrQuery()
        {
            var linqCars = tempView.LinqQuery<Car>();

            var twoCars = new List<Car>(from c in linqCars where c.HorsePowers == 175 || c.HorsePowers == 176 select c);

            Assert.AreEqual(2, twoCars.Count());
        }

        [Test]
        public void ShouldHandleTransformationQuery()
        {
            var linqCars = tempView.LinqQuery<Car>();

            var hps = new int[] { 176, 177 };
            var twoMoreCars = new List<string>(from c in linqCars where hps.Contains(c.HorsePowers) select c.Make + " " + c.Model + " with " + c.HorsePowers + "HPs");

            Assert.AreEqual("Saab 93 with 176HPsSaab 93 with 177HPs", twoMoreCars.Aggregate("", (s, e) => s + e));
        }

        [TearDown]
        public void TearDown()
        {
            db.Delete();
        }

        #endregion

        private CouchServer server;
        private CouchDatabase db;
        private CouchViewDefinition tempView;
        private const string DbName = "divan_linq_unit_tests";
    }
}