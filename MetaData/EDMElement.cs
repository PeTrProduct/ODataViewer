namespace ODataViewer
{
    public class EDMElement
    {
        public string Name { get; set; }
        public string NameType { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Type: {1}", Name, NameType);
        }
    }
}
