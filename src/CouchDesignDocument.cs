using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A named design document in CouchDB. Holds CouchViewDefinitions.
    /// </summary>
    public class CouchDesignDocument : CouchDocument, IEquatable<CouchDesignDocument>
    {
        public IList<CouchViewDefinition> Definitions = new List<CouchViewDefinition>();
        public string Language = "javascript";
        public CouchDatabase Owner;

        public CouchDesignDocument(string documentId, CouchDatabase owner)
            : base("_design/" + documentId)
        {
            Owner = owner;
        }

        public CouchDesignDocument()
        {
            
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

        public void RemoveViewNamed(string viewName)
        {
            RemoveView(FindView(viewName));
        }

        private CouchViewDefinition FindView(string name)
        {
            return Definitions.Where(x => x.Name == name).First();
        }

        public void RemoveView(CouchViewDefinition view)
        {
            view.Doc = null;
            Definitions.Remove(view);
        }

        /// <summary>
        /// If this design document is missing in the database,
        /// or if it is different - then we save it overwriting the one in the db.
        /// </summary>
        public void Synch()
        {
            if (!Owner.HasDocument(this)) {
                Owner.SaveDocument(this);
            } else
            {
                var docInDb = Owner.GetDocument<CouchDesignDocument>(Id);
                if (!docInDb.Equals(this)) {
                    // This way we forcefully save our version over the one in the db.
                    Rev = docInDb.Rev;
                    Owner.WriteDocument(this);
                }
            }
        }

        public override void WriteJson(JsonWriter writer)
        {
            WriteIdAndRev(this, writer);
            writer.WritePropertyName("language");
            writer.WriteValue(Language);
            writer.WritePropertyName("views");
            writer.WriteStartObject();
            foreach (var definition in Definitions)
            {
                definition.WriteJson(writer);
            }
            writer.WriteEndObject();
        }

        public override void ReadJson(JObject obj)
        {
            ReadIdAndRev(this, obj);
            Language = obj["language"].Value<string>();
            Definitions = new List<CouchViewDefinition>();
            var views = (JObject)obj["views"];

            foreach (var property in views.Properties())
            {
                var v = new CouchViewDefinition(property.Name, this);
                v.ReadJson((JObject)views[property.Name]);
                Definitions.Add(v);
            }
        }

        public bool Equals(CouchDesignDocument other)
        {
            return Id.Equals(other.Id) && Language.Equals(other.Language) && Definitions.SequenceEqual(other.Definitions);
        }
    }
}