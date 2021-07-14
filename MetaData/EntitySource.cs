namespace ODataViewer
{
    public class EntitySource : EDMElement
    {
        public DSType EntityType { get; set; }
        public bool IsNull => string.IsNullOrEmpty(NameType);
    }
}
