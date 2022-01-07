using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Classes;
using Core.Jsons;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Valve.Newtonsoft.Json.Serialization;

namespace Core.JsonConverters
{
    public class BaseItemConverter : JsonConverter
    {
        private readonly Dictionary<string, string> _propertyMappings = new Dictionary<string, string>
        {
            // old | new

            // 3.2.8
            {"dll file", BaseItem.JDllFile},
            {"public id", BaseItem.JPublicID},
            {"is public", BaseItem.JIsPublic},
            {"last edit", BaseItem.JLastEdit},
            {"preview image", BaseItem.JPreviewImage},
            {"web preview image", BaseItem.JWebPreviewImage},

            // Some older version
            {"Name", BaseItem.JName},
            {"Description", BaseItem.JDescription},
            {"Tagline", BaseItem.JTagline},
            {"Version", BaseItem.JVersion},
            {"Dll File", BaseItem.JDllFile},
            {"Last Edit", BaseItem.JLastEdit},
            {"Source", BaseItem.JRepository},
            {"Preview Image", BaseItem.JPreviewImage},
            {"Web Preview Image", BaseItem.JWebPreviewImage},
            {"Dependencies", BaseItem.JDependencies},
            {"Public ID", BaseItem.JPublicID},
            {"Is Public", BaseItem.JIsPublic},
            {"Unlisted", BaseItem.JUnlisted}
        };

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            BaseItem instance = new BaseItem();
            var props = objectType.GetTypeInfo().DeclaredProperties.ToList();

            JObject jo = JObject.Load(reader);
            foreach (JProperty jp in jo.Properties())
            {
                string name = jp.Name;
                // Checks to see if any old properties are present
                if (_propertyMappings.TryGetValue(jp.Name, out string updatedName))
                {
                    name = updatedName;
                }

                // Loops through all the variables in the class BaseItem
                for (int i = 0; i < props.Count; i++)
                {
                    if (!props[i].CanWrite)
                        continue;
                    
                    // Checks if it has the attribute and the names match
                    var hasAttribute = props[i].GetCustomAttribute<JsonPropertyAttribute>();
                    if (hasAttribute != null &&
                        hasAttribute.PropertyName == name)
                    {
                        // Sets the instance variable to the converted json object
                        object newValue = jp.Value.ToObject(props[i].PropertyType, serializer);
                        Logger.Log($"Setting {name} to value of {newValue}({jp.Value})");
                        props[i].SetValue(instance,newValue);
                        break;
                    }
                }
            }

            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JsonObjectContract contract =
                (JsonObjectContract)serializer.ContractResolver.ResolveContract(value.GetType());

            writer.WriteStartObject();
            foreach (var property in contract.Properties)
            {
                if (property.PropertyType == typeof(DirectoryInfo))
                    continue;
                
                writer.WritePropertyName(property.PropertyName);
                
                if (property.PropertyType == typeof(List<Material>))
                {
                    List<Material> list = property.ValueProvider.GetValue(value) as List<Material>;
                    if (list == null)
                    {
                        writer.WriteNull();
                        continue;
                    }
                    
                    writer.WriteStartArray();
                    for (int i = 0; i < list.Count; i++)
                    {
                        Material currentMat = list[i];
                        writer.WriteStartObject();
                        
                        writer.WritePropertyName("Name");
                        writer.WriteValue(currentMat.Name);

                        writer.WritePropertyName("Textures");
                        foreach (var keyPair in currentMat.Textures)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName(keyPair.Key);
                            writer.WriteValue(keyPair.Value);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    continue;
                }

                if (property.PropertyType == typeof(List<string>))
                {
                    List<string> list = property.ValueProvider.GetValue(value) as List<string>;
                    if (list == null)
                    {
                        writer.WriteNull();
                        continue;
                    }

                    writer.WriteStartArray();
                    for (int i = 0; i < list.Count; i++)
                    {
                        writer.WriteValue(list[i]);
                    }

                    writer.WriteEndArray();
                    continue;
                }

                writer.WriteValue(property.ValueProvider.GetValue(value));
            }

            writer.WriteEndObject();
        }
    }
}