using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A named design document in CouchDB. Holds CouchViewDefinitions.
    /// </summary>
    public class DesignCouchDocument : CouchDocument
    {
        public IList<CouchViewDefinition> Definitions = new List<CouchViewDefinition>();
        public string Language = "javascript";
        public CouchDatabase Owner;

        public DesignCouchDocument(string documentId, CouchDatabase owner)
            : base("_design/" + documentId)
        {
            Owner = owner;
        }

        /// <summary>
        /// Add view without a reduce function.
        /// </summary>
        /// <param name="name">Name of view</param>
        /// <param name="map">Map function</param>
        /// <returns></returns>
        public CouchViewDefinition AddView(string name, string map)
        {
            return AddView(name, map, null);
        }

        /// <summary>
        /// Add view with a reduce function.
        /// </summary>
        /// <param name="name">Name of view</param>
        /// <param name="map">Map function</param>
        /// <param name="reduce">Reduce function</param>
        /// <returns></returns>
        public CouchViewDefinition AddView(string name, string map, string reduce)
        {
            var def = new CouchViewDefinition(name, map, reduce, this);
            Definitions.Add(def);
            return def;
        }

        public override void WriteJson(JsonWriter writer)
        {
            WriteIdAndRev(this, writer);
            writer.WritePropertyName("language");
            writer.WriteValue(Language);
            writer.WritePropertyName("views");
            writer.WriteStartObject();
            foreach (CouchViewDefinition definition in Definitions)
            {
                definition.WriteJson(writer);
            }
            writer.WriteEndObject();
        }

        public override void ReadJson(JObject obj)
        {
            ReadIdAndRev(this, obj);
        }
    }
}