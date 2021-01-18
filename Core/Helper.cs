using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace Core
{
    public static class Helper
    {
        public static JObject JObjectTryParse(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JObject.Parse(content);
            }
            catch (Exception e)
            {
                exception = e;
                return null;
            }
        }
        public static T DeserializeObject<T>(string content, out Exception exception)
        {
            try
            {
                exception = null;
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception e)
            {
                exception = e;
                return default;
            }
        }
    }
}
