using System;
namespace Divan
{
    public interface ICouchRequest
    {
        CouchRequest AddHeader(string key, string value);
        CouchRequest Check(string message);
        CouchRequest Copy();
        CouchRequest Data(System.IO.Stream dataStream);
        CouchRequest Data(string data);
        CouchRequest Data(byte[] data);
        CouchRequest Delete();
        string Etag();
        CouchRequest Etag(string value);
        CouchRequest Get();
        CouchRequest Head();
        bool IsETagValid();
        CouchRequest MimeType(string type);
        CouchRequest MimeTypeJson();
        T Parse<T>() where T : Newtonsoft.Json.Linq.JToken;
        Newtonsoft.Json.Linq.JObject Parse();
        CouchRequest Path(string name);
        CouchRequest Post();
        CouchRequest PostJson();
        CouchRequest Put();
        CouchRequest Query(string name);
        CouchRequest QueryOptions(System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>> options);
        System.Net.WebResponse Response();
        T Result<T>() where T : Newtonsoft.Json.Linq.JToken;
        Newtonsoft.Json.Linq.JObject Result();
        CouchRequest Send();
        Newtonsoft.Json.JsonTextReader Stream();
        string String();
    }
}
