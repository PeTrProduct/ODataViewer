using System.Collections.Generic;

namespace ODataViewer
{
    public class EntityContainer : EDMElement
    {
        public Dictionary<string, Entity> Entities { get; } = new Dictionary<string, Entity>();
        public Dictionary<string, EntitySet> EntitySets { get; } = new Dictionary<string, EntitySet>();
        public Dictionary<string, Association> AssociationSet { get; } = new Dictionary<string, Association>();
    }
}
