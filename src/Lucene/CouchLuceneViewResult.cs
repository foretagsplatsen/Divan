using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Divan
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
		/// Perform a bulk retrieval of the documents that was returned by this
		/// query. Note that this may be a subset of TotalCount().
		/// </summary>
		public virtual IList<T> GetDocuments<T>() where T : ICouchDocument, new()
        {
            var ids = new List<string>();
            foreach (JObject row in Rows())
            {
                ids.Add(row["id"].Value<string>());
            }
            return view.Db().GetDocuments<T>(ids);
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