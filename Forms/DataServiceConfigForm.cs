using System;
using System.Windows.Forms;

namespace ODataViewer
{
    public partial class DataServiceConfigForm : Form
    {
        public DataServiceConfigForm()
        {
            InitializeComponent();
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public string ServicePath
        {
            get => txbServicePath.Text;
            set => txbServicePath.Text = value;
        }
    }
}
