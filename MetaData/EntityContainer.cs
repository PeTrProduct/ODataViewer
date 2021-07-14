using System.Collections.Generic;

namespace ODataViewer
{
    public class EntityContainer : EDMElement
    {
        public EntityContainer()
        {
            EntitySets = new Dictionary<string, EntitySet>();
            AssociationSet = new Dictionary<string, Association>();
        }

        public Dictionary<string, EntitySet> EntitySets { get; set; }
        public Dictionary<string, Association> AssociationSet { get; set; }
    }
}
