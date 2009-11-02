using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A view query with all its options. A CouchQuery is constructed to hold all query options that
    /// CouchDB views support and to support ETag caching.
    /// A CouchQuery object can be executed multiple times, holds the last result, the ETag for it,
    /// and a reference to the CouchDatabase object used to perform the query.
    /// </summary>
    public class CouchQuery
    {
        public readonly CouchViewDefinition View;

        // Special options
        public bool checkETagUsingHead;
        public Dictionary<string, string> Options = new Dictionary<string, string>();
        public string postData;
        public CouchViewResult Result;

        public CouchQuery(CouchViewDefinition view)
        {
            View = view;
        }

        public void ClearOptions()
        {
            Options = new Dictionary<string, string>();
        }

        /// <summary>
        /// Setting POST data which will automatically trigger the query to be a POST request.
        /// </summary>
        public CouchQuery Data(string data)
        {
            postData = data;
            return this;
        }


        /// <summary>
        /// This is a bulk key request, not to be confused with requests using complex keys, see Key().
        /// </summary>
        public CouchQuery Keys(object[] keys)
        {
            var bulk = new CouchBulkKeys(keys);
            Data(CouchDocument.WriteJson(bulk));
            return this;
        }

        /// <summary>
        /// This is a bulk key request, not to be confused with requests using complex keys, see Key().
        /// </summary>
        public CouchQuery Keys(IList<object> keys)
        {
            var bulk = new CouchBulkKeys(keys.ToArray());
            Data(CouchDocument.WriteJson(bulk));
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery Key(object value)
        {
            Options["key"] = JToken.FromObject(value).ToString();
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery Key(params object[] value)
        {
            Options["key"] = value == null ? "null" : JToken.FromObject(value).ToString();
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery StartKey(object value)
        {
            Options["startkey"] = JToken.FromObject(value).ToString();
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery StartKey(params object[] value)
        {
            Options["startkey"] = value == null ? "null" : JToken.FromObject(value).ToString();
            return this;
        }

        public CouchQuery StartKeyDocumentId(string value)
        {
            Options["startkey_docid"] = value;
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery EndKey(object value)
        {
            Options["endkey"] = JToken.FromObject(value).ToString();
            return this;
        }

        /// <summary>
        /// Any valid JSON value is a valid key. This means:
        ///  null, true, false, a string, a number, a Dictionary (JSON object) or an array (JSON array)
        /// </summary>
        public CouchQuery EndKey(params object[] value)
        {
            Options["endkey"] = value == null ? "null" : JToken.FromObject(value).ToString();
            return this;
        }

        public CouchQuery EndKeyDocumentId(string value)
        {
            Options["endkey_docid"] = value;
            return this;
        }

        public CouchQuery Limit(int value)
        {
            Options["limit"] = value.ToString();
            return this;
        }

        public CouchQuery Stale()
        {
            Options["stale"] = "ok";
            return this;
        }

        public CouchQuery Descending()
        {
            Options["descending"] = "true";
            return this;
        }

        public CouchQuery Skip(int value)
        {
            Options["skip"] = value.ToString();
            return this;
        }

        public CouchQuery Group()
        {
            Options["group"] = "true";
            return this;
        }

        public CouchQuery GroupLevel(int value)
        {
            Options["group_level"] = value.ToString();
            return this;
        }

        public CouchQuery Reduce()
        {
            Options["reduce"] = "true";
            return this;
        }

        public CouchQuery IncludeDocuments()
        {
            Options["include_docs"] = "true";
            return this;
        }

        /// <summary>
        /// Tell this query to do a HEAD request first to see
        /// if ETag has changed and only then do the full request.
        /// This is only interesting if you are reusing this query object.
        /// </summary>
        public CouchQuery CheckETagUsingHead()
        {
            checkETagUsingHead = true;
            return this;
        }

        public CouchGenericViewResult GetResult()
        {
            try
            {
                return GetResult<CouchGenericViewResult>();
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

        public T GetResult<T>() where T : CouchViewResult, new()
        {
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
                Result.Result(json);
                Result.etag = req.Etag();
            }
            return (T) Result;
        }

        public CouchViewResultStream<T> StreamResult<T>() where T: ICanJson, new()
        {
            return new CouchViewResultStream<T>(Request().Stream());
        }
    }
}