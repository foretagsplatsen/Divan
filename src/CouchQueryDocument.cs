using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// This is used to hold only metadata about a document retrieved from view queries.
    /// </summary>
    public class CouchQueryDocument : CouchDocument
    {
        public string Key { get; set; }

        public override void ReadJson(JObject obj)
        {
            Id = obj["id"].Value<string>();
            Key = obj["key"].Value<string>();
            Rev = (obj["value"].Value<JObject>())["rev"].Value<string>();
        }
    }
}