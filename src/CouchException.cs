using System;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;

namespace Divan
{
    /// <summary>
    /// All Exceptions thrown inside Divan uses this class, MOST of these wrap a WebException
    /// and we extract the HttpStatusCode to make it easily accessible.
    /// </summary>
    [Serializable]
    public class CouchException : Exception
    {
        public HttpStatusCode StatusCode;

        public CouchException()
        {
        }

        public CouchException(string message)
            : base(message)
        {
        }

        public CouchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CouchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static Exception Create(string message)
        {
            return new CouchException(message);
        }

        public static Exception Create(string message, WebException e)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, message + ": {0}", e.Message);
            if (e.Response != null)
            {
                // Pick out status code
                HttpStatusCode code = ((HttpWebResponse) e.Response).StatusCode;

                // Create any specific exceptions we care to use
                if (code == HttpStatusCode.Conflict)
                {
                    return new CouchConflictException(msg, e);
                }
                if (code == HttpStatusCode.NotFound)
                {
                    return new CouchNotFoundException(msg, e);
                }
            }

            // Fall back on generic CouchException
            return new CouchException(msg, e);
        }
    }
}