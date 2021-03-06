﻿using System;
using System.Linq;
using Banzai.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Banzai.Json
{
    public class TypeJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var aggregateType = (Type)value;
            string serializationString;
            
            if (aggregateType.IsGenericType)
            {
                Type nodeType = aggregateType.GetGenericTypeDefinition();
                Type argType = aggregateType.GetGenericArguments()[0];

                var argString = GetTypeName(argType);
                var nodeString = GetTypeName(nodeType);
                nodeString = nodeString.Substring(0, nodeString.IndexOf("`", StringComparison.Ordinal));

                serializationString = $"{nodeString}[{argString}]";
            }
            else
            {
                serializationString = GetTypeName((Type)value);
            }

            serializer.Serialize(writer, serializationString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string typeName;

            if (reader.ValueType == typeof(string))
            {
                typeName = (string)reader.Value;
            }
            else
            {
                JObject jsonObject = JObject.Load(reader);
                var properties = jsonObject.Properties().ToList();

                typeName = (string)properties[0].Value;    
            }

            var typeNames = typeName.Split(new []{'[',']'}, StringSplitOptions.RemoveEmptyEntries);

            string nodeTypeName = typeNames[0];
            bool isGeneric = typeNames.Length > 1;
            Type nodeType;

            if (isGeneric)
            {
                nodeTypeName += "`" + (typeNames.Length - 1);
                nodeType = GetType(nodeTypeName);
                string subjectTypeName = typeNames[1];
                Type subjectType = GetType(subjectTypeName);
                nodeType = nodeType.MakeGenericType(subjectType);
            }
            else
            {
                nodeType = GetType(nodeTypeName);
            }
            return nodeType;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Type));
        }

        private static string GetTypeName(Type type)
        {
            if (!TypeAbbreviationCache.TryGetName(type, out var retrievedName))
            {
                retrievedName = type.AssemblyQualifiedName;
            }

            return retrievedName;
        }

        private static Type GetType(string typeName)
        {
            if (!TypeAbbreviationCache.TryGetType(typeName, out var retrievedType))
            {
                retrievedType = Type.GetType(typeName);
            }

            return retrievedType;
        }

    }
}