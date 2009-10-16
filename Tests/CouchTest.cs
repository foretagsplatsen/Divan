using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace Divan.Test
{
    /// <summary>
    /// Unit tests for Divan. Operates in a separate CouchDB database called divan_unit_tests.
    /// If you are not running a CouchDB on localhost:5984 you will need to edit
    /// the Tests/App.config file.
    /// 
    /// Run from command line using something like:
    /// 	nunit-console2 --labels -run=Divan.Test.CouchTest Tests/bin/Debug/Tests.dll
    /// </summary>
    [TestFixture]
    public class CouchTest
    {
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

        private CouchServer server;
        private CouchDatabase db;
        private const string DbName = "divan_unit_tests";

        [Test]
        public void ShouldCheckChangedDocument()
        {
            CouchJsonDocument doc = db.CreateDocument("{\"CPU\": \"Intel\"}");
            CouchJsonDocument doc2 = db.GetDocument(doc.Id);
            Assert.That(db.HasDocumentChanged(doc), Is.False);
            doc2.Obj["CPU"] = JToken.FromObject("AMD");
            db.WriteDocument(doc2);
            Assert.That(db.HasDocumentChanged(doc), Is.True);
        }

        [Test]
        public void ShouldCountDocuments()
        {
            Assert.That(db.CountDocuments(), Is.EqualTo(0));
            db.CreateDocument("{\"CPU\": \"Intel\"}");
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
        }

        [Test]
        public void ShouldCreateDocument()
        {
            var doc = new CouchJsonDocument("{\"CPU\": \"Intel\"}");
            ICouchDocument cd = db.CreateDocument(doc);
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
            Assert.That(cd.Id, Is.Not.Null);
            Assert.That(cd.Rev, Is.Not.Null);
        }

        [Test]
        public void ShouldCreateDocuments()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            var doc1 = new CouchJsonDocument(doc);
            var doc2 = new CouchJsonDocument(doc);
            IList<ICouchDocument> list = new List<ICouchDocument> {doc1, doc2};
            db.SaveDocuments(list, true);
            Assert.That(db.CountDocuments(), Is.EqualTo(2));
            Assert.That(doc1.Id, Is.Not.Null);
            Assert.That(doc1.Rev, Is.Not.Null);
            Assert.That(doc2.Id, Is.Not.Null);
            Assert.That(doc2.Rev, Is.Not.Null);
            Assert.That(doc1.Id, Is.Not.EqualTo(doc2.Id));
        }

        [Test]
        public void ShouldCreateDocumentWithSpecifiedId()
        {
            const string doc = "{\"_id\": \"foo\"}";
            var doc1 = new CouchJsonDocument(doc);
            db.SaveDocument(doc1);
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
            Assert.That(doc1.Id, Is.EqualTo("foo"));
            Assert.That(doc1.Rev, Is.Not.Null);
        }

        [Test, ExpectedException(typeof (CouchNotFoundException))]
        public void ShouldDeleteDatabase()
        {
            db.Delete();
            Assert.That(server.HasDatabase(db.Name), Is.EqualTo(false));
            server.DeleteDatabase(db.Name); // one more time should fail
        }

        [Test]
        public void ShouldDeleteDocuments()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            CouchJsonDocument doc1 = db.CreateDocument(doc);
            CouchJsonDocument doc2 = db.CreateDocument(doc);
            if (String.Compare(doc1.Id, doc2.Id) < 0)
            {
                db.DeleteDocuments(doc1.Id, doc2.Id);
            }
            else
            {
                db.DeleteDocuments(doc2.Id, doc1.Id);
            }
            Assert.That(db.HasDocument(doc1.Id), Is.False);
            Assert.That(db.HasDocument(doc2.Id), Is.False);
        }

        [Test, ExpectedException(typeof (CouchException))]
        public void ShouldFailCreateDatabase()
        {
            server.CreateDatabase(db.Name); // one more time should fail
        }

        [Test]
        public void ShouldGetDatabaseNames()
        {
            bool result = server.GetDatabaseNames().Contains(db.Name);
            Assert.That(result, Is.EqualTo(true));
        }

        [Test]
        public void ShouldGetDocument()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            CouchJsonDocument oldDoc = db.CreateDocument(doc);
            CouchJsonDocument newDoc = db.GetDocument(oldDoc.Id);
            Assert.That(oldDoc.Id, Is.EqualTo(newDoc.Id));
            Assert.That(oldDoc.Rev, Is.EqualTo(newDoc.Rev));
        }

        [Test]
        public void ShouldGetDocuments()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            CouchJsonDocument doc1 = db.CreateDocument(doc);
            CouchJsonDocument doc2 = db.CreateDocument(doc);
            var ids = new List<string> {doc1.Id, doc2.Id};

            // Bulk request for multiple keys.
            var docs = db.GetDocuments(ids);
            Assert.That(doc1.Id, Is.EqualTo(docs.First().Id));
            Assert.That(doc2.Id, Is.EqualTo(docs.Last().Id));

            var keys = new List<object> { doc1.Id, doc2.Id };
            // Bulk query on a view for multple keys.
            docs = db.QueryAllDocuments().Keys(keys).IncludeDocuments().GetResult().Documents();
            Assert.That(doc1.Id, Is.EqualTo(docs.First().Id));
            Assert.That(doc2.Id, Is.EqualTo(docs.Last().Id));
        }

        [Test]
        public void ShouldReturnNullWhenNotFound()
        {
            var doc = db.GetDocument<CouchJsonDocument>("jadda");
            Assert.That(doc, Is.Null);
            CouchJsonDocument doc2 = db.GetDocument("jadda");
            Assert.That(doc2, Is.Null);
        }

        [Test]
        public void ShouldSaveDocumentWithId()
        {
            var doc = new CouchJsonDocument("{\"_id\":\"123\", \"CPU\": \"Intel\"}");
            ICouchDocument cd = db.SaveDocument(doc);
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
            Assert.That(cd.Id, Is.Not.Null);
            Assert.That(cd.Rev, Is.Not.Null);
        }

        [Test]
        public void ShouldSaveDocumentWithoutId()
        {
            var doc = new CouchJsonDocument("{\"CPU\": \"Intel\"}");
            ICouchDocument cd = db.SaveDocument(doc);
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
            Assert.That(cd.Id, Is.Not.Null);
            Assert.That(cd.Rev, Is.Not.Null);
        }

        [Test]
        public void ShouldStoreGetAndDeleteAttachment()
        {
            var doc = new CouchJsonDocument("{\"CPU\": \"Intel\"}");
            ICouchDocument cd = db.CreateDocument(doc);
            Assert.That(db.HasAttachment(cd), Is.False);
            db.WriteAttachment(cd, "jabbadabba", "text/plain");
            Assert.That(db.HasAttachment(cd), Is.True);
            Assert.That(db.ReadAttachment(cd), Is.EqualTo("jabbadabba"));
            db.WriteAttachment(cd, "jabbadabba-doo", "text/plain");
            Assert.That(db.HasAttachment(cd), Is.True);
            Assert.That(db.ReadAttachment(cd), Is.EqualTo("jabbadabba-doo"));
            db.DeleteAttachment(cd);
            Assert.That(db.HasAttachment(cd), Is.False);
        }

        [Test, ExpectedException(typeof (CouchConflictException))]
        public void ShouldThrowConflictExceptionOnAlreadyExists()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            CouchJsonDocument doc1 = db.CreateDocument(doc);
            var doc2 = new CouchJsonDocument(doc) {Id = doc1.Id};
            db.WriteDocument(doc2);
        }

        [Test, ExpectedException(typeof (CouchConflictException))]
        public void ShouldThrowConflictExceptionOnStaleWrite()
        {
            const string doc = "{\"CPU\": \"Intel\"}";
            CouchJsonDocument doc1 = db.CreateDocument(doc);
            CouchJsonDocument doc2 = db.GetDocument(doc1.Id);
            doc1.Obj["CPU"] = JToken.FromObject("AMD");
            db.SaveDocument(doc1);
            doc2.Obj["CPU"] = JToken.FromObject("Via");
            db.SaveDocument(doc2);
        }

        [Test]
        public void ShouldUseETagForView()
        {
            var design = db.NewDesignDocument("computers");
            design.AddView("by_cpumake",
                           @"function(doc) {
                        emit(doc.CPU, doc);
                    }");
            db.WriteDocument(design);

            CouchJsonDocument doc1 = db.CreateDocument("{\"CPU\": \"Intel\"}");
            db.CreateDocument("{\"CPU\": \"AMD\"}");
            db.CreateDocument("{\"CPU\": \"Via\"}");
            db.CreateDocument("{\"CPU\": \"Sparq\"}");

            CouchQuery query = db.Query("computers", "by_cpumake").StartKey("Intel").EndKey("Via").CheckETagUsingHead();
            // Query has no result yet so should not be cached
            Assert.That(query.IsCachedAndValid(), Is.False);
            query.GetResult();
            // Now it is cached and should be valid
            Assert.That(query.IsCachedAndValid(), Is.True);
            // Make a change invalidating the view
            db.SaveDocument(doc1);
            // It should now be false
            Assert.That(query.IsCachedAndValid(), Is.False);
            query.GetResult();
            // And now it should be cached again
            Assert.That(query.IsCachedAndValid(), Is.True);
            query.GetResult();
            // Still cached of course
            Assert.That(query.IsCachedAndValid(), Is.True);
        }

        [Test]
        public void ShouldWriteDocument()
        {
            var doc = new CouchJsonDocument("{\"_id\":\"123\", \"CPU\": \"Intel\"}");
            ICouchDocument cd = db.WriteDocument(doc);
            Assert.That(db.CountDocuments(), Is.EqualTo(1));
            Assert.That(cd.Id, Is.Not.Null);
            Assert.That(cd.Rev, Is.Not.Null);
        }

        [Test]
        public void ShouldSyncDesignDocuments()
        {
            var design = db.NewDesignDocument("computers");
            design.AddView("by_cpumake",
                           @"function(doc) {
                        emit(doc.CPU, doc);
                    }");
            db.SynchDesignDocuments(); // This writes them to the db.

            var db2 = server.GetDatabase(DbName);
            design = db2.NewDesignDocument("computers");
            design.AddView("by_cpumake",
                           @"function(doc) {
                        emit(doc.CPU, nil);
                    }");
            db2.SynchDesignDocuments(); // This should detect difference and overwrite the one in the db

            Assert.That(db.GetDocument<CouchDesignDocument>("_design/computers").Definitions[0].Map,
                        Is.EqualTo(
                            @"function(doc) {
                        emit(doc.CPU, nil);
                    }"));
        }

        /// <summary>
        /// Test that keys can be given as C# types representing proper JSON values:
        ///  string, number, true, false, null, JSON array and JSON object.
        /// </summary>
        [Test]
        public void QueryKeyShouldGiveProperJsonValue()
        {
            var query = db.Query("test", "test");
            query.Key("a string");
            Assert.That(query.Options["key"].Equals("\"a string\""));
            query.Key(12);
            Assert.That(query.Options["key"].Equals("12"));
            query.Key(-12.0);
            Assert.That(query.Options["key"].Equals("-12.0"));
            query.Key(true);
            Assert.That(query.Options["key"].Equals("true"));
            query.Key(false);
            Assert.That(query.Options["key"].Equals("false"));
            query.Key(null);
            Assert.That(query.Options["key"].Equals("null"));

            query.Key(new[] {"one", "two"});
            var json = Regex.Replace(query.Options["key"], @"\s", ""); // removes all whitespace
            Assert.That(json.Equals("[\"one\",\"two\"]"));

            var dict = new Dictionary<string, string>();
            dict["one"] = "two";
            dict["three"] = "four";
            query.Key(dict);
            json = Regex.Replace(query.Options["key"], @"\s", ""); // removes all whitespace
            Assert.That(json.Equals("{\"one\":\"two\",\"three\":\"four\"}"));

            query.Key("one", "two");
            json = Regex.Replace(query.Options["key"], @"\s", ""); // removes all whitespace
            Assert.That(json.Equals("[\"one\",\"two\"]"));
        }
    }
}