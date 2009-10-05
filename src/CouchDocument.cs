using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// This is a base class that domain objects can inherit in order to get 
    /// Id and Rev instance variables. You can also implement ICouchDocument yourself if
    /// you are not free to pick this class as your base. Some static methods to read and write
    /// CouchDB documents are also kept here.
    /// 
    /// This class can also be used if you only need to retrieve id and revision from CouchDB.
    /// 
    /// See sample subclasses to understand how to use this class.
    /// </summary>
    public class CouchDocument : ICouchDocument
    {
        public CouchDocument(string id, string rev)
        {
            Id = id;
            Rev = rev;
        }

        public CouchDocument(string id)
        {
            Id = id;
        }

        public CouchDocument()
        {
        }

        public CouchDocument(IDictionary<string, JToken> doc)
            : this(doc["_id"].Value<string>(), doc["_rev"].Value<string>())
        {
        }

        #region ICouchDocument Members

        public string Id { get; set; }
        public string Rev { get; set; }

        public virtual void WriteJson(JsonWriter writer)
        {
            WriteIdAndRev(this, writer);
        }

        public virtual void ReadJson(JObject obj)
        {
            ReadIdAndRev(this, obj);
        }

        #endregion

        public void WriteJsonObject(JsonWriter writer)
        {
            writer.WriteStartObject();
            WriteJson(writer);
            writer.WriteEndObject();
        }

        public static string WriteJson(ICanJson doc)
        {
            var sb = new StringBuilder();
            using (JsonWriter jsonWriter = new JsonTextWriter(new StringWriter(sb, CultureInfo.InvariantCulture)))
            {
                //jsonWriter.Formatting = Formatting.Indented;
                if (!(doc is ISelfContained))
                {
                    jsonWriter.WriteStartObject();
                    doc.WriteJson(jsonWriter);
                    jsonWriter.WriteEndObject();
                } else
                    doc.WriteJson(jsonWriter);

                string result = sb.ToString();
                return result;
            }
        }

        public static void WriteIdAndRev(ICouchDocument doc, JsonWriter writer)
        {
            if (doc.Id != null)
            {
                writer.WritePropertyName("_id");
                writer.WriteValue(doc.Id);
            }
            if (doc.Rev != null)
            {
                writer.WritePropertyName("_rev");
                writer.WriteValue(doc.Rev);
            }
        }

        public static void ReadIdAndRev(ICouchDocument doc, JObject obj)
        {
            doc.Id = obj["_id"].Value<string>();
            doc.Rev = obj["_rev"].Value<string>();
        }

        public static void ReadIdAndRev(ICouchDocument doc, JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName && (reader.Value as string == "_id"))
            {
                reader.Read();
                doc.Id = reader.Value as string;
            }
            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName && (reader.Value as string == "_rev"))
            {
                reader.Read();
                doc.Rev = reader.Value as string;
            }
        }
    }
}