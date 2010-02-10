using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// Only used as pseudo doc when doing bulk updates/inserts.
    /// </summary>
    public class CouchBulkDeleteDocuments : CouchBulkDocuments
    {
        public CouchBulkDeleteDocuments(IEnumerable<ICouchDocument> docs) : base(docs)
        {
        }

        public override void WriteJson(JsonWriter writer)
        {
            writer.WritePropertyName("docs");
            writer.WriteStartArray();
            foreach (ICouchDocument doc in Docs)
            {
                writer.WriteStartObject();
                CouchDocument.WriteIdAndRev(doc, writer);
                writer.WritePropertyName("_deleted");
                writer.WriteValue(true);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public override void ReadJson(JObject obj)
        {
            throw new NotImplementedException();
        }
    }
}
