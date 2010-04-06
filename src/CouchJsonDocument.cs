using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A CouchDocument that holds its contents as a parsed JObject DOM which can be used
    /// as a "light weight" base document instead of CouchDocument.
    /// The _id and _rev are held inside the JObject.
    /// </summary>
    public class CouchJsonDocument : ICouchDocument
    {
        public CouchJsonDocument(string json, string id, string rev)
        {
            Obj = JObject.Parse(json);
            Id = id;
            Rev = rev;
        }

        public CouchJsonDocument(string json, string id)
        {
            Obj = JObject.Parse(json);
            Id = id;
        }

        public CouchJsonDocument(string json)
        {
            Obj = JObject.Parse(json);
        }

        public CouchJsonDocument(JObject doc)
        {
            Obj = doc;
        }

        public CouchJsonDocument()
        {
            Obj = new JObject();
        }

        public override string ToString()
        {
            return Obj.ToString();
        }

        public JObject Obj { get; set; }

        #region ICouchDocument Members

        public virtual void WriteJson(JsonWriter writer)
        {
            foreach (JToken token in Obj.Children())
            {
                token.WriteTo(writer);
            }
        }

        // Presume that Obj has _id and _rev
        public void ReadJson(JObject obj)
        {
            Obj = obj;
        }

        public string Rev
        {
            get
            {
                if (Obj["_rev"] == null)
                {
                    return null;
                }
                return Obj["_rev"].Value<string>();
            }
            set { Obj["_rev"] = JToken.FromObject(value); }
        }
        public string Id
        {
            get
            {
                if (Obj["_id"] == null)
                {
                    return null;
                }
                return Obj["_id"].Value<string>();
            }
            set { Obj["_id"] = JToken.FromObject(value); }
        }

        #endregion
    }
}