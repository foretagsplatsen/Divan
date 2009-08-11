using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// Only used as psuedo doc when doing bulk reads.
    /// </summary>
    public class CouchBulkKeys : ICanJson
    {
        public CouchBulkKeys(IEnumerable<string> ids)
        {
            Ids = ids.ToArray();
        }

        public CouchBulkKeys()
        {
        }

        public CouchBulkKeys(string[] ids)
        {
            Ids = ids;
        }

        public string[] Ids { get; set; }

        #region ICouchBulk Members

        public virtual void WriteJson(JsonWriter writer)
        {
            writer.WritePropertyName("keys");
            writer.WriteStartArray();
            foreach (string id in Ids)
            {
                writer.WriteValue(id);
            }
            writer.WriteEndArray();
        }

        public virtual void ReadJson(JObject obj)
        {
            throw new NotImplementedException();
        }

        public int Count()
        {
            return Ids.Count();
        }

        #endregion
    }
}