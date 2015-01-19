using System;
using System.Net;

namespace Divan
{
    /// <summary>
    /// Represents a CouchDB HTTP 409 conflict.
    /// </summary>
    public class CouchConflictException : CouchException
    {
        public CouchConflictException(string msg, Exception e) : base(msg, e)
        {
        }

        public CouchConflictException(string msg, Exception e, HttpStatusCode statusCode) : this (msg, e) {
            StatusCode = statusCode;
        }
    }
}