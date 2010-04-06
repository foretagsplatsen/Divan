using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Divan.Lucene
{
    /// <summary>
    /// This is a view result from a CouchLuceneQuery. The result is returned as JSON
    /// from CouchDB and parsed into a JObject by Newtonsoft.Json. Basically
    /// the result is a JSON array of document key/score pairs.
    /// </summary>
    public class CouchLuceneViewResult
    {
		private CouchLuceneViewDefinition view;
        public string etag;
        public JObject result;

        public void Result(JObject obj, CouchLuceneViewDefinition aView)
        {
            result = obj;
			view = aView;
        }

		public string ETag()
        {
            return result["etag"].Value<string>();
        }
		
		/// <summary>
		/// The total number of hits for the query. Not all may be returned due to Limit(), see Count().
		/// </summary>
        public int TotalCount()
        {
            return result["total_rows"].Value<int>();
        }

		/// <summary>
		/// Maximum number of hits that CouchDB was allowed to return.
		/// </summary>
        public int Limit()
        {
            return result["limit"].Value<int>();
        }

		/// <summary>
		/// Number of milliseconds spent fetching CouchDB documents.
		/// </summary>
        public int FetchDuration()
        {
            return result["fetch_duration"].Value<int>();
        }
		
		/// <summary>
		/// Number of milliseconds spent executing the actual search in Lucene.
		/// </summary>
		public int SearchDuration()
        {
            return result["search_duration"].Value<int>();
        }
		
		/// <summary>
		/// Number of skipped results in this query. Renamed to Offset() to match API in CouchDB more closely.
		/// </summary>
        public int Offset()
        {
            return result["skip"].Value<int>();
        }

        public JEnumerable<JToken> Rows()
        {
            return result["rows"].Children();
        }


        /// <summary>
        /// Return all hits with all meta info. A hit can be told to retrieve its CouchDocument, 
        /// if you used IncludeDocuments() in the query.
        /// </summary>
        public virtual IEnumerable<CouchLuceneHit> Hits()
        {
            var hits = new List<CouchLuceneHit>();
            foreach (JObject row in Rows())
            {
                hits.Add(new CouchLuceneHit(row));
            }
            return hits;
        }

		/// <summary>
		/// Extract documents from hits or perform a bulk retrieval of the documents
		/// that was returned by this query. Note that this may be a subset of TotalCount().
		/// </summary>
		public virtual IEnumerable<T> GetDocuments<T>() where T : ICouchDocument, new()
        {
		    var docs = new List<T>();

		    var hits = Hits();
            if (!hits.Any())
            {
                return docs;
            }

		    var firstHit = hits.First();
		    var db = view.Db();
            if (firstHit.HasDocument())
            {
                foreach (var hit in hits)
                {
                    docs.Add(hit.Document<T>());
                }
                return docs;
            }
		    var ids = new List<string>();
		    foreach (var hit in hits)
		    {
		        ids.Add(hit.Id());
		    }
		    return db.GetDocuments<T>(ids);
        }
		
		/// <summary>
		/// Returns number of documents returned in this result. See TotalCount() for the total number of hits.
		/// </summary>
        public int Count()
        {
            return result["rows"].Value<JArray>().Count;
        }
    }
}