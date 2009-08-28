using System.Collections.Generic;
using Newtonsoft.Json;

namespace Divan
{
    /// <summary>
    /// A definition of a CouchDB view with a name, a map and a reduce function and a reference to the
    /// owning DesignCouchDocument. 
    /// </summary>
    public class CouchViewDefinition
    {
        /// <summary>
        /// Constructor used to create "on the fly" definitions, like for example for "_all_docs".
        /// </summary>
        /// <param name="name">View name used in URI.</param>
        /// <param name="doc">A design doc, can also be created on the fly.</param>
        public CouchViewDefinition(string name, DesignCouchDocument doc)
        {
            Doc = doc;
            Name = name;
        }

        /// <summary>
        /// Constructor used for permanent views, see CouchDesignDocument.
        /// </summary>
        /// <param name="name">View name.</param>
        /// <param name="map">Map function.</param>
        /// <param name="reduce">Optional reduce function.</param>
        /// <param name="doc">Parent document.</param>
        public CouchViewDefinition(string name, string map, string reduce, DesignCouchDocument doc)
        {
            Doc = doc;
            Name = name;
            Map = map;
            Reduce = reduce;
        }

        public DesignCouchDocument Doc { get; set; }
        public string Name { get; set; }
        public string Map { get; set; }
        public string Reduce { get; set; }

        public CouchRequest Request()
        {
            return Doc.Owner.Request(Path());
        }

        public CouchDatabase Db()
        {
            return Doc.Owner;
        }

        public void WriteJson(JsonWriter writer)
        {
            writer.WritePropertyName(Name);
            writer.WriteStartObject();
            writer.WritePropertyName("map");
            writer.WriteValue(Map);
            if (Reduce != null)
            {
                writer.WritePropertyName("reduce");
                writer.WriteValue(Reduce);
            }
            writer.WriteEndObject();
        }

        public CouchQuery Query()
        {
            return Doc.Owner.Query(this);
        }

        public void Touch()
        {
            Query().Limit(0).GetResult();
        }

        public string Path()
        {
            if (Doc.Id == "_design/")
            {
                return Name;
            }
            return Doc.Id + "/_view/" + Name;
        }

        /// <summary>
        /// Utility methods to make queries shorter.
        /// </summary>
        public IList<T> Key<T>(string key) where T : ICouchDocument, new()
        {
            return Query().Key(key).IncludeDocuments().GetResult().Documents<T>();
        }

        public IList<T> KeyStartEnd<T>(object start, object end) where T : ICouchDocument, new()
        {
            return Query().StartKey(start).EndKey(end).IncludeDocuments().GetResult().Documents<T>();
        }

        public IList<T> KeyStartEnd<T>(object[] start, object[] end) where T : ICouchDocument, new()
        {
            return Query().StartKey(start).EndKey(end).IncludeDocuments().GetResult().Documents<T>();
        }

        public IList<T> All<T>() where T : ICouchDocument, new()
        {
            return Query().IncludeDocuments().GetResult().Documents<T>();
        }
    }
}