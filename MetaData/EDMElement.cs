namespace ODataViewer
{
    public class EDMElement
    {
        public string Name { get; set; }
        public string NameType { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Type: {NameType}";
        }
    }
}
