using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A CouchDB HTTP request with all its options. This is where we do the actual HTTP requests to CouchDB.
    /// </summary>
    public class CouchRequest
    {
        private readonly CouchDatabase db;
        private readonly CouchServer server;
        private string etag, etagToCheck;
        public Dictionary<string, string> headers = new Dictionary<string, string>();

        // Query options
        public string method = "GET"; // PUT, DELETE, POST, HEAD
        public string mimeType;
        public string path;
        public string postData;
        public string query;

        public JToken result;

        public CouchRequest(CouchServer server)
        {
            this.server = server;
        }

        public CouchRequest(CouchDatabase db)
        {
            server = db.Server;
            this.db = db;
        }

        public CouchRequest Etag(string value)
        {
            etagToCheck = value;
            headers["If-Modified"] = value;
            return this;
        }

        public CouchRequest Path(string name)
        {
            path = name;
            return this;
        }

        public CouchRequest Query(string name)
        {
            query = name;
            return this;
        }

        public CouchRequest QueryOptions(ICollection<KeyValuePair<string, string>> options)
        {
            if (options == null || options.Count == 0)
            {
                return this;
            }

            var sb = new StringBuilder();
            sb.Append("?");
            foreach (var q in options)
            {
                if (sb.Length > 1)
                {
                    sb.Append("&");
                }
                sb.Append(HttpUtility.UrlEncode(q.Key));
                sb.Append("=");
                sb.Append(HttpUtility.UrlEncode(q.Value));
            }

            return Query(sb.ToString());
        }

        // HEAD requests seem to be problematic under Mono.
        public CouchRequest Head()
        {
            method = "HEAD";
            return this;
        }

        public CouchRequest PostJson()
        {
            MimeTypeJson();
            return Post();
        }

        public CouchRequest Post()
        {
            method = "POST";
            return this;
        }

        public CouchRequest Get()
        {
            method = "GET";
            return this;
        }

        public CouchRequest Put()
        {
            method = "PUT";
            return this;
        }

        public CouchRequest Delete()
        {
            method = "DELETE";
            return this;
        }

        public CouchRequest Data(string data)
        {
            postData = data;
            return this;
        }

        public CouchRequest MimeType(string type)
        {
            mimeType = type;
            return this;
        }

        public CouchRequest MimeTypeJson()
        {
            MimeType("application/json");
            return this;
        }

        public JObject Result()
        {
            return (JObject) result;
        }

        public T Result<T>() where T : JToken
        {
            return (T) result;
        }

        public string Etag()
        {
            return etag;
        }

        public CouchRequest Check(string message)
        {
            try
            {
                if (result == null)
                {
                    Parse();
                }
                if (!result["ok"].Value<bool>())
                {
                    throw CouchException.Create(string.Format(CultureInfo.InvariantCulture, message + ": {0}", result));
                }
                return this;
            }
            catch (WebException e)
            {
                throw CouchException.Create(message, e);
            }
        }

        private HttpWebRequest GetRequest()
        {
            Uri requestUri = new UriBuilder("http", server.Host, server.Port, ((db != null) ? db.Name + "/" : "") + path, query).Uri;

            var request = WebRequest.Create(requestUri) as HttpWebRequest;
            if (request == null)
            {
                throw CouchException.Create("Failed to create request");
            }
            request.Timeout = 3600000; // 1 hour. May use System.Threading.Timeout.Infinite;
            request.Method = method;

            if (mimeType != null)
            {
                request.ContentType = mimeType;
	    }
			
            if (postData != null)
            {
		byte[] bytes = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = bytes.Length;
                using (Stream ps = request.GetRequestStream())
                {
                    ps.Write(bytes, 0, bytes.Length);
                    ps.Close();
                }
            }

            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Request: {0} Method: {1}", requestUri, method));
            return request;
        }

        public JObject Parse()
        {
            return Parse<JObject>();
        }

        public T Parse<T>() where T : JToken
        {
            using (WebResponse response = GetResponse())
            {
		PickETag(response);
                if (etagToCheck != null)
                {
                    if (IsETagValid())
                    {
                        return null;
                    }
                }
		if (method == "HEAD") {
			return null;
		}
                using (Stream stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        using (var textReader = new JsonTextReader(reader))
                        {
                            result = JToken.ReadFrom(textReader); // We know it is a top level JSON JObject.
                        }
                    }
                }
            }
            return (T) result;
        }

        private void PickETag(WebResponse response)
        {
            etag = response.Headers["ETag"];
            if (etag != null)
            {
                etag = etag.EndsWith("\"") ? etag.Substring(1, etag.Length - 2) : etag;
            }
        }

        /// <summary>
        /// Return the request as a plain string instead of trying to parse it.
        /// </summary>
        public string String()
        {
            using (WebResponse response = GetResponse())
            {
		PickETag(response);
                if (etagToCheck != null)
                {
                        if (IsETagValid())
                        {
                            return null;
                        }
                }
		if (method == "HEAD") {
			return null;
		}
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    
                    return reader.ReadToEnd();
                }
            }
        }

        private WebResponse GetResponse()
        {
		return GetRequest().GetResponse();
        }

        public CouchRequest Send()
        {
            using (WebResponse response = GetResponse())
            {
                PickETag(response);
                return this;
            }
        }

        public bool IsETagValid()
        {
            return etagToCheck == etag;
        }
    }
}
