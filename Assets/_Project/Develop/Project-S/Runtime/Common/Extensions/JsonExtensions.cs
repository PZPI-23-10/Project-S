using Newtonsoft.Json;

namespace Project_S.Runtime.Common.Extensions
{
    public static class JsonExtensions
    {
        public static string ToJson(this object obj) =>
            JsonConvert.SerializeObject(obj);

        public static T FromJson<T>(this string json, bool ignoreException = false)
        {
            if (!ignoreException)
                return JsonConvert.DeserializeObject<T>(json);

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}