using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace Divan
{
    /// <summary>
    /// Unit tests for the Lucene part in Divan. Operates in a separate CouchDB database called divan_lucene_unit_tests.
    /// </summary>
    [TestFixture]
    public class CouchLuceneTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            server = new CouchServer("192.168.9.32");
            db = server.GetNewDatabase(DbName);
        }

        [TearDown]
        public void TearDown()
        {
            //db.Delete();
        }

        #endregion

        private CouchServer server;
        private CouchDatabase db;
        private const string DbName = "divan_lucene_unit_tests";

        [Test]
        public void ShouldHandleTrivialQuery()
        {
            var design = db.NewDesignDocument("test");
            var view = design.AddLuceneView("noindex", 
                            @"function(doc) {
                              return null;
                            }
                            ");
            db.WriteDocument(design);

            CouchJsonDocument doc1 = db.CreateDocument("{\"CPU\": \"Intel\"}");
            db.CreateDocument("{\"CPU\": \"AMD\"}");
            db.CreateDocument("{\"CPU\": \"Via\"}");
            db.CreateDocument("{\"CPU\": \"Sparq\"}");

            var query = view.Query().Query("Via").GetResult();


        }


    }
}