using System.Collections.Generic;

namespace ODataViewer
{
    public class Entity : EDMElement
    {
        public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Property> Properties { get; set; } = new Dictionary<string, Property>();
        public Dictionary<string, Entity> NavigationProperties { get; set; } = new Dictionary<string, Entity>();
        public string BaseType { get; set; }
    }
}
