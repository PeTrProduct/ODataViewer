namespace ODataViewer
{
    public class EDMProperty : EDMElement
    {
        public bool Nullable { get; set; }
        public int MaxLength { get; set; }
        public bool Unicode { get; set; }
        public bool FixedLength { get; set; }
    }
}
