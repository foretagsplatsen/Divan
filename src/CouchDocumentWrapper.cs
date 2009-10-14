using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Divan
{

    public class CouchDocumentWrapper<T>: ICouchDocument, ISelfContained
    {
        class MemberWrapper
        {
            public FieldInfo field;
            public PropertyInfo property;

            public void SetValue(object instance, object value)
            {
                if (field == null)
                    property.SetValue(instance, value, null);
                else
                    field.SetValue(instance, value);
            }

            public object GetValue(object instance)
            {
                if (field == null)
                    return property.GetValue(instance, null);

                return field.GetValue(instance);
            }
        }

        private Func<T> ctor;
        private T instance;
        private MemberWrapper rev;
        private MemberWrapper id;
        private JsonSerializer serializer = new JsonSerializer();

        protected CouchDocumentWrapper()
        {
            rev = GetFunc("_rev") ?? GetFunc("rev");
            id = GetFunc("_id") ?? GetFunc("id");
        }

        public CouchDocumentWrapper(Func<T> ctor): this()
        {
            this.ctor = ctor;
            instance = ctor();
        }

        public CouchDocumentWrapper(T instance): this()
        {
            this.instance = instance;
        }

        /// <summary>
        /// Gets a function that accesses the value of a property or field
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        private MemberWrapper GetFunc(string name)
        {
            var members = typeof(T).GetMember(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (members == null || members.Length == 0)
                return null;

            var prop = members[0] as PropertyInfo;
            if (prop == null)
            {
                var field = members[0] as FieldInfo;
                if (field == null)
                    return null;

                return new MemberWrapper() { field = field };
            }

            return new MemberWrapper() { property = prop };
        }

        #region ICouchDocument Members

        public T Instance { get { return instance; } }

        public string Rev
        {
            get
            {
                if (rev == null) 
                    return null;

                return (string)rev.GetValue(instance);
            }
            set
            {
                if (rev == null)
                    return;

                rev.SetValue(instance, value);
            }
        }

        public string Id
        {
            get
            {
                if (id == null)
                    return null;

                return (string)id.GetValue(instance);
            }
            set
            {
                if (id == null)
                    return;

                id.SetValue(instance, value);
            }
        }

        #endregion

        #region ICanJson Members

        public void WriteJson(Newtonsoft.Json.JsonWriter writer)
        {
            if (Id == null)
            {
                var tokenWriter = new JTokenWriter();
                serializer.Serialize(tokenWriter, instance);
                var obj = tokenWriter.Token as JObject;
                obj.Remove("_id");
                obj.Remove("_rev");
                obj.WriteTo(writer);
            } else
                serializer.Serialize(writer, instance);
        }

        public void ReadJson(Newtonsoft.Json.Linq.JObject obj)
        {
            instance = (T)serializer.Deserialize(new JTokenReader(obj), typeof(T));
            id.SetValue(instance, obj["_id"].Value<string>());
            rev.SetValue(instance, obj["_rev"].Value<string>());
        }

        #endregion
    }
}
