using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Valve.Newtonsoft.Json.Serialization;

namespace Core.Jsons
{
    public class BaseItemConverter : JsonConverter
    {
        private readonly Dictionary<string, string> _propertyMappings = new Dictionary<string, string>
        {
            // old | new

            // 3.2.8
            { "dll file", BaseItem.JDllFile},
            { "public id", BaseItem.JPublicID},
            { "is public", BaseItem.JIsPublic},
            { "last edit", BaseItem.JLastEdit },
            { "preview image", BaseItem.JPreviewImage},
            { "web preview image", BaseItem.JWebPreviewImage },

            // Some older version
            { "Name", BaseItem.JName },
            { "Description", BaseItem.JDescription },
            { "Tagline", BaseItem.JTagline },
            { "Version", BaseItem.JVersion },
            { "Dll File", BaseItem.JDllFile },
            { "Last Edit", BaseItem.JLastEdit },
            { "Source", BaseItem.JRepository },
            { "Preview Image", BaseItem.JPreviewImage },
            { "Web Preview Image", BaseItem.JWebPreviewImage },
            { "Dependencies", BaseItem.JDependencies },
            { "Public ID", BaseItem.JPublicID },
            { "Is Public", BaseItem.JIsPublic },
            { "Unlisted", BaseItem.JUnlisted }

        };

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object instance = Activator.CreateInstance(objectType);
            var props = objectType.GetTypeInfo().DeclaredProperties.ToList();

            JObject jo = JObject.Load(reader);
            foreach (JProperty jp in jo.Properties())
            {
                if (!_propertyMappings.TryGetValue(jp.Name, out string name))
                {
                    name = jp.Name;
                }

                for (int i = 0; i < props.Count; i++)
                {
                    if (!props[i].CanWrite)
                        continue;
                    var hasAttribute = props[i].GetCustomAttribute<JsonPropertyAttribute>();
                    if (hasAttribute != null &&
                        hasAttribute.PropertyName == name)
                    {
                        hasAttribute.PropertyName = name;
                        props[i].SetValue(instance, jp.Value.ToObject(props[i].PropertyType, serializer));
                        break;
                    }
                }
            }

            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JsonObjectContract contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(value.GetType());

            writer.WriteStartObject();
            foreach (var property in contract.Properties)
            {
                if (property.PropertyType == typeof(DirectoryInfo))
                    continue;

                writer.WritePropertyName(property.PropertyName);

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
