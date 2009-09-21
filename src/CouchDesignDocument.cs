using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A named design document in CouchDB. Holds CouchViewDefinitions and CouchLuceneViewDefinitions (if you use Couchdb-Lucene).
    /// </summary>
    public class CouchDesignDocument : CouchDocument, IEquatable<CouchDesignDocument>
    {
        public IList<CouchViewDefinition> Definitions = new List<CouchViewDefinition>();
        
        // This List is only used if you also have Couchdb-Lucene installed
        public IList<CouchLuceneViewDefinition> LuceneDefinitions = new List<CouchLuceneViewDefinition>();

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
        /// Add Lucene fulltext view.
        /// </summary>
        /// <param name="name">Name of view</param>
        /// <param name="index">Index function</param>
        /// <returns></returns>
        public CouchLuceneViewDefinition AddLuceneView(string name, string index)
        {
            var def = new CouchLuceneViewDefinition(name, index, this);
            LuceneDefinitions.Add(def);
            return def;
        }

        /// <summary>
        /// Add a Lucene view with a predefined index function that will index EVERYTHING.
        /// </summary>
        /// <returns></returns>
        public CouchLuceneViewDefinition AddLuceneViewIndexEverything(string name)
        {
            return AddLuceneView(name,
                                 @"function(doc) {
                                    var ret = new Document();

                                    function idx(obj) {
                                    for (var key in obj) {
                                        switch (typeof obj[key]) {
                                        case 'object':
                                        idx(obj[key]);
                                        break;
                                        case 'function':
                                        break;
                                        default:
                                        ret.add(obj[key]);
                                        break;
                                        }
                                    }
                                    };

                                    idx(doc);

                                    if (doc._attachments) {
                                    for (var i in doc._attachments) {
                                        ret.attachment(""attachment"", i);
                                    }
                                    }}");
        }

        // All these three methods duplicated for Lucene views, perhaps we should hold them all in one List?
        public void RemoveLuceneViewNamed(string viewName)
        {
            RemoveLuceneView(FindLuceneView(viewName));
        }

        private CouchLuceneViewDefinition FindLuceneView(string name)
        {
            return LuceneDefinitions.Where(x => x.Name == name).First();
        }

        public void RemoveLuceneView(CouchLuceneViewDefinition view)
        {
            view.Doc = null;
            LuceneDefinitions.Remove(view);
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
            
            // If we have Lucene definitions we write them too
            if (LuceneDefinitions.Count > 0)
            {
                writer.WritePropertyName("fulltext");
                writer.WriteStartObject();
                foreach (var definition in LuceneDefinitions)
                {
                    definition.WriteJson(writer);
                }
                writer.WriteEndObject();
            }
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

            var fulltext = (JObject)obj["fulltext"];
            // If we have Lucene definitions we read them too
            if (fulltext != null)
            {
                foreach (var property in fulltext.Properties())
                {
                    var v = new CouchLuceneViewDefinition(property.Name, this);
                    v.ReadJson((JObject) views[property.Name]);
                    LuceneDefinitions.Add(v);
                }
            }
        }

        public bool Equals(CouchDesignDocument other)
        {
            return Id.Equals(other.Id) && Language.Equals(other.Language) && Definitions.SequenceEqual(other.Definitions);
        }
    }
}