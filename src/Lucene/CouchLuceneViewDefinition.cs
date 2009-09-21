using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// A definition of a CouchDB Lucene view with a name, an index function and some options, see below.
    /// </summary>
    public class CouchLuceneViewDefinition : CouchViewDefinitionBase, IEquatable<CouchLuceneViewDefinition>
    {     
        /// <summary>
        /// Basic constructor used in ReadJson() etc.
        /// </summary>
        /// <param name="name">View name used in URI.</param>
        /// <param name="doc">A design doc, can also be created on the fly.</param>
        public CouchLuceneViewDefinition(string name, CouchDesignDocument doc) : base(name, doc) { }


        /// <summary>
        /// Constructor used for permanent views, see CouchDesignDocument.
        /// </summary>
        /// <param name="name">View name.</param>
        /// <param name="index">Index function.</param>
        /// <param name="doc">Parent document.</param>
        public CouchLuceneViewDefinition(string name, string index, CouchDesignDocument doc) : base(name, doc)
        {
            Index = index;
        }

        /// <summary>
        /// Copied from http://github.com/rnewson/couchdb-lucene/tree/v0.4/README.md 
        /// 
        /// You must supply a index function in order to enable couchdb-lucene as, by default,
        /// nothing will be indexed. To suppress a document from the index, return null.
        /// It's more typical to return a single Document object which contains everything
        /// you'd like to query and retrieve. You may also return an array of Document
        /// objects if you wish.
        /// 
        /// You may add any number of index views in any number of design documents.
        /// All searches will be constrained to documents emitted by the index functions.
        /// 
        /// Example function: "function(doc) { var ret=new Document(); ret.add(doc.subject); return ret }"
        /// </summary>
        public string Index { get; set; }

        /// <summary>
        /// Copied from http://github.com/rnewson/couchdb-lucene/tree/v0.4/README.md 
        /// 
        /// Lucene has numerous ways of converting free-form text into tokens, these classes are called Analyzer's.
        /// By default, the StandardAnalyzer is used which lower-cases all text, drops common English words
        /// ("the", "and", and so on), among other things. This processing might not always suit you, so you can
        /// choose from several others by setting the "analyzer" field to one of the following values;
        /// 
        ///     brazilian, chinese, cjk, czech, dutch, english, french, german, keyword, porter,
        ///     russian, simple, standard, thai
        /// </summary>
        public string Analyzer { get; set; }

        /// <summary>
        /// Copied from http://github.com/rnewson/couchdb-lucene/tree/v0.4/README.md 
        /// 
        /// The defaults for numerous indexing options can be overridden here. A full list of options follows.
        /// 
        ///     Name: 	Description:                                    Available options:      Default:
        /// 
        ///     field 	the field name to index under 	                user-defined 	        default
        ///     store 	whether the data is stored.
        ///             Value will be returned in the search result. 	yes, no 	            no
        /// 
        ///     index 	whether (and how) the data is indexed 	        analyzed,
        ///                                                             analyzed_no_norms,
        ///                                                             no, not_analyzed,
        ///                                                             not_analyzed_no_norms 	analyzed
        /// 
        /// In Divan we flattened this so that this object has all these three settings instead of a Dictionary:
        /// </summary>
        public string Field { get; set; }
        public bool Store { get; set; }
        public string IndexHow { get; set; }


        public void WriteJson(JsonWriter writer)
        {
            writer.WritePropertyName(Name);
            writer.WriteStartObject();
            if (Analyzer != null)
            {
                writer.WritePropertyName("analyzer");
                writer.WriteValue(Analyzer);
            }
            // Bah, if this gets out of hand we should use a Dictionary<string,string> instead :)
            if (Field != null || Store || IndexHow != null)
            {
                writer.WritePropertyName("defaults");
                writer.WriteStartObject();
                if (Field != null)
                {
                    writer.WritePropertyName("field");
                    writer.WriteValue(Field);
                }
                if (Store)
                {
                    writer.WritePropertyName("store");
                    writer.WriteValue("yes");
                }
                if (IndexHow != null)
                {
                    writer.WritePropertyName("index");
                    writer.WriteValue(IndexHow);
                }
                writer.WriteEndObject();
            }
            writer.WritePropertyName("index");
            writer.WriteValue(Index);
            writer.WriteEndObject();
        }

        public void ReadJson(JObject obj)
        {
            if (obj["analyzer"] != null)
            {
                Analyzer = obj["reduce"].Value<string>();
            }
            if (obj["defaults"] != null)
            {
                var defaults = obj["defaults"];
                if (defaults["field"] != null)
                {
                    Field = defaults["field"].Value<string>();
                }
                if (defaults["store"] != null)
                {
                    Store = (defaults["store"].Value<string>()).Equals("yes");
                }
                if (defaults["index"] != null)
                {
                    IndexHow = defaults["index"].Value<string>();
                }
            }
            Index = obj["index"].Value<string>();
        }

        public CouchLuceneQuery Query()
        {
            return Db().Query(this);
        }

        public override string Path()
        {
			// Todo: "_fti" is hardcoded here.
			// Also, for some odd reason Couchdb-Lucene does not want the "_design/"-prefix in query URLs.
		
            return "_fti/" + WithoutDesignPart(Doc.Id) + "/" + Name;
        }

		static private string WithoutDesignPart(string name) {
			if (name.StartsWith("_design/")) {
				return name.Substring(8);
			}
			return name;
		}
		
        public bool Equals(CouchLuceneViewDefinition other)
        {
            return Name.Equals(other.Name) && Index.Equals(other.Index) && Analyzer.Equals(other.Analyzer) &&
                Store.Equals(other.Store) && Field.Equals(other.Field) && IndexHow.Equals(other.IndexHow);
        }
    }
}