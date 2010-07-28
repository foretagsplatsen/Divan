using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System;
using Divan.Lucene;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Divan
{
    /// <summary>
    /// A CouchDatabase corresponds to a named CouchDB database in a specific CouchServer.
    /// This is the main API to work with CouchDB. One useful approach is to create your own subclasses
    /// for your different databases.
    /// </summary>
    public class CouchDatabase : ICouchDatabase
    {
        private string name;
        public readonly IList<CouchDesignDocument> DesignDocuments = new List<CouchDesignDocument>();

        public CouchDatabase()
        {
            Name = "default";
        }

        public CouchDatabase(ICouchServer server)
            : this()
        {
            Server = server;
        }

        public CouchDatabase(string name, ICouchServer server)
        {
            Name = name;
            Server = server;
        }

        public string Name
        {
            get
            {
                if (Server == null)
                    return name;
                return Server.DatabasePrefix + name;
            }
            set
            {
                name = value;
            }
        }

        public ICouchServer Server { get; set; }

        public bool RunningOnMono()
        {
            return Server.RunningOnMono;
        }

        public CouchDesignDocument NewDesignDocument(string aName)
        {
            var newDoc = new CouchDesignDocument(aName, this);
            DesignDocuments.Add(newDoc);
            return newDoc;
        }

        /// <summary>
        /// Only to be used when developing.
        /// </summary>
        public ICouchViewDefinition NewTempView(string designDoc, string viewName, string mapText)
        {
            var doc = NewDesignDocument(designDoc);
            var view = doc.AddView(viewName, "function (doc) {" + mapText + "}");
            doc.Synch();
            return view;
        }

        /// <summary>
        /// Currently the logic is that the code is always the master.
        /// And we also do not remove design documents in the database that
        /// we no longer have in code.
        /// </summary>
        public void SynchDesignDocuments()
        {
            foreach (var doc in DesignDocuments)
            {
                doc.Synch();
            }
        }

        public ICouchRequest Request()
        {
            return new CouchRequest(this);
        }

        public ICouchRequest Request(string path)
        {
            return (new CouchRequest(this)).Path(path);
        }

        public int CountDocuments()
        {
            return (Request().Parse())["doc_count"].Value<int>();
        }

        public ICouchRequest RequestAllDocuments()
        {
            return Request("_all_docs");
        }

        /// <summary>
        /// Return all documents in the database as CouchJsonDocuments.
        /// This method is only practical for testing purposes.
        /// </summary>
        /// <returns>A list of all documents.</returns>
        public IEnumerable<CouchJsonDocument> GetAllDocuments()
        {
            return QueryAllDocuments().IncludeDocuments().GetResult().Documents<CouchJsonDocument>();
        }

        /// <summary>
        /// Return all documents in the database using a supplied
        /// document type implementing ICouchDocument.
        /// This method is only practical for testing purposes.
        /// </summary>
        /// <typeparam name="T">The document type to use.</typeparam>
        /// <returns>A list of all documents.</returns>
        public IEnumerable<T> GetAllDocuments<T>() where T : ICouchDocument, new()
        {
            return QueryAllDocuments().IncludeDocuments().GetResult().Documents<T>();
        }

        /// <summary>
        /// Return all documents in the database, but only with id and revision.
        /// CouchDocument does not contain the actual content.
        /// </summary>
        /// <returns>List of documents</returns>
        public IEnumerable<CouchDocument> GetAllDocumentsWithoutContent()
        {
            QueryAllDocuments().GetResult().ValueDocuments<CouchDocument>();

            var list = new List<CouchDocument>();
            JObject json = RequestAllDocuments().Parse();
            foreach (JObject row in json["rows"])
            {
                list.Add(new CouchDocument(row["id"].ToString(), (row["value"])["rev"].ToString()));
            }
            return list;
        }

        /// <summary>
        /// Initialize CouchDB database by saving new or changed design documents into it.
        /// Override if needed in subclasses.
        /// </summary>
        public virtual void Initialize()
        {
            SynchDesignDocuments();
        }

        public bool Exists()
        {
            return Server.HasDatabase(Name);
        }

        /// <summary>
        /// Check first if database exists, and if it does not - create it and initialize it.
        /// </summary>
        public void Create()
        {
            if (!Exists())
            {
                Server.CreateDatabase(Name);
                Initialize();
            }
        }

        public void Delete()
        {
            if (Exists())
            {
                Server.DeleteDatabase(Name);
            }
        }

        /// <summary>
        /// Write a document given as plain JSON and a document id. A document may already exist in db and will then be overwritten.
        /// </summary>
        /// <param name="json">Document as a JSON string</param>
        /// <param name="documentId">Document identifier</param>
        /// <returns>A new CouchJsonDocument</returns>
        public ICouchDocument WriteDocument(string json, string documentId)
        {
            return WriteDocument(new CouchJsonDocument(json, documentId));
        }

        /// <summary>
        /// Write a CouchDocument or ICouchDocument, it may already exist in db and will then be overwritten.
        /// </summary>
        /// <param name="document">Couch document</param>
        /// <returns>Couch Document with new Rev set.</returns>
        /// <remarks>This relies on the document to already have an id.</remarks>
        public ICouchDocument
            WriteDocument(ICouchDocument document)
        {
            return WriteDocument(document, false);
        }

        public T SaveArbitraryDocument<T>(T document)
        {
            return ((CouchDocumentWrapper<T>)SaveDocument(new CouchDocumentWrapper<T>(document))).Instance;
        }

        /// <summary>
        /// This is a convenience method that creates or writes a ICouchDocument depending on if
        /// it has an id or not. If it does not have an id we create the document and let CouchDB allocate
        /// an id. If it has an id we use WriteDocument which will overwrite the existing document in CouchDB.
        /// </summary>
        /// <param name="document">ICouchDocument</param>
        /// <returns>ICouchDocument with new Rev set.</returns>
        public ICouchDocument SaveDocument(ICouchDocument document)
        {
            var reconcilingDoc = document as IReconcilingDocument;
            ICouchDocument savedDoc;
            try
            {
                savedDoc = document.Id == null ? 
                    CreateDocument(document) : 
                    WriteDocument(document);
            }
            catch (CouchConflictException)
            {
                if (reconcilingDoc == null)
                    throw;

                // can't handle a brand-new document
                if (String.IsNullOrEmpty(reconcilingDoc.Rev))
                    throw;

                switch (reconcilingDoc.ReconcileBy)
                {
                    case ReconcileStrategy.None:
                        throw;
                    default:
                        reconcilingDoc.Reconcile(reconcilingDoc.GetDatabaseCopy(this));
                        SaveDocument(reconcilingDoc);
                        break;
                }

                savedDoc = reconcilingDoc;
            }

            if (reconcilingDoc != null)
                reconcilingDoc.SaveCommited();

            return savedDoc;
        }

        /// <summary>
        /// Write a CouchDocument or ICouchDocument, it may already exist in db and will then be overwritten.
        /// </summary>
        /// <param name="document">Couch document</param>
        /// <param name="batch">True if we don't want to wait for flush (commit).</param>
        /// <returns>Couch Document with new Rev set.</returns>
        /// <remarks>This relies on the document to already have an id.</remarks>
        public ICouchDocument WriteDocument(ICouchDocument document, bool batch)
        {
            if (document.Id == null)
            {
                throw CouchException.Create(
                    "Failed to write document using PUT because it lacks an id, use CreateDocument instead to let CouchDB generate an id");
            }
            JObject result =
                Request(document.Id).Query(batch ? "?batch=ok" : null).Data(CouchDocument.WriteJson(document)).Put().Check("Failed to write document").Result();
            document.Id = result["id"].Value<string>(); // Not really needed
            document.Rev = result["rev"].Value<string>();

            return document;
        }

        /// <summary>
        /// Add an attachment to an existing ICouchDocument, it may already exist in db and will then be overwritten.
        /// </summary>
        /// <param name="document">Couch document</param>
        /// <param name="attachmentName">Name of the attachment.</param>
        /// <param name="attachmentData">The attachment data.</param>
        /// <param name="mimeType">The MIME type for the attachment.</param>
        /// <returns>The document.</returns>
        /// <remarks>This relies on the document to already have an id.</remarks>
        public ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, string attachmentData, string mimeType)
        {
            var byteData = Encoding.UTF8.GetBytes(attachmentData);
            return WriteAttachment(document, attachmentName, byteData, mimeType);
        }

        /// <summary>
        /// Add an attachment to an existing ICouchDocument, it may already exist in db and will then be overwritten.
        /// </summary>
        /// <param name="document">Couch document</param>
        /// <param name="attachmentName">Name of the attachment.</param>
        /// <param name="attachmentData">The attachment data.</param>
        /// <param name="mimeType">The MIME type for the attachment.</param>
        /// <returns>The document.</returns>
        /// <remarks>This relies on the document to already have an id.</remarks>
        public ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, byte[] attachmentData, string mimeType)
        {
            if (document.Id == null)
            {
                throw CouchException.Create(
                    "Failed to add attachment to document using PUT because it lacks an id");
            }

            JObject result =
                Request(document.Id + "/" + attachmentName).Query("?rev=" + document.Rev).Data(attachmentData).MimeType(mimeType).Put().Check("Failed to write attachment")
                    .Result();
            document.Id = result["id"].Value<string>(); // Not really neeed
            document.Rev = result["rev"].Value<string>();

            return document;
        }

        /// <summary>
        /// Writes the attachment.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="attachmentName">Name of the attachment.</param>
        /// <param name="attachmentData">The attachment data.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>The document.</returns>
        /// <remarks>This relies on the document to already have an id.</remarks>
        public ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, Stream attachmentData, string mimeType)
        {
            if (document.Id == null)
            {
                throw CouchException.Create(
                    "Failed to add attachment to document using PUT because it lacks an id");
            }

            JObject result =
                Request(document.Id + "/" + attachmentName).Query("?rev=" + document.Rev).Data(attachmentData).MimeType(mimeType).Put().Check("Failed to write attachment")
                    .Result();
            document.Id = result["id"].Value<string>(); // Not really neeed
            document.Rev = result["rev"].Value<string>();

            return document;
        }

        /// <summary>
        /// Read a ICouchDocument with an id even if it has not changed revision.
        /// </summary>
        /// <param name="document">Document to fill.</param>
        public void ReadDocument(ICouchDocument document)
        {
            document.ReadJson(ReadDocument(document.Id));
        }

        /// <summary>
        /// Read the attachment for an ICouchDocument.
        /// </summary>
        /// <param name="document">Document to read.</param>
        /// <param name="attachmentName">Name of the attachment.</param>
        /// <returns></returns>
        public WebResponse ReadAttachment(ICouchDocument document, string attachmentName)
        {
            return ReadAttachment(document.Id, attachmentName);
        }

        /// <summary>
        /// First use HEAD to see if it has indeed changed.
        /// </summary>
        /// <param name="document">Document to fill.</param>
        public void FetchDocumentIfChanged(ICouchDocument document)
        {
            if (HasDocumentChanged(document))
            {
                ReadDocument(document);
            }
        }

        /// <summary>
        /// Read a CouchDocument or ICouchDocument, this relies on the document to obviously have an id.
        /// We also check the revision so that we can avoid parsing JSON if the document is unchanged.
        /// </summary>
        /// <param name="document">Document to fill.</param>
        public void ReadDocumentIfChanged(ICouchDocument document)
        {
            JObject result = Request(document.Id).Etag(document.Rev).Parse();
            if (result == null)
            {
                return;
            }
            document.ReadJson(result);
        }

        /// <summary>
        /// Read a couch document given an id, this method does not have enough information to do caching.
        /// </summary>
        /// <param name="documentId">Document identifier</param>
        /// <returns>Document Json as JObject</returns>
        public JObject ReadDocument(string documentId)
        {
            try
            {
                return Request(documentId).Parse();
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to read document", e);
            }
        }

        /// <summary>
        /// Read a couch document given an id, this method does not have enough information to do caching.
        /// </summary>
        /// <param name="documentId">Document identifier</param>
        /// <returns>Document Json as string</returns>
        public string ReadDocumentString(string documentId)
        {
            try
            {
                return Request(documentId).String();
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to read document: " + e.Message, e);
            }
        }

        /// <summary>
        /// Read a couch attachment given a document id, this method does not have enough information to do caching.
        /// </summary>
        /// <param name="documentId">Document identifier</param>
        /// <returns>Document attachment</returns>
        public WebResponse ReadAttachment(string documentId, string attachmentName)
        {
            try
            {
                return Request(documentId + "/" + attachmentName).Response();
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to read document: " + e.Message, e);
            }
        }

        /// <summary>
        /// Create a CouchDocument given JSON as a string. Uses POST and CouchDB will allocate a new id.
        /// </summary>
        /// <param name="json">Json data to store.</param>
        /// <returns>Couch document with data, id and rev set.</returns>
        /// <remarks>POST which may be problematic in some environments.</remarks>
        public CouchJsonDocument CreateDocument(string json)
        {
            return (CouchJsonDocument)CreateDocument(new CouchJsonDocument(json));
        }

        /// <summary>
        /// Create a given ICouchDocument in CouchDB. Uses POST and CouchDB will allocate a new id and overwrite any existing id.
        /// </summary>
        /// <param name="document">Document to store.</param>
        /// <returns>Document with Id and Rev set.</returns>
        /// <remarks>POST which may be problematic in some environments.</remarks>
        public ICouchDocument CreateDocument(ICouchDocument document)
        {
            try
            {
                JObject result = Request().Data(CouchDocument.WriteJson(document)).PostJson().Check("Failed to create document").Result();
                document.Id = result["id"].Value<string>();
                document.Rev = result["rev"].Value<string>();
                return document;
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to create document", e);
            }
        }

        public void SaveArbitraryDocuments<T>(IEnumerable<T> documents, bool allOrNothing)
        {
            SaveDocuments(documents.Select(doc => new CouchDocumentWrapper<T>(doc)).Cast<ICouchDocument>(), allOrNothing);
        }

        /// <summary>
        /// Create or update a list of ICouchDocuments in CouchDB. Uses POST and CouchDB will 
        /// allocate new ids if the documents lack them.
        /// </summary>
        /// <param name="documents">List of documents to store.</param>
        /// <remarks>POST may be problematic in some environments.</remarks>
        public void SaveDocuments(IEnumerable<ICouchDocument> documents, bool allOrNothing)
        {
            var bulk = new CouchBulkDocuments(documents);
            try
            {
                var result = Request("_bulk_docs")
                    .Data(CouchDocument.WriteJson(bulk))
                    .Query("?all_or_nothing=" + allOrNothing.ToString().ToLower())
                    .PostJson()
                    .Parse<JArray>();

                int index = 0;
                foreach (var document in documents)
                {
                    document.Id = (result[index])["id"].Value<string>();
                    document.Rev = (result[index])["rev"].Value<string>();
                    ++index;
                }                
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to create bulk documents", e);
            }
        }

        public void SaveArbitraryDocuments<T>(IEnumerable<T> documents, int chunkCount, IEnumerable<ICouchViewDefinition> views, bool allOrNothing)
        {
            SaveDocuments(
                documents.Select(doc => new CouchDocumentWrapper<T>(doc)).Cast<ICouchDocument>(),
                chunkCount,
                views,
                allOrNothing);
        }

        /// <summary>
        /// Create or updates documents in bulk fashion, chunk wise. Optionally access given view 
        /// after each chunk to trigger reindexing.
        /// </summary>
        /// <param name="documents">List of documents to store.</param>
        /// <param name="chunkCount">Number of documents to store per "POST"</param>
        /// <param name="views">List of views to touch per chunk.</param>
        public void SaveDocuments(IEnumerable<ICouchDocument> documents, int chunkCount, IEnumerable<ICouchViewDefinition> views, bool allOrNothing)
        {
            var chunk = new List<ICouchDocument>(chunkCount);
            int counter = 0;

            foreach (ICouchDocument doc in documents)
            {
                // Do we have a chunk ready to create?
                if (counter == chunkCount)
                {
                    counter = 0;
                    SaveDocuments(chunk, allOrNothing);
                    TouchViews(views);
                    /* Skipping separate thread for now, ASP.Net goes bonkers...
                    (new Thread(
                        () => GetView<CouchPermanentViewResult>(designDocumentName, viewName, ""))
                    {
                        Name = "View access in background", Priority = ThreadPriority.BelowNormal
                    }).Start(); */

                    chunk = new List<ICouchDocument>(chunkCount);
                }
                counter++;
                chunk.Add(doc);
            }

            SaveDocuments(chunk, allOrNothing);
            TouchViews(views);
        }

        public void TouchViews(IEnumerable<ICouchViewDefinition> views)
        {
            //var timer = new Stopwatch();
            if (views != null)
            {
                foreach (var view in views)
                {
                    if (view != null)
                    {
                        //timer.Reset();
                        //timer.Start();
                        view.Touch();
                        //timer.Stop();
                        //Server.Debug("Update view " + view.Path() + ":" + timer.ElapsedMilliseconds + " ms");
                    }
                }
            }
        }

        /// <summary>
        /// Create documents in bulk fashion, chunk wise. 
        /// </summary>
        /// <param name="documents">List of documents to store.</param>
        /// <param name="chunkCount">Number of documents to store per "POST"</param>
        public void SaveDocuments(IEnumerable<ICouchDocument> documents, int chunkCount, bool allOrNothing)
        {
            SaveDocuments(documents, chunkCount, null, allOrNothing);
        }

        public void SaveArbitraryDocuments<T>(IEnumerable<T> documents, int chunkCount, bool allOrNothing)
        {
            SaveArbitraryDocuments(documents, chunkCount, null, allOrNothing);
        }
                
        public IEnumerable<CouchJsonDocument> GetDocuments(IEnumerable<string> documentIds)
        {
            return GetDocuments<CouchJsonDocument>(documentIds);
        }
        
        public IEnumerable<T> GetDocuments<T>(IEnumerable<string> documentIds) where T : ICouchDocument, new()
        {
            var bulk = new CouchBulkKeys(documentIds.Cast<object>());
            return QueryAllDocuments().Data(CouchDocument.WriteJson(bulk)).IncludeDocuments().GetResult().Documents<T>();
        }

        public T GetDocument<T>(string documentId) where T : ICouchDocument, new()
        {
            var doc = new T { Id = documentId };
            try
            {
                ReadDocument(doc);
            }
            catch (CouchNotFoundException)
            {
                return default(T);
            }
            return doc;
        }

        public T GetArbitraryDocument<T>(string documentId, Func<T> ctor)
        {
            var doc = new CouchDocumentWrapper<T>(ctor);
            doc.Id = documentId;
            try
            {
                ReadDocument(doc);
            }
            catch (CouchNotFoundException)
            {
                return default(T);
            }
            return doc.Instance;
        }

        public IEnumerable<T> GetArbitraryDocuments<T>(IEnumerable<string> documentIds, Func<T> ctor)
        {
            var bulk = new CouchBulkKeys(documentIds.Cast<object>());
            return QueryAllDocuments().Data(CouchDocument.WriteJson(bulk)).IncludeDocuments().GetResult().ArbitraryDocuments(ctor);
        }

        public CouchJsonDocument GetDocument(string documentId)
        {
            try
            {
                try
                {
                    return new CouchJsonDocument(Request(documentId).Parse());
                }
                catch (WebException e)
                {
                    throw CouchException.Create("Failed to get document", e);
                }
            }
            catch (CouchNotFoundException)
            {
                return null;
            }
        }
        /// <summary>
        /// Query a view by name (that we know exists in CouchDB). This method then creates
        /// a CouchViewDefinition on the fly. Better to use existing CouchViewDefinitions.
        /// </summary>
        public CouchQuery Query(string designName, string viewName)
        {
            return Query(new CouchViewDefinition(viewName, NewDesignDocument(designName)));
        }

        public CouchQuery Query(ICouchViewDefinition view)
        {
            return new CouchQuery(view);
        }


        public CouchLuceneQuery Query(CouchLuceneViewDefinition view)
        {
            return new CouchLuceneQuery(view);
        }

        public CouchQuery QueryAllDocuments()
        {
            return Query(null, "_all_docs");
        }

        public void TouchView(string designDocumentId, string viewName)
        {
            Query(designDocumentId, viewName).Limit(0).GetResult();
        }

        public void DeleteDocument(ICouchDocument document)
        {
            DeleteDocument(document.Id, document.Rev);
        }

        public ICouchDocument DeleteAttachment(ICouchDocument document, string attachmentName)
        {
            JObject result = Request(document.Id + "/" + attachmentName).Query("?rev=" + document.Rev).Delete().Check("Failed to delete attachment").Result();
            document.Id = result["id"].Value<string>(); // Not really neeed
            document.Rev = result["rev"].Value<string>();
            return document;
        }

        public void DeleteAttachment(string id, string rev, string attachmentName)
        {
            Request(id + "/" + attachmentName).Query("?rev=" + rev).Delete().Check("Failed to delete attachment");
        }

        public void DeleteDocument(string id, string rev)
        {
            Request(id).Query("?rev=" + rev).Delete().Check("Failed to delete document");
        }

        /// <summary>
        /// Delete documents in key range. This method needs to retrieve
        /// revisions and then use them to post a bulk delete. Couch can not
        /// delete documents without being told about their revisions.
        /// </summary>
        public void DeleteDocuments(string startKey, string endKey)
        {
            var docs = QueryAllDocuments().StartKey(startKey).EndKey(endKey).GetResult().RowDocuments().Cast<ICouchDocument>();
            DeleteDocuments(docs);
        }

        /// <summary>
        /// Delete documents in bulk fashion.
        /// </summary>
        /// <param name="documents">Array of documents to delete.</param>
        public void DeleteDocuments(IEnumerable<ICouchDocument> documents)
        {
            DeleteDocuments(new CouchBulkDeleteDocuments(documents));
        }

        /// <summary>
        /// Delete documents in bulk fashion.
        /// </summary>
        public void DeleteDocuments(ICanJson bulk)
        {
            try
            {
                var result = Request("_bulk_docs").Data(CouchDocument.WriteJson(bulk)).PostJson().Parse<JArray>();
                for (int i = 0; i < result.Count(); i++)
                {
                    //documents[i].id = (result[i])["id"].Value<string>();
                    //documents[i].rev = (result[i])["rev"].Value<string>();
                    if ((result[i])["error"] != null)
                    {
                        throw CouchException.Create(string.Format(CultureInfo.InvariantCulture,
                            "Document with id {0} was not deleted: {1}: {2}",
                            (result[i])["id"].Value<string>(), (result[i])["error"], (result[i])["reason"]));
                    }
                }
            }
            catch (WebException e)
            {
                throw CouchException.Create("Failed to bulk delete documents", e);
            }
        }

        public bool HasDocument(ICouchDocument document)
        {
            return HasDocument(document.Id);
        }

        public bool HasAttachment(ICouchDocument document, string attachmentName)
        {
            return HasAttachment(document.Id, attachmentName);
        }

        public bool HasDocumentChanged(ICouchDocument document)
        {
            return HasDocumentChanged(document.Id, document.Rev);
        }

        public bool HasDocumentChanged(string documentId, string rev)
        {
            return Request(documentId).Head().Send().Etag() != rev;
        }

        public bool HasDocument(string documentId, string revision)
        {
            try
            {
                Request(documentId).QueryOptions(new Dictionary<string, string> {{"Rev", revision}}).Head().Send();
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public bool HasDocument(string documentId)
        {
            try
            {
                Request(documentId).Head().Send();
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public bool HasAttachment(string documentId, string attachmentName)
        {
            try
            {
                Request(documentId + "/" + attachmentName).Head().Send();
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies a document based on its document id.
        /// </summary>
        /// <param name="sourceDocumentId">The source document id.</param>
        /// <param name="destinationDocumentId">The destination document id.</param>
        /// <remarks>Use this method when the destination document does not exist.</remarks>
        public void Copy(string sourceDocumentId, string destinationDocumentId)
        {
            try
            {
                Request(sourceDocumentId)
                    .AddHeader("Destination", destinationDocumentId)
                    .Copy()
                    .Send()
                    .Parse();

                // TODO add the following check statement.
                // Currently on Windows the COPY command does not return an ok=true pair. This might be
                // a bug in the implementation, but once it is sorted out the check should be added.
                //.Check("Error copying document");
            }
            catch (WebException e)
            {
                throw new CouchException(e.Message, e);
            }
        }

        /// <summary>
        /// Copies a document based on its document id and replaces another existing document.
        /// </summary>
        /// <param name="sourceDocumentId">The source document id.</param>
        /// <param name="destinationDocumentId">The destination document id.</param>
        /// <param name="destinationRev">The destination rev.</param>
        /// <remarks>Use this method when the destination document already exists</remarks>
        public void Copy(string sourceDocumentId, string destinationDocumentId, string destinationRev)
        {
            try
            {
                Request(sourceDocumentId)
                    .AddHeader("Destination", destinationDocumentId + "?rev=" + destinationRev)
                    .Copy()
                    .Parse();

                // TODO add the following check statement.
                // Currently on Windows the COPY command does not return an ok=true pair. This might be
                // a bug in the implementation, but once it is sorted out the check should be added.
                //.Check("Error copying document");
            }
            catch (WebException e)
            {
                throw new CouchException(e.Message, e);
            }
        }

        /// <summary>
        /// Copies the specified source document to the destination document, replacing it.
        /// </summary> 
        /// <param name="sourceDocument">The source document.</param>
        /// <param name="destinationDocument">The destination document.</param>
        /// <remarks>This method does not update the destinationDocument object.</remarks>
        public void Copy(ICouchDocument sourceDocument, ICouchDocument destinationDocument)
        {
            Copy(sourceDocument.Id, destinationDocument.Id, destinationDocument.Rev);
        }
    }
}
