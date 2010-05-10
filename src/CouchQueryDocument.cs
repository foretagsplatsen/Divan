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
        	var tmp = obj["value"];
			Rev = tmp.ToString() == "null" ? null : tmp.Value<JObject>()["_rev"].Value<string>(); //Rev is null if the value emitted is not doc or does not contain _rev
        }
    }
}