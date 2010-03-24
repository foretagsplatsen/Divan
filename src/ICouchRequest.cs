using System;
namespace Divan
{
    public interface ICouchRequest
    {
        ICouchRequest AddHeader(string key, string value);
        ICouchRequest Check(string message);
        ICouchRequest Copy();
        ICouchRequest Data(System.IO.Stream dataStream);
        ICouchRequest Data(string data);
        ICouchRequest Data(byte[] data);
        ICouchRequest Delete();
        string Etag();
        ICouchRequest Etag(string value);
        ICouchRequest Get();
        ICouchRequest Head();
        bool IsETagValid();
        ICouchRequest MimeType(string type);
        ICouchRequest MimeTypeJson();
        T Parse<T>() where T : Newtonsoft.Json.Linq.JToken;
        Newtonsoft.Json.Linq.JObject Parse();
        ICouchRequest Path(string name);
        ICouchRequest Post();
        ICouchRequest PostJson();
        ICouchRequest Put();
        ICouchRequest Query(string name);
        ICouchRequest QueryOptions(System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>> options);
        System.Net.WebResponse Response();
        T Result<T>() where T : Newtonsoft.Json.Linq.JToken;
        Newtonsoft.Json.Linq.JObject Result();
        ICouchRequest Send();
        Newtonsoft.Json.JsonTextReader Stream();
        string String();
    }
}
