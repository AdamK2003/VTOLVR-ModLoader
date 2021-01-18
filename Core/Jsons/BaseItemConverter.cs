using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace Core.Jsons
{
    class BaseItemConverter : JsonConverter
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
            return objectType.GetTypeInfo().IsClass;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
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
                if (!_propertyMappings.TryGetValue(jp.Name, out var name))
                    name = jp.Name;

                PropertyInfo prop = props.FirstOrDefault(pi =>
                    pi.CanWrite && pi.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == name);

                prop?.SetValue(instance, jp.Value.ToObject(prop.PropertyType, serializer));
            }

            return instance;
        }
    }
}
