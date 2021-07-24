using System.Collections.Generic;

namespace ODataViewer
{
    public class EntityContainer : EDMElement
    {
        public Dictionary<string, Entity> Entities { get; set; } = new Dictionary<string, Entity>();
        public Dictionary<string, EntitySet> EntitySets { get; set; } = new Dictionary<string, EntitySet>();
        public Dictionary<string, Association> AssociationSet { get; set; } = new Dictionary<string, Association>();
    }
}
