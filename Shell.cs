using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
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
                if (ServicePath.EndsWith("/", StringComparison.Ordinal))
                {
                    ServicePath = ServicePath.Substring(0, ServicePath.Length - 1);
                }

                return $"{ServicePath}/{txbQuery.Text}";
            }
        }

        private void txbQuery_TextChanged(object sender, EventArgs e)
        {
            tslbl.Text = $"{ServicePath}/{txbQuery.Text}";

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
            //QureyWebBrowser.Navigate(FullPath);
            tabControl1.SelectedTab = tabControl1.TabPages[1];

            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(new Uri(FullPath)))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        MessageBox.Show(response.StatusCode.ToString());
                        return;
                    }
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
                    else if (contentType.IndexOf("plain", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dt = PopulateDataTableFromText(content);
                    }

                    if (!(dt is null))
                    {
                        dataGridView1.DataSource = dt;
                        tabControl1.SelectedTab = tabControl1.TabPages[3];
                    }
                    else
                    {
                        MessageBox.Show("NO DATA FOUND");
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DataTable PopulateDataTableFromText(string content)
        {
            DataTable dt = new DataTable
            {
                Locale = CultureInfo.InvariantCulture
            };
            string column_name = "Plain Text";
            dt.Columns.Add(column_name);
            DataRow workRow = dt.NewRow();
            workRow[column_name] = content;
            dt.Rows.Add(workRow);
            return dt;
        }

        private DataTable PopulateDataTableFromJSON(string content)
        {
            DataTable dt = new DataTable
            {
                Locale = CultureInfo.InvariantCulture
            };

            JsonDocument result = JsonDocument.Parse(content);
            JsonElement root = result.RootElement;


            if (root.TryGetProperty("value", out JsonElement value))
            {
                if (value.ValueKind == JsonValueKind.Array) // collection of complex values
                {
                    ExtractJsonValuesDataTable(dt, value);
                }
                else if (value.ValueKind == JsonValueKind.String) // primitive value
                {
                    ExtractPrimitiveValueDataTable(dt, value);
                }
            }
            else if (root.TryGetProperty("error", out JsonElement error))
            {
                if (error.TryGetProperty("message", out JsonElement message))
                {
                    MessageBox.Show(message.ToString(), "OData ERROR:");
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                ExtractComplexValueDataTable(dt, root); // complex value
            }

            return dt;
        }

        /// <summary>
        /// Extract complex value
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="value"></param>
        private void ExtractComplexValueDataTable(DataTable dt, JsonElement value)
        {
            IEnumerable<(string, JsonElement)> props = value.DescendantPropertyValues();
            foreach ((string, JsonElement) prop in props)
            {
                if (!string.IsNullOrEmpty(prop.Item1) && prop.Item1 != "@odata.context")
                {
                    dt.Columns.Add(prop.Item1);
                }
            }

            DataRow workRow = dt.NewRow();
            foreach ((string, JsonElement) prop in props)
            {
                if (!string.IsNullOrEmpty(prop.Item1) && prop.Item1 != "@odata.context")
                {
                    workRow[prop.Item1] = prop.Item2.ToString();
                }
            }
            dt.Rows.Add(workRow);
        }

        /// <summary>
        /// Extract primitive value
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="value"></param>
        private void ExtractPrimitiveValueDataTable(DataTable dt, JsonElement value)
        {
            string column_name = "String value";
            dt.Columns.Add(column_name);
            DataRow workRow = dt.NewRow();
            workRow[column_name] = value.GetString();
            dt.Rows.Add(workRow);
        }

        /// <summary>
        /// Extract collection of complex values
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="element"></param>
        private static void ExtractJsonValuesDataTable(DataTable dt, JsonElement element)
        {
            JsonElement.ArrayEnumerator values = element.EnumerateArray();
            #region Build DataTable Columns

            HashSet<string> columns = new HashSet<string>();

            foreach (JsonElement value in values)
            {
                IEnumerable<(string, JsonElement)> props = value.DescendantPropertyValues();
                foreach ((string, JsonElement) prop in props)
                {
                    if (!string.IsNullOrEmpty(prop.Item1))
                    {
                        columns.Add(prop.Item1);
                    }
                }
            }

            foreach (string col in columns)
            {
                dt.Columns.Add(col);
            }
            #endregion

            #region Insert Data to DataTable
            foreach (JsonElement obj in values)
            {
                DataRow workRow = dt.NewRow();

                IEnumerable<(string, JsonElement)> props = obj.DescendantPropertyValues();
                foreach ((string, JsonElement) prop in props)
                {
                    //var value_type = prop.Item2.ValueKind.ToString();
                    if (!string.IsNullOrEmpty(prop.Item1))
                    {
                        workRow[prop.Item1] = prop.Item2.ToString();
                    }
                }
                dt.Rows.Add(workRow);
            }
            #endregion
        }

        private DataTable PopulateDataTableFromXML(string xmlString)
        {
            XElement xe = XElement.Parse(xmlString);
            return PopulateDataTableFromXML(xe);
        }

        private DataTable PopulateDataTableFromXML(XElement xe)
        {
            DataTable dt = new DataTable
            {
                Locale = CultureInfo.InvariantCulture
            };

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

            txbQuery.Clear();

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


            string metadata = $"{ServicePath}/$metadata";

            dataGridView1.Columns.Clear();
            
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
            string name = ((TabControl)sender).SelectedTab.Name;
            if (name == tabGraph.Name
                && graph is null)
            {
                DrawGraph();
            }
            else if (name == QureyResulttabPage.Name)
            {
                QureyWebBrowser.Navigate(FullPath);
            }
        }

        private void DrawGraph()
        {
            if (gViewer.Graph != null)
            {
                gViewer.Graph = null;
            }
            graph = new Microsoft.Msagl.Drawing.Graph("graph");

            foreach (KeyValuePair<string, EntitySet> kvp in MetaData.Model.EntitySets)
            {
                _ = graph.AddNode(kvp.Key);
                //Microsoft.Msagl.Drawing.Node node = graph.AddNode(kvp.Key);
                //node.Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            }

            foreach (KeyValuePair<string, Association> kvp in MetaData.Model.AssociationSet)
            {
                if (kvp.Value.EndRoles.Count > 1)
                {
                    Microsoft.Msagl.Drawing.Edge node = graph.AddEdge(kvp.Value.EndRoles[0].Role, kvp.Value.EndRoles[1].Role);
                    node.Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
                }
            }

            gViewer.Graph = graph;
        }
    }
}
