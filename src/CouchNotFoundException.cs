using System;
using System.Net;

namespace Divan
{
    /// <summary>
    /// Represents a HttpStatusCode of 404, document not found.
    /// </summary>
    public class CouchNotFoundException : CouchException
    {
        public CouchNotFoundException(string msg, Exception e) : base(msg, e)
        {
        }

        public CouchNotFoundException(string msg, Exception e, HttpStatusCode statusCode) : this (msg, e) {
            StatusCode = statusCode;
        }
    }
}