using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace Divan
{
    /// <summary>
    /// Conflict reconcilliation strategies supported by Divan
    /// </summary>
    public enum ReconcileStrategy
    {
        /// <summary>
        /// If a conflict occurs, the exception is propagated to the application
        /// </summary>
        None,
        /// <summary>
        /// The document will retain additional state information to identify which fields
        /// have been modified since the last save, and use that to merge with the database
        /// copy. The merge will be performed automatically, by reflecting over all public
        /// and non-public fields and properties of the document.
        /// </summary>
        AutoMergeFields,
        /// <summary>
        /// The document will retain additional state information to identify which fields
        /// have been modified since the last save, and use that to merge with the database
        /// copy. The merge will be delegated to the document instance, by invoking the 
        /// Reconcile() method with the database document.
        /// </summary>
        ManualMergeFields
    }

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
    public class CouchDocument : ICouchDocument, IReconcilingDocument
    {
        private ReconcileStrategy reconcileBy = ReconcileStrategy.None;
        private CouchDocument sourceData;
        
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

        public virtual ReconcileStrategy ReconcileBy
        {
            get { return reconcileBy; }
            set { reconcileBy = value; }
        }

        /// <summary>
        /// The data set used to construct this document
        /// </summary>
        protected CouchDocument SourceData { get { return sourceData; } }

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

            if (ReconcileBy != ReconcileStrategy.None)
            {
                sourceData = GetType().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as CouchDocument;
                
                // set this to prevent infinite recursion
                sourceData.ReconcileBy = ReconcileStrategy.None;
                sourceData.ReadJson(obj);
            }
        }

        #endregion

        /// <summary>
        /// Automatically reconcile the database copy with the target instance. This method
        /// uses reflection to perform the reconcilliation, and as such won't perform as well
        /// as other version, but is available for low-occurance scenarios
        /// </summary>
        /// <param name="databaseCopy"></param>
        protected void AutoReconcile(ICouchDocument databaseCopy)
        {
            var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
                // if we haven't changed the field, 
                if (field.GetValue(sourceData) == field.GetValue(this))
                    field.SetValue(this, field.GetValue(databaseCopy));

            foreach (var prop in properties)
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0)
                    continue;
                else if (prop.GetValue(sourceData, null) == prop.GetValue(this, null))
                    prop.SetValue(this, prop.GetValue(databaseCopy, null), null);
        }

        protected CouchDocument AutoClone()
        {
            var doc = GetType().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as CouchDocument;
            var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
                field.SetValue(doc, field.GetValue(this));

            foreach (var prop in properties)
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0)
                    continue;
                else
                    prop.SetValue(doc, prop.GetValue(this, null), null);

            return doc;
        }

        protected virtual CouchDocument Clone()
        {
            var doc = GetType().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as CouchDocument;
            doc.Rev = Rev;
            doc.Id = Id;

            return doc;
        }

        public void SaveCommited()
        {
            switch (ReconcileBy)
            {
                case ReconcileStrategy.AutoMergeFields:
                    sourceData = AutoClone();
                    break;
                case ReconcileStrategy.ManualMergeFields:
                    sourceData = Clone();
                    break;
            }

            sourceData.ReconcileBy = ReconcileStrategy.None;
        }

        /// <summary>
        /// Called by the runtime when a conflict is detected during save. The supplied parameter
        /// is the database copy of the document being saved.
        /// </summary>
        /// <param name="databaseCopy"></param>
        public virtual void Reconcile(ICouchDocument databaseCopy)
        {
            if (ReconcileBy == ReconcileStrategy.AutoMergeFields)
            {
                AutoReconcile(databaseCopy);
                return;
            }

            Rev = databaseCopy.Rev;
        }

        public virtual IReconcilingDocument GetDatabaseCopy(CouchDatabase db)
        {
            return db.GetDocument<CouchDocument>(Id);
        }

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