using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Divan.Test.Autoreconcile
{
    /// <summary>
    /// Unit tests for the Lucene part in Divan. Operates in a separate CouchDB database called divan_lucene_unit_tests.
    /// Requires a working Couchdb-Lucene installation according to Couchdb-Lucene's documentation.
    /// Run from command line using something like:
    /// 	nunit-console2 --labels -run=Divan.Test.Lucene src/bin/Debug/Divan.dll
    /// </summary>
    [TestFixture]
    public class CouchAutoreconcileTest
    {
        private class Car : CouchDocument
        {
            public string Make;
            public string Model;
            public int HorsePowers;

            public Car()
            {
                ReconcileBy = ReconcileStrategy.AutoMergeFields;
            }

            public Car(string make, string model, int hps): this()
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

            public override IReconcilingDocument GetDatabaseCopy(CouchDatabase db)
            {
                return db.GetDocument<Car>(Id);
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
        }

        [TearDown]
        public void TearDown()
        {
            db.Delete();
        }

        #endregion

        [Test, ExpectedException(ExceptionType = typeof(CouchConflictException))]
        public void ShouldCauseConflict()
        {
            var doc = new Car("Hoopty", "Type R", 5);
            doc.ReconcileBy = ReconcileStrategy.None;
            doc = db.SaveDocument(doc) as Car;

            var rev = doc.Rev;
            doc = db.SaveDocument(doc) as Car;

            doc.Rev = rev;
            db.SaveDocument(doc);
        }

        [Test]
        public void ShouldHandleConflict()
        {
            var doc = new Car("Hoopty", "Type R", 5);
            doc = db.SaveDocument(doc) as Car;

            var rev = doc.Rev;
            doc = db.SaveDocument(doc) as Car;

            doc.Rev = rev;
            db.SaveDocument(doc);

            Assert.That(doc.Rev.StartsWith("3"), "Incorrect revision");
        }

        [Test]
        public void ShouldResolveConflict()
        {
            var doc = new Car("Hoopty", "Type R", 5);
            doc = db.SaveDocument(doc) as Car;

            var doc2 = db.GetDocument<Car>(doc.Id);
            doc2.Make = "Slightly Better";
            doc2.HorsePowers = 6;
            doc2.Id = doc.Id;
            doc2.Rev = doc.Rev;

            doc.Model = "Type S";
            doc = db.SaveDocument(doc) as Car;

            doc2 = db.SaveDocument(doc2) as Car;

            Assert.That(doc2.Rev.StartsWith("3"), "Incorrect revision");
            Assert.AreEqual("Slightly Better", doc2.Make);
            Assert.AreEqual("Type S", doc2.Model);
            Assert.AreEqual(6, doc2.HorsePowers);
        }

        [Test, ExpectedException(ExceptionType = typeof(CouchConflictException))]
        public void ShouldCauseConflictOnNewDoc()
        {
            var doc = new Car("Hoopty", "Type R", 5);
            doc = db.SaveDocument(doc) as Car;

            var doc2 = new Car("Some other", "Car", 1000000);
            doc2.Id = doc.Id;
            db.SaveDocument(doc2);
        }

        private CouchServer server;
        private CouchDatabase db;
        private const string DbName = "divan_reconcile_unit_tests";
    }
}