using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Divan
{
    /// <summary>
    /// Basic capability to write and read myself using Newtonsoft.JSON.
    /// Writing is done using JsonWriter in a fast streaming fashion.
    /// Reading is done using JObject "DOM style".
    /// </summary>
    public interface ICanJson
    {
        void WriteJson(JsonWriter writer);
        void ReadJson(JObject obj);
    }
}