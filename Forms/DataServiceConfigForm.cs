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

        public string UserName
        {
            get => txtUserName.Text;
            set => txtUserName.Text = value;
        }

        public string UserPassword
        {
            get => txtUserPassword.Text;
            set => txtUserPassword.Text = value;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
