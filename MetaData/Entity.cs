using System.Collections.Generic;

namespace ODataViewer
{
    public class Entity : EDMElement
    {
        public Dictionary<string, string> Keys { get; } = new Dictionary<string, string>();
        public Dictionary<string, EDMProperty> Properties { get; } = new Dictionary<string, EDMProperty>();
        public Dictionary<string, Entity> NavigationProperties { get; } = new Dictionary<string, Entity>();
        public string BaseType { get; set; }
    }
}
