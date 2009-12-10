using System;
using System.Collections.Generic;
namespace Divan
{
    public interface ICouchDatabase
    {
        void Copy(string sourceDocumentId, string destinationDocumentId);
        void Copy(string sourceDocumentId, string destinationDocumentId, string destinationRev);
        void Copy(ICouchDocument sourceDocument, ICouchDocument destinationDocument);
        int CountDocuments();
        void Create();
        ICouchDocument CreateDocument(ICouchDocument document);
        CouchJsonDocument CreateDocument(string json);
        void Delete();
        void DeleteAttachment(string id, string rev, string attachmentName);
        ICouchDocument DeleteAttachment(ICouchDocument document, string attachmentName);
        void DeleteDocument(string id, string rev);
        void DeleteDocument(ICouchDocument document);
        void DeleteDocuments(string startKey, string endKey);
        void DeleteDocuments(System.Collections.Generic.IEnumerable<ICouchDocument> documents);
        void DeleteDocuments(ICanJson bulk);
        bool Exists();
        void FetchDocumentIfChanged(ICouchDocument document);
        System.Collections.Generic.IEnumerable<CouchJsonDocument> GetAllDocuments();
        System.Collections.Generic.IEnumerable<T> GetAllDocuments<T>() where T : ICouchDocument, new();
        System.Collections.Generic.IEnumerable<CouchDocument> GetAllDocumentsWithoutContent();
        T GetArbitraryDocument<T>(string documentId, Func<T> ctor);
        System.Collections.Generic.IEnumerable<T> GetArbitraryDocuments<T>(System.Collections.Generic.IEnumerable<string> documentIds, Func<T> ctor);
        T GetDocument<T>(string documentId) where T : ICouchDocument, new();
        CouchJsonDocument GetDocument(string documentId);
        System.Collections.Generic.IEnumerable<CouchJsonDocument> GetDocuments(System.Collections.Generic.IEnumerable<string> documentIds);
        System.Collections.Generic.IEnumerable<T> GetDocuments<T>(System.Collections.Generic.IEnumerable<string> documentIds) where T : ICouchDocument, new();
        bool HasAttachment(string documentId, string attachmentName);
        bool HasAttachment(ICouchDocument document, string attachmentName);
        bool HasDocument(ICouchDocument document);
        bool HasDocument(string documentId);
        bool HasDocumentChanged(string documentId, string rev);
        bool HasDocumentChanged(ICouchDocument document);
        void Initialize();
        string Name { get; set; }
        CouchDesignDocument NewDesignDocument(string aName);
        ICouchViewDefinition NewTempView(string designDoc, string viewName, string mapText);
        CouchQuery Query(ICouchViewDefinition view);
        CouchQuery Query(string designName, string viewName);
        Divan.Lucene.CouchLuceneQuery Query(Divan.Lucene.CouchLuceneViewDefinition view);
        CouchQuery QueryAllDocuments();
        System.Net.WebResponse ReadAttachment(string documentId, string attachmentName);
        System.Net.WebResponse ReadAttachment(ICouchDocument document, string attachmentName);
        void ReadDocument(ICouchDocument document);
        Newtonsoft.Json.Linq.JObject ReadDocument(string documentId);
        void ReadDocumentIfChanged(ICouchDocument document);
        string ReadDocumentString(string documentId);
        CouchRequest Request(string path);
        CouchRequest Request();
        CouchRequest RequestAllDocuments();
        bool RunningOnMono();
        T SaveArbitraryDocument<T>(T document);
        void SaveArbitraryDocuments<T>(System.Collections.Generic.IEnumerable<T> documents, bool allOrNothing);
        void SaveArbitraryDocuments<T>(System.Collections.Generic.IEnumerable<T> documents, int chunkCount, bool allOrNothing);
        void SaveArbitraryDocuments<T>(System.Collections.Generic.IEnumerable<T> documents, int chunkCount, IEnumerable<ICouchViewDefinition> views, bool allOrNothing);
        ICouchDocument SaveDocument(ICouchDocument document);
        void SaveDocuments(System.Collections.Generic.IEnumerable<ICouchDocument> documents, int chunkCount, IEnumerable<ICouchViewDefinition> views, bool allOrNothing);
        void SaveDocuments(System.Collections.Generic.IEnumerable<ICouchDocument> documents, bool allOrNothing);
        void SaveDocuments(System.Collections.Generic.IEnumerable<ICouchDocument> documents, int chunkCount, bool allOrNothing);
        ICouchServer Server { get; set; }
        void SynchDesignDocuments();
        void TouchView(string designDocumentId, string viewName);
        void TouchViews(IEnumerable<ICouchViewDefinition> views);
        ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, System.IO.Stream attachmentData, string mimeType);
        ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, byte[] attachmentData, string mimeType);
        ICouchDocument WriteAttachment(ICouchDocument document, string attachmentName, string attachmentData, string mimeType);
        ICouchDocument WriteDocument(string json, string documentId);
        ICouchDocument WriteDocument(ICouchDocument document);
        ICouchDocument WriteDocument(ICouchDocument document, bool batch);
    }
}
