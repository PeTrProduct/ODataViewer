using System.Collections.Generic;

namespace ODataViewer
{
    public class EntitySet : EDMElement
    {
        public Entity Entity { get; }

        public EntitySet()
        {
            Entities = new Dictionary<string, Entity>();
            Entity = new Entity();
        }
        public EntitySet(string name, string entityType) : this()
        {
            Name = name;
            NameType = entityType;//.Split('.').Last();
            Entity.NameType = NameType;
            Entity.Name = NameType;
        }

        public Dictionary<string, Entity> Entities { get; set; }
    }
}
