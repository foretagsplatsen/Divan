using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace Divan.Lucene
{
    /// <summary>
    /// Unit tests for the Lucene part in Divan. Operates in a separate CouchDB database called divan_lucene_unit_tests.
    /// Requires a working Couchdb-Lucene installation according to Couchdb-Lucene's documentation.
    /// </summary>
    [TestFixture]
    public class CouchLuceneTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            server = new CouchServer();
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
            var view = design.AddLuceneView("simple", @"function (doc) { var ret = new Document(); ret.add(doc.text); return ret;}");
            db.SynchDesignDocuments();

            db.CreateDocument("{\"text\": \"one two three four\"}");

            var result = view.Query().Q("one").GetResult();
//			var docs = result.GetDocuments<CouchJsonDocument>();
//			var doc = docs.First<CouchJsonDocument>();
			Assert.That(result.Count(), Is.EqualTo(1));
//			Assert.That(doc.Obj["text"].Value<string>(), Is.EqualTo("one two three four"));
        }

/*		[Test]
        public void ShouldHandleEmptyIndex()
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

            var query = view.Query().Q("Via").GetResult();


        }
*/

    }
}