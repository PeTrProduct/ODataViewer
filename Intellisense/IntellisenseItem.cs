using ODataViewer.Properties;
using System.Windows.Media.Imaging;

namespace ODataViewer
{
    public class IntellisenseItem
    {
        private DSType m_Type;

        public IntellisenseItem()
        {
        }
        public IntellisenseItem(string text, DSType type) : this()
        {
            Text = text;
            Type = type;

            SetPicture();
        }
        public IntellisenseItem(string text, DSType type, string tooltip)
            : this(text, type)
        {
            ToolTip = tooltip;
        }

        public string Text { get; set; }
        public string ToolTip { get; set; }
        public DSType Type
        {
            get => m_Type;
            set { m_Type = value; SetPicture(); }
        }
        public EDMElement Element { get; set; }
        public BitmapSource Picture { get; set; }

        private void SetPicture()
        {
            switch (Type)
            {
                case DSType.End:
                    Picture = Resources.End.ToBitmapSource();
                    break;

                case DSType.EntitySet:
                    Picture = Resources.EntitySet.ToBitmapSource();
                    break;

                case DSType.Entity:
                    Picture = Resources.Entity.ToBitmapSource();
                    break;

                case DSType.Operation:
                    Picture = Resources.Operation.ToBitmapSource();
                    break;

                case DSType.Key:
                    Picture = Resources.Key.ToBitmapSource();
                    break;

                case DSType.Property:
                    Picture = Resources.Property.ToBitmapSource();
                    break;

                case DSType.NavigationProperty:
                    Picture = Resources.NavigationProperty.ToBitmapSource();
                    break;

                case DSType.Function:
                    Picture = Resources.Function.ToBitmapSource();
                    break;

                case DSType.Expression:
                    Picture = Resources.Function.ToBitmapSource();
                    break;

                default:
                    break;
            }
        }
    }
}
