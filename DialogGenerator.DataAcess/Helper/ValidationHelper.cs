using DialogGenerator.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;

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
            try
            {
                JObject _jObject = JObject.Parse(_jsonString);

                return _jObject.IsValid(msSchema, out messages);
            }
            catch(Newtonsoft.Json.JsonReaderException e)
            {
                messages = new List<string>();
                messages.Add(e.Message);
            }
            catch(Exception e)
            {
                messages = new List<string>();
                messages.Add(e.Message);
            }

            return false;
        }
    }
}
