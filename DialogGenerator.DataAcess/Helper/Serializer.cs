using Newtonsoft.Json;
using System.IO;

namespace DialogGenerator.DataAccess.Helper
{
    public static class Serializer
    {
        public static void Serialize(object data,string path)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            string _jsonLocal = JsonConvert.SerializeObject(data, settings);

            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            File.WriteAllText(path, _jsonLocal);
        }

        public static T Deserialize<T>(string content)
        {
            var obj = JsonConvert.DeserializeObject<T>(content);

            return obj;
        }
    }
}
