using System.Collections.Generic;

namespace ODataViewer
{
    public class Entity : EDMElement
    {
        public Entity()
        {
            Keys = new Dictionary<string, string>();
            Properties = new Dictionary<string, Property>();
            NavigationProperties = new Dictionary<string, Entity>();
        }

        public Dictionary<string, string> Keys { get; set; }
        public Dictionary<string, Property> Properties { get; set; }
        public Dictionary<string, Entity> NavigationProperties { get; set; }
    }
}
