using DialogGenerator.Core;
using DialogGenerator.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DialogGenerator.DataAccess.Helper
{
    public static class ValidationHelper
    {
        private static readonly JSchema msSchema;

        static ValidationHelper()
        {
            msSchema = _generateSchema();
        }

        private static JSchema _generateSchema()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema schema = generator.Generate(typeof(JSONObjectsTypesList));

            return schema;
        }

        public static bool Validate(string _jsonString, out IList<string> messages)
        {
            JObject _jObject = JObject.Parse(_jsonString);

            return _jObject.IsValid(msSchema, out messages);
        }
    }
}
