using System.Collections.Generic;

namespace ODataViewer
{
    public class Association : EDMElement
    {
        public List<EndRole> EndRoles { get; set; }

        public Association()
        {
            EndRoles = new List<EndRole>();
        }
    }
}
