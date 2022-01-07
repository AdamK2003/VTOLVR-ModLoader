using System.Collections.Generic;

namespace Core.Classes
{
    public class Material
    {
        public string Name { get; set; }
        public Dictionary<string, string> Textures = new Dictionary<string, string>();
    }
}