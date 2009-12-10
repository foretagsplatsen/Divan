using System;
namespace Divan
{
    public interface ICouchViewDefinition : ICouchViewDefinitionBase
    {
        System.Collections.Generic.IEnumerable<T> All<T>() where T : ICouchDocument, new();
        bool Equals(ICouchViewDefinition other);
        System.Collections.Generic.IEnumerable<T> Key<T>(object key) where T : ICouchDocument, new();
        System.Collections.Generic.IEnumerable<T> KeyStartEnd<T>(object[] start, object[] end) where T : ICouchDocument, new();
        System.Collections.Generic.IEnumerable<T> KeyStartEnd<T>(object start, object end) where T : ICouchDocument, new();
        Divan.Linq.CouchLinqQuery<T> LinqQuery<T>();
        string Map { get; set; }
        CouchQuery Query();
        void ReadJson(Newtonsoft.Json.Linq.JObject obj);
        string Reduce { get; set; }
        void Touch();
        void WriteJson(Newtonsoft.Json.JsonWriter writer);
    }
}
