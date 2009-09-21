using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Divan.Lucene
{
    /// <summary>
    /// A Lucene query with all its options. This class overlaps with CouchQuery but I could not find
    /// a nice way to use inheritance and still keep a fluent style interface without going into generics HELL.
    /// 
    /// You can perform all types of queries using Lucene's default query syntax:
    ///     http://lucene.apache.org/java/2_4_0/queryparsersyntax.html
    /// 
    /// The _body field is searched by default which will include the extracted text from all attachments.
    /// </summary>
    public class CouchLuceneQuery
    {
        public readonly CouchLuceneViewDefinition View;

        // Special options
        public bool checkETagUsingHead;
        public Dictionary<string, string> Options = new Dictionary<string, string>();

        public string postData;
        public CouchLuceneViewResult Result;

        public CouchLuceneQuery(CouchLuceneViewDefinition view)
        {
            View = view;
        }

        public void ClearOptions()
        {
            Options = new Dictionary<string, string>();
        }

        /// <summary>
        /// The analyzer used to convert the query string into a query object. 
        /// </summary>
        public CouchLuceneQuery Analyzer(string value)
        {
            Options["analyzer"] = value;
            return this;
        }


        /// <summary>
        /// Specify a JSONP callback wrapper. The full JSON result will be prepended
        /// with this parameter and also placed with parentheses.
        /// </summary>
        public CouchLuceneQuery Callback(string value)
        {
            Options["callback"] = value;
            return this;
        }

        /// <summary>
        /// Setting this to true disables response caching (the query is executed every time)
        /// and indents the JSON response for readability.
        /// </summary>
        public CouchLuceneQuery Debug()
        {
            Options["debug"] = "true";
            return this;
        }


        /// <summary>
        /// Usually couchdb-lucene determines the Content-Type of its response based on the
        /// presence of the Accept header. If Accept contains "application/json", you get
        /// "application/json" in the response, otherwise you get "text/plain;charset=utf8".
        /// Some tools, like JSONView for FireFox, do not send the Accept header but do render
        /// "application/json" responses if received. Setting force_json=true forces all response 
        /// to "application/json" regardless of the Accept header.
        /// </summary>
        public CouchLuceneQuery ForceJson()
        {
            Options["force_json"] = "true";
            return this;
        }

        public CouchLuceneQuery IncludeDocuments()
        {
            Options["include_docs"] = "true";
            return this;
        }

        public CouchLuceneQuery Limit(int value)
        {
            Options["limit"] = value.ToString();
            return this;
        }

        /// <summary>
        /// The query to run (e.g, subject:hello). If not specified, the default field is searched.
        /// </summary>
        public CouchLuceneQuery Q(string value)
        {
            Options["q"] = value;
            return this;
        }

        /// <summary>
        /// (EXPERT) if true, returns a json response with a rewritten query and term frequencies.
        /// This allows correct distributed scoring when combining the results from multiple nodes.
        /// </summary>
        public CouchLuceneQuery Rewrite()
        {
            Options["rewrite"] = "true";
            return this;
        }

        public CouchLuceneQuery Skip(int value)
        {
            Options["skip"] = value.ToString();
            return this;
        }

        /// <summary>
        /// The fields to sort on. Prefix with / for ascending order
        /// and \ for descending order (ascending is the default if not specified).
        /// </summary>
        public CouchLuceneQuery Sort(params object[] value)
        {
            if (value != null)
            {
                Options["sort"] = JToken.FromObject(value).ToString();
            }
            return this;
        }

        /// <summary>
        /// If you set the stale option to ok, couchdb-lucene may not perform any
        /// refreshing on the index. Searches may be faster as Lucene caches important
        /// data (especially for sorting). A query without stale=ok will use the latest
        /// data committed to the index.
        /// </summary>
        public CouchLuceneQuery Stale()
        {
            Options["stale"] = "ok";
            return this;
        }

        /// <summary>
        /// Tell this query to do a HEAD request first to see
        /// if ETag has changed and only then do the full request.
        /// This is only interesting if you are reusing this query object.
        /// </summary>
        public CouchLuceneQuery CheckETagUsingHead()
        {
            checkETagUsingHead = true;
            return this;
        }

        public CouchLuceneViewResult GetResult()
        {
            try
            {
                return GetResult<CouchLuceneViewResult>();
            }
            catch (WebException e)
            {
                throw CouchException.Create("Query failed", e);
            } 
        }

        public bool IsCachedAndValid()
        {
            // If we do not have a result it is not cached
            if (Result == null)
            {
                return false;
            }
            CouchRequest req = View.Request().QueryOptions(Options);
            req.Etag(Result.etag);
            return req.Head().Send().IsETagValid();
        }

        public string String()
        {
            return Request().String();
        }


        public CouchRequest Request()
        {
            var req = View.Request().QueryOptions(Options);
            if (postData != null)
            {
                req.Data(postData).Post();
            }
            return req;
        }

        public T GetResult<T>() where T : CouchLuceneViewResult, new()
        {
            if (Options["q"] == null)
            {
                throw CouchException.Create("Lucene query failed, you need to specify Q(<Lucene-query-string>).");
            }
            var req = Request();

            if (Result == null)
            {
                Result = new T();
            }
            else
            {
                // Tell the request what we already have
                req.Etag(Result.etag);
                if (checkETagUsingHead)
                {
                    // Make a HEAD request to avoid transfer of data
                    if (req.Head().Send().IsETagValid())
                    {
                        return (T) Result;
                    }
                    // Set back to GET before proceeding below
                    req.Get();
                }
            }

            JObject json = req.Parse();
            if (json != null) // ETag did not match, view has changed
            {
                Result.Result(json, View);
                Result.etag = req.Etag();
            }
            return (T) Result;
        }
    }
}