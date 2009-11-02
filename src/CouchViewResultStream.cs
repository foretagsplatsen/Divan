using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;
using System.Collections;

namespace Divan
{
    /// <summary>
    /// This is a view result from a CouchQuery that can return CouchDocuments for
    /// resulting documents (include_docs) and/or ICanJson documents for the
    /// result values. A value returned from a CouchDB view does not need to be
    /// a CouchDocument.
    /// </summary>
    public class CouchViewResultStream<T> : CouchViewResult, IEnumerable<CouchRecord<T>> , IDisposable where T: ICanJson, new()
    {
        class RecordEnumerator : IEnumerator<CouchRecord<T>>
        {
            JsonReader reader;
            CouchRecord<T> current;
            bool hasMore = true;

            public RecordEnumerator(JsonReader reader)
            {
                this.reader = reader;
            }

            public CouchRecord<T> Current
            {
                get { return current; }
            }

            public void Dispose() { }

            object IEnumerator.Current
            {
                get { return current; }
            }

            public bool MoveNext()
            {
                if (!hasMore)
                    return false;

                var token = JToken.ReadFrom(reader);
                hasMore = reader.Read() && reader.TokenType == JsonToken.StartObject;

                current = new CouchRecord<T>(token as JObject);

                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        JsonReader reader;

        public CouchViewResultStream(JsonReader reader)
        {
            this.reader = reader;

            var header = new JObject();
            
            // start object
            reader.Read();

            while (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                var name = reader.Value.ToString();
                if (name == "rows")
                    continue;

                reader.Read();
                header[name] = new JValue(reader.Value);
            }

            reader.Read();
        }

        public void Dispose()
        {
            reader.Close();
        }

        public IEnumerator<CouchRecord<T>> GetEnumerator()
        {
            return new RecordEnumerator(reader);
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new RecordEnumerator(reader);
        }
    }
}