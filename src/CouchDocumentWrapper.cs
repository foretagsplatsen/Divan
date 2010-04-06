using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{

    public class CouchDocumentWrapper<T>: ICouchDocument, ISelfContained
    {
        class MemberWrapper
        {
            public FieldInfo Field;
            public PropertyInfo Property;

            public void SetValue(object instance, object value)
            {
                if (Field == null)
                {
                    Property.SetValue(instance, value, null);
                }
                else
                {
                    Field.SetValue(instance, value);
                }
            }

            public object GetValue(object instance)
            {
                if (Field == null)
                {
                    return Property.GetValue(instance, null);
                }

                return Field.GetValue(instance);
            }
        }

        private T instance;
        private readonly MemberWrapper rev;
        private readonly MemberWrapper id;
        private readonly JsonSerializer serializer = new JsonSerializer();

        protected CouchDocumentWrapper()
        {
            rev = GetFunc("_rev") ?? GetFunc("rev");
            id = GetFunc("_id") ?? GetFunc("id");
        }

        public CouchDocumentWrapper(Func<T> ctor): this()
        {
            instance = ctor();
        }

        public CouchDocumentWrapper(T instance): this()
        {
            this.instance = instance;
        }

        /// <summary>
        /// Gets a function that accesses the value of a property or field
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private static MemberWrapper GetFunc(string name)
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

                return new MemberWrapper { Field = field };
            }

            return new MemberWrapper { Property = prop };
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

        public void WriteJson(JsonWriter writer)
        {
            if (Id == null)
            {
                var tokenWriter = new JTokenWriter();
                serializer.Serialize(tokenWriter, instance);
                var obj = (JObject)tokenWriter.Token;
                obj.Remove("_rev");
                obj.Remove("_id");                
                obj.WriteTo(writer);
            } else
                serializer.Serialize(writer, instance);
        }

        public void ReadJson(JObject obj)
        {
            instance = (T)serializer.Deserialize(new JTokenReader(obj), typeof(T));
            id.SetValue(instance, obj["_id"].Value<string>());
            rev.SetValue(instance, obj["_rev"].Value<string>());
        }

        #endregion
    }
}
