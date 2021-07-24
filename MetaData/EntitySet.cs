using System.Collections.Generic;

namespace ODataViewer
{
    public class EntitySet : EDMElement
    {
        public EntitySet(string name, Entity entity)
        {
            Name = name;
            NameType = entity.NameType;
            Entity = entity;
        }

        public Entity Entity { get; }

        //public Dictionary<string, Entity> Entities { get; set; } = new Dictionary<string, Entity>();
    }
}
