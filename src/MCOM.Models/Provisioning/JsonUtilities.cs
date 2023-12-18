using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCOM.Models.Provisioning
{
    public class NullToEmptyStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteValue(string.Empty);
            }
            else
            {
                writer.WriteValue(value);
            }
        }
    }

    public class NullToEmptyStringDictionaryConverter : JsonConverter<Dictionary<string, string>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, string> value, JsonSerializer serializer)
        {
            var convertedDictionary = value.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value ?? string.Empty
            );
            serializer.Serialize(writer, convertedDictionary);
        }

        public override Dictionary<string, string> ReadJson(JsonReader reader, Type objectType, Dictionary<string, string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
