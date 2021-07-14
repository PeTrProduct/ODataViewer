using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ODataViewer
{
    public partial class Shell : Form
    {
        #region Fields
        private const string ATOM = "{http://www.w3.org/2005/Atom}";
        private const string META = "{http://schemas.microsoft.com/ado/2007/08/dataservices/metadata}";
        private const string DS = "{http://schemas.microsoft.com/ado/2007/08/dataservices}";
        private TreeNode NodeSelected;
        private MetaData MetaData;
        private string ServicePath;
        private readonly DataServiceConfigForm dscf;
        private readonly WebClient client;
        private readonly HttpClient httpClient;
        #endregion

        public Shell()
        {
            InitializeComponent();
            client = new WebClient()
            {
                UseDefaultCredentials = true
            };
            client.Headers.Add("Content-Type", "application/xml, application/atom+xml, application/json");

            httpClient = new HttpClient();


            EntityTree.AfterSelect += new TreeViewEventHandler(EntityTree_AfterSelect);
            dscf = new DataServiceConfigForm();
            txbQuery.KeyDown += txbQuery_KeyDown;
            intellisenseWPFControl.SelectedItellisense += SelectedItellisense;
            intellisenseWPFControl.ToolTipChanged += intellisenseWPFControl_ToolTipChanged;
            txbQuery.DataBindings.Add(
                propertyName: "Text", 
                dataSource: intellisenseWPFControl, 
                dataMember: "Prefix", 
                formattingEnabled: false, 
                updateMode: DataSourceUpdateMode.OnPropertyChanged);
            WPFHost.DataBindings.Add(
                propertyName: "Visible", 
                dataSource: intellisenseWPFControl, 
                dataMember: "IsVisible", 
                formattingEnabled: false, 
                updateMode: DataSourceUpdateMode.OnPropertyChanged);

            
            HideIntellisense();
        }

        private void intellisenseWPFControl_ToolTipChanged(object sender, IntellisenseEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Intellisense))
            {
                return;
            }

            IntellisenseToolTip.Text = e.Intellisense;
            Point newLoac = new Point(WPFHost.Location.X + WPFHost.Size.Width + 5,
                                       WPFHost.Location.Y + 5);
            IntellisenseToolTip.Location = newLoac;
            try // при закрытии программы с открытым IntellisenseToolTip: Ошибка при создании дескриптора окна
            {
                IntellisenseToolTip.Visible = true;
            }
            catch { }
            IntellisenseTimer.Start();
        }

        private void IntellisenseTimer_Tick(object sender, EventArgs e)
        {
            IntellisenseToolTip.Visible = false;
            IntellisenseTimer.Stop();
        }

        private void SelectedItellisense(object sender, IntellisenseEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Intellisense))
            {
                txbQuery.Text = e.Intellisense;
            }

            txbQuery.Focus();
            txbQuery.SelectionStart = txbQuery.Text.Length;
            HideIntellisense();
        }

        public string FullPath
        {
            get
            {
                if (ServicePath.EndsWith("/"))
                {
                    ServicePath = ServicePath.Substring(0, ServicePath.Length - 1);
                }

                return string.Format("{0}/{1}", ServicePath, txbQuery.Text);
            }
        }

        private void txbQuery_TextChanged(object sender, EventArgs e)
        {
            tslbl.Text = string.Format("{0}/{1}", ServicePath, txbQuery.Text);

            ShowIntellisense();
        }

        private void txbQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter && WPFHost.Visible == false)
            {
                btnGo_Click(this, EventArgs.Empty);
            }
            else if (e.KeyData == Keys.Enter && WPFHost.Visible == true)
            {
                txbQuery.Text = intellisenseWPFControl.GetSelectedIntelliSense();
                txbQuery.SelectionStart = txbQuery.Text.Length;
                HideIntellisense();
            }
            else if (e.KeyData == Keys.Escape)
            {
                HideIntellisense();
            }
            else if (e.KeyData == Keys.Down && WPFHost.Visible == false)
            {
                ShowIntellisense();
            }
            else if (e.KeyData == Keys.Down && WPFHost.Visible == true)
            {
                intellisenseWPFControl.SelectedIndexDown();
            }
            else if (e.KeyData == Keys.Up && WPFHost.Visible == true)
            {
                intellisenseWPFControl.SelectedIndexUp();
            }
        }

        private async Task LoadData()
        {
            QureyWebBrowser.Navigate(FullPath);
            tabControl1.SelectedTab = tabControl1.TabPages[1];

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(new Uri(FullPath));
                string contentType = response.Content.Headers.ContentType.MediaType;
                string content = await response.Content.ReadAsStringAsync();

                DataTable dt = null;
                if (contentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dt = PopulateDataTableFromXML(content);
                }
                else if (contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dt = PopulateDataTableFromJSON(content);
                }

                if (!(dt is null))
                {
                    dataGridView1.DataSource = dt;
                    tabControl1.SelectedTab = tabControl1.TabPages[2];
                }
                else
                {
                    MessageBox.Show("NO DATA FOUND");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DataTable PopulateDataTableFromJSON(string content)
        {
            DataTable dt = new DataTable();

            JsonDocument result = JsonDocument.Parse(content);
            JsonElement root = result.RootElement;

            
            if (root.TryGetProperty("value", out JsonElement value) && value.ValueKind == JsonValueKind.Array)
            {
                JsonElement.ArrayEnumerator values = value.EnumerateArray();
                #region Build DataTable Columns

                foreach ((string, JsonElement) prop in (IEnumerable<(string, JsonElement)>)values.First().DescendantPropertyValues())
                {
                    dt.Columns.Add(prop.Item1);
                }
                #endregion 

                #region Insert Data to DataTable
                foreach (JsonElement obj in values)
                {
                    List<string> colms = new List<string>();

                    IEnumerable<(string, JsonElement)> props = obj.DescendantPropertyValues();
                    foreach ((string, JsonElement) prop in props)
                    {
                        colms.Add(prop.Item2.ToString());
                        //var value_type = prop.Item2.ValueKind.ToString();
                        //var name = prop.Item1;
                    }
                    dt.LoadDataRow(colms.ToArray(), true);
                }
                #endregion 
            }

            return dt;
        }


        private DataTable PopulateDataTableFromXML(string xmlString)
        {
            XElement xe = XElement.Parse(xmlString);
            return PopulateDataTableFromXML(xe);
        }

        private DataTable PopulateDataTableFromXML(XElement xe)
        {
            DataTable dt = new DataTable();

            IEnumerable<XElement> rows;
            if (xe.Elements(ATOM + "entry").Count() > 0)
            {
                rows = xe.Elements(ATOM + "entry")
                             .Elements(ATOM + "content")
                             .Elements(META + "properties");
            }
            else
            {
                rows = xe.Elements(ATOM + "content")
                             .Elements(META + "properties");
            }

            #region Build DataTable Columns
            if (rows.First() == null)
            {
                return dt;
            }

            foreach (XElement colm in rows.First().Elements())
            {
                dt.Columns.Add(colm.Name.LocalName);
            }

            #endregion

            #region Insert Data to DataTable
            foreach (XElement row in rows)
            {
                List<string> colms = new List<string>();

                foreach (XElement col in row.Elements())
                {
                    colms.Add(col.Value);
                }

                dt.LoadDataRow(colms.ToArray(), true);
            }
            #endregion

            return dt;
        }


        private void MetaData_ReadCompleted(object sender, EventArgs e)
        {
            EntityTree.Nodes.Clear();
            EntityTree.BuildTree(MetaData);
            EntityTree.ExpandAll();

            intellisenseWPFControl.Start(MetaData);
            txbQuery.TextChanged += new System.EventHandler(txbQuery_TextChanged);

            DrawGraph();
        }

        private void HideIntellisense()
        {
            IntellisenseTimer_Tick(this, EventArgs.Empty);
            WPFHost.Visible = false;
        }
        private void ShowIntellisense()
        {
            SetItellisenseLocation();
            WPFHost.Visible = true;
            intellisenseWPFControl.ShowIntelliSense(txbQuery.Text);
        }

        private void SetItellisenseLocation()
        {
            WPFHost.Location = new Point(120 + txbQuery.Text.Length * 10, txbQuery.Location.Y + 25);
        }

        private void OpenDataServiceConfigForm()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 2 && Uri.IsWellFormedUriString(args[1], UriKind.Absolute))
            {
                dscf.ServicePath = ServicePath = args[1];
            }
            else
            {
                dscf.ShowDialog();
                ServicePath = dscf.ServicePath;
            }
            
            if (ServicePath.EndsWith("/$metadata", StringComparison.OrdinalIgnoreCase))
            {
                ServicePath = ServicePath.Replace("/$metadata", "");
            }

            if (ServicePath.EndsWith("/", StringComparison.Ordinal))
            {
                ServicePath = ServicePath.Substring(0, ServicePath.Length - 1);
            }

            Show();

            
            string metadata = string.Format("{0}/$metadata", ServicePath);

            webBrowser.Navigate(metadata);

            MetaData = new MetaData(metadata);
            MetaData.ReadCompleted += new EventHandler(MetaData_ReadCompleted);
        }

        #region Toolbar
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDataServiceConfigForm();
        }
        private void tsBtnOpenWeb_Click(object sender, EventArgs e)
        {
            //Process.Start("Firefox.exe", FullPath.Replace(" ", "%20"));
            Process.Start(FullPath.Replace(" ", "%20"));
        }
        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(FullPath);
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new E4D.About().ShowDialog();
        }
        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            ShowIntellisense();
        }
        #endregion

        private void Shell_Load(object sender, EventArgs e)
        {
            OpenDataServiceConfigForm();
        }

        #region TreeView
        private void TreeViewExpandAll_Click(object sender, EventArgs e)
        {
            EntityTree.SelectedNode.ExpandAll();

        }
        private void TreeViewCollapse_Click(object sender, EventArgs e)
        {
            if (EntityTree.SelectedNode != null)
            {
                EntityTree.SelectedNode.Collapse();
            }
        }

        private void ShowMetadataTree_Click(object sender, EventArgs e)
        {
            if (mainSplitContainer.Panel1Collapsed)
            {
                mainSplitContainer.Panel1Collapsed = false;
                btnShowMetadataTree.Text = "Hide Metadata Tree";
            }
            else
            {
                mainSplitContainer.Panel1Collapsed = true;
                btnShowMetadataTree.Text = "Show Metadata Tree";
            }
        }

        private void EntityTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (NodeSelected != null)
            {
                NodeSelected.BackColor = Color.White;
            }

            e.Node.BackColor = Color.Yellow;
            NodeSelected = e.Node;
            //txbQuery.Text += e.Node.Text;
        }

        #endregion

        private async void btnGo_Click(object sender, EventArgs e)
        {
            HideIntellisense();
            await LoadData();
        }

        private void tab_Changed(object sender, EventArgs e)
        {
            if ((sender as System.Windows.Forms.TabControl).SelectedTab.Name == tabGraph.Name 
                && graph is null)
            {
                DrawGraph();
            }
        }

        private void DrawGraph()
        {
            graph = new Microsoft.Msagl.Drawing.Graph("graph");

            foreach (KeyValuePair<string, EntitySet> kvp in MetaData.Model.EntitySets)
            {
                Microsoft.Msagl.Drawing.Node node = graph.AddNode(kvp.Key);
                //node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            }

            foreach (KeyValuePair<string, Association> kvp in MetaData.Model.AssociationSet)
            {
                if(kvp.Value.EndRoles.Count > 1)
                {
                    Microsoft.Msagl.Drawing.Edge node = graph.AddEdge(kvp.Value.EndRoles[0].Role, kvp.Value.EndRoles[1].Role);
                    node.Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
                }
            }
            
            gViewer.Graph = graph;
        }
    }
}
