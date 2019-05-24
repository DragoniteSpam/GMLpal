﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace GMLpal {
    public enum GMSTypes {
        GMS1, GMS2
    }
    public partial class GMLpal : Form {
        private const string extGMS1 = ".gmx";
        private const string extGMS2 = ".yyp";

        private string fileDisplayName = "";
        private string fileName = "";
        private string filePath = "";
        private string tab = "";

        private readonly string dndCode = "603";
        private readonly string[] shaderSep = new string[] { "//######################_==_YOYO_SHADER_MARKER_==_######################@~" };
        private readonly List<Code> codeList = new List<Code>();
        private readonly Control controlsCode, controlsFind, controlsStats;
        private readonly List<Color> chartPalette = new List<Color>();
        private readonly Random rnd = new Random();
        private readonly Font matchFont = new Font("Consolas", 8.25f, FontStyle.Regular);

        private Regex regexObject;
        private RegexOptions regexOptions;
        private int findResults, findItems;
        private string findRegex, replaceRegex;
        private bool changed, askBackup;
        private int statsLines, statsSize, statsItems;
        private SeriesChartType statsChart = SeriesChartType.Pie;

        public GMLpal() {
            InitializeComponent();
            controlsCode = tbxCode;
            controlsFind = panFind;
            controlsStats = panStats;
            Size = new Size(900, 500);
            for (int i = 0; i < 6; i++) clbxFindProject.SetItemChecked(i, true);
            lblFindResult.Text = "";
            changed = false;
            askBackup = true;
            cmbxStatsChart.Text = "Pie";
            ShufflePalette();
        }

        // Actions
        private void Open(GMSTypes type) {
            tab = "";
            container.Panel2.Controls.Clear();
            codeList.Clear();
            lblFindResult.Text = "";
            treeFind.Nodes.Clear();
            changed = false;
            askBackup = true;
            treeProject.Nodes.Clear();
            treeProject.BeginUpdate();

            switch (type) {
                case GMSTypes.GMS1:
                    try {
                        OpenGMS1();
                    } catch (Exception e) {
                        MessageBox.Show("Bad GameMaker: Studio project, apparnetly?\n" + e.ToString());
                    }
                    
                    break;
                case GMSTypes.GMS2:
                    try {
                        OpenGMS2();
                    } catch (Exception e) {
                        MessageBox.Show("Bad Game Maker Studio 2 project, apparnetly?\n" + e.ToString());
                    }
                    break;
            }
            
            treeProject.EndUpdate();
        }

        private void OpenGMS1() {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            TreeNode root = treeProject.Nodes.Add(fileDisplayName);
            AddScripts(doc.SelectSingleNode("assets/scripts"), root.Nodes.Add("Scripts"));
            AddShaders(doc.SelectSingleNode("assets/shaders"), root.Nodes.Add("Shaders"));
            AddTimelines(doc.SelectSingleNode("assets/timelines"), root.Nodes.Add("Timelines"));
            AddObjects(doc.SelectSingleNode("assets/objects"), root.Nodes.Add("Objects"));
            AddRooms(doc.SelectSingleNode("assets/rooms"), root.Nodes.Add("Rooms"));

            int constantN = -1;
            root.Nodes.Add("Constants");
            foreach (XmlNode constant in doc.SelectNodes("assets/constants/constant")) {
                constantN++;
                TreeNode constantNode = root.Nodes[5].Nodes.Add(constant.Attributes[0].Value);
                constantNode.Tag = new Constant(
                    fileName,
                    "Constant " + constant.Attributes[0].Value,
                    constantN,
                    constant.InnerText
                );
                constantNode.ImageIndex = 2;
                constantNode.SelectedImageIndex = 2;
                codeList.Add((Code)constantNode.Tag);
            }
        }

        private void OpenGMS2() {
            GMS2Project project = JsonConvert.DeserializeObject<GMS2Project>(File.ReadAllText(fileName));
            project.Name = Path.GetFileName(fileName);
        }

        public void AddScripts(XmlNode folder, TreeNode node) {
            if (folder == null) return;
            foreach (XmlNode child in folder.ChildNodes) {
                TreeNode newNode = node.Nodes.Add("");
                if (child.Name == "scripts") {
                    newNode.Text = child.Attributes[0].Value;
                    AddScripts(child, newNode);
                } else {
                    string file = filePath + child.InnerText;
                    newNode.Text = Path.GetFileNameWithoutExtension(child.InnerText);
                    newNode.Tag = new Script(file, "Script " + newNode.Text, File.ReadAllText(file));
                    newNode.ImageIndex = 2;
                    newNode.SelectedImageIndex = 2;
                    codeList.Add((Code)newNode.Tag);
                }
            }
        }
        public void AddShaders(XmlNode folder, TreeNode node) {
            if (folder == null) return;
            foreach (XmlNode child in folder.ChildNodes) {
                TreeNode newNode = node.Nodes.Add("");
                if (child.Name == "shaders") {
                    newNode.Text = child.Attributes[0].Value;
                    AddShaders(child, newNode);
                } else {
                    string file = filePath + child.InnerText;
                    string[] split = File.ReadAllText(file).Split(shaderSep, StringSplitOptions.None);
                    string vertexCode = split[0];
                    string fragmentCode = split[1];
                    newNode.Text = Path.GetFileNameWithoutExtension(child.InnerText);

                    TreeNode vertexNode = newNode.Nodes.Add("Vertex shader");
                    vertexNode.Tag = new VertexShader(file, "Vertex shader of " + newNode.Text, vertexCode);
                    vertexNode.ImageIndex = 2;
                    vertexNode.SelectedImageIndex = 2;
                    codeList.Add((Code)vertexNode.Tag);

                    TreeNode fragmentNode = newNode.Nodes.Add("Fragment shader");
                    fragmentNode.Tag = new FragmentShader(file, "Fragment shader of " + newNode.Text, fragmentCode);
                    fragmentNode.ImageIndex = 2;
                    fragmentNode.SelectedImageIndex = 2;
                    codeList.Add((Code)fragmentNode.Tag);
                }
            }
        }
        public void AddTimelines(XmlNode folder, TreeNode node) {
            if (folder == null) return;
            foreach (XmlNode child in folder.ChildNodes) {
                TreeNode newNode = node.Nodes.Add("");
                if (child.Name == "timelines") {
                    newNode.Text = child.Attributes[0].Value;
                    AddTimelines(child, newNode);
                } else {
                    string file = filePath + child.InnerText + ".timeline.gmx";
                    int stepN = -1;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file);
                    newNode.Text = Path.GetFileNameWithoutExtension(child.InnerText);
                    foreach (XmlNode entry in doc.SelectNodes("timeline/entry")) { // Steps
                        stepN++;
                        TreeNode stepNode = newNode.Nodes.Add(" Step " + entry.SelectSingleNode("step").InnerText);
                        int actionN = -1;
                        foreach (XmlNode dndAction in entry.SelectNodes("event/action")) { // D&D actions
                            actionN++;
                            if (dndAction.SelectSingleNode("id").InnerText != dndCode) continue;
                            TreeNode actionNode = stepNode.Nodes.Add("D&D code action");
                            actionNode.Tag = new TimelineAction(
                                file,
                                "D&D code action in Step " + stepN + " of " + newNode.Text,
                                stepN,
                                actionN,
                                dndAction.SelectSingleNode("arguments/argument/string").InnerText
                            );
                            actionNode.ImageIndex = 2;
                            actionNode.SelectedImageIndex = 2;
                            codeList.Add((Code)actionNode.Tag);
                        }
                    }
                }
            }
        }
        public void AddObjects(XmlNode folder, TreeNode node) {
            if (folder == null) return;
            foreach (XmlNode child in folder.ChildNodes) {
                TreeNode newNode = node.Nodes.Add("");
                if (child.Name == "objects") {
                    newNode.Text = child.Attributes[0].Value;
                    AddObjects(child, newNode);
                } else {
                    string file = filePath + child.InnerText + ".object.gmx";
                    int eventN = -1;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file);
                    newNode.Text = Path.GetFileNameWithoutExtension(child.InnerText);
                    foreach (XmlNode events in doc.SelectNodes("object/events/event")) { // Steps
                        eventN++;
                        TreeNode eventNode = newNode.Nodes.Add(EventName(events.Attributes[0].Value) + " event");
                        int actionN = -1;
                        foreach (XmlNode dndAction in events.SelectNodes("action")) { // D&D actions
                            actionN++;
                            if (dndAction.SelectSingleNode("id").InnerText != dndCode) continue;
                            TreeNode actionNode = eventNode.Nodes.Add("D&D code action");
                            actionNode.Tag = new ObjectAction(
                                file,
                                "D&D code action in " + eventNode.Text + " of " + newNode.Text,
                                eventN,
                                actionN,
                                dndAction.SelectSingleNode("arguments/argument/string").InnerText
                            );
                            actionNode.ImageIndex = 2;
                            actionNode.SelectedImageIndex = 2;
                            codeList.Add((Code)actionNode.Tag);
                        }
                    }
                }
            }
        }
        public void AddRooms(XmlNode folder, TreeNode node) {
            if (folder == null) return;
            foreach (XmlNode child in folder.ChildNodes) {
                TreeNode newNode = node.Nodes.Add("");
                if (child.Name == "rooms") {
                    newNode.Text = child.Attributes[0].Value;
                    AddRooms(child, newNode);
                } else {
                    string file = filePath + child.InnerText + ".room.gmx", roomCode;
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file);
                    roomCode = doc.SelectSingleNode("room/code").InnerText;
                    newNode.Text = Path.GetFileNameWithoutExtension(child.InnerText);
                    if (roomCode != "") { // Room creation code
                        TreeNode roomNode = newNode.Nodes.Add("Creation code");
                        roomNode.Tag = new Room(file, "Room creation code of " + newNode.Text, roomCode);
                        roomNode.ImageIndex = 2;
                        roomNode.SelectedImageIndex = 2;
                        codeList.Add((Code)roomNode.Tag);
                    }
                    int instanceN = -1;
                    foreach (XmlNode instance in doc.SelectNodes("room/instances/instance")) { // Instances
                        string instanceCode = instance.Attributes[5].Value;
                        instanceN++;
                        if (instanceCode == "") continue;
                        TreeNode instanceNode = newNode.Nodes.Add("Creation code in instance of " + instance.Attributes[0].Value);
                        instanceNode.Tag = new Instance(
                            file,
                            "Instance creation code in " + instance.Attributes[3].Value + " of " + instance.Attributes[0].Value + " in " + newNode.Text,
                            instanceN,
                            instance.Attributes[3].Value,
                            instanceCode
                        );
                        instanceNode.ImageIndex = 2;
                        instanceNode.SelectedImageIndex = 2;
                        codeList.Add((Code)instanceNode.Tag);
                    }
                }
            }
        }
        private void Save() {
            string backupLocation = Path.GetDirectoryName(Path.GetDirectoryName(fileName)) + @"\" + fileDisplayName + " - backup";
            if (askBackup && MessageBox.Show("Do you want to create a backup of the project before saving?" + Environment.NewLine + "The backup will be stored in " + backupLocation, "GMLpal", MessageBoxButtons.YesNo) == DialogResult.Yes) DirectoryCopy(Path.GetDirectoryName(fileName), backupLocation);
            foreach (Code c in codeList) c.Save();
            changed = false;
            askBackup = false;
        }
        private void Find() {
            regexOptions = RegexOptions.Multiline;
            findResults = 0;
            findItems = 0;

            // Check find regex
            findRegex = tbxFind.Text;
            replaceRegex = tbxReplace.Text;
            if (rbtnFindSimple.Checked) findRegex = Regex.Escape(findRegex);
            if (!cbxFindCase.Checked) regexOptions |= RegexOptions.IgnoreCase;
            if (cbxRegexMultiLine.Checked) regexOptions |= RegexOptions.Multiline;
            if (cbxFindWord.Checked) findRegex = @"\b" + findRegex + @"\b";
            try {
                MatchCollection matches = Regex.Matches("foo", findRegex, regexOptions);
            } catch (Exception e) {
                MessageBox.Show("Invalid RegEx!" + Environment.NewLine + e.Message);
                return;
            }

            // Start searching
            regexObject = new Regex(findRegex, regexOptions);
            treeFind.Nodes.Clear();
            treeFind.BeginUpdate();
            if (rbtnFindSelected.Checked) {
                if (treeProject.SelectedNode == null) {
                    MessageBox.Show("No item is selected for searching.");
                    return;
                }
                FindInNode(treeProject.SelectedNode);
            } else {
                for (int i = 0; i < 6; i++) {
                    if (clbxFindProject.GetItemChecked(i)) FindInNode(treeProject.Nodes[0].Nodes[i]);
                }
            }
            treeFind.EndUpdate();

            lblFindResult.Text = findResults + " results found in " + findItems + " items";
        }
        public void FindInNode(TreeNode node) {
            if (node.Tag != null) {
                Code codeObject = (Code)node.Tag;
                MatchCollection matches = regexObject.Matches(codeObject.code);

                if (matches.Count > 0) {
                    string[] codeLines = codeObject.code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    TreeNode resultRoot = treeFind.Nodes.Add(codeObject.name + ", " + matches.Count + " match");
                    if (matches.Count != 1) resultRoot.Text += "es";
                    findItems++;
                    foreach (Match match in matches) {
                        TreeNode matchNode = resultRoot.Nodes.Add("");
                        int line = LineNumber(codeLines, match.Index);
                        if (cbxReplace.Checked) matchNode.Text = "\"" + match.Value + "\" -> \"" + regexObject.Replace(match.Value, replaceRegex) + "\" at line " + (line + 1) + ": " + regexObject.Replace(codeLines[line], replaceRegex).Trim();
                        else matchNode.Text = "\"" + match.Value + "\" at line " + (line + 1) + ": " + codeLines[line].Trim();
                        matchNode.NodeFont = matchFont;
                        findResults++;
                    }
                    if (cbxReplace.Checked) {
                        codeObject.code = regexObject.Replace(codeObject.code, replaceRegex);
                        codeObject.changed = true;
                        changed = true;
                    }
                }
            }
            foreach (TreeNode n in node.Nodes) FindInNode(n);
        }
        private void UpdateStats() {
            TreeNode node = treeProject.SelectedNode;
            if (node == null) return;
            lblStatsHeader.Text = "Stats (" + node.Text + ")";
            statsLines = 0;
            statsSize = 0;
            statsItems = 0;

            // Set up chart
            Series series = chartStats.Series[0];
            series.ChartType = statsChart;
            series.BackGradientStyle = GradientStyle.TopBottom;
            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = "Black";
            series.Label = "#LABEL";
            series.Points.Clear();

            GetStats(node, true);
            lblStats.Text = "Lines of code: " + statsLines + Environment.NewLine;
            lblStats.Text += "Size: " + SizeToString(statsSize) + Environment.NewLine;
            lblStats.Text += "Items: " + statsItems;
            chartStats.Visible = (series.Points.Count > 0);
        }
        private int GetStats(TreeNode node, bool root) {
            int val = 0;
            if (node.Tag != null) {
                Code codeObject = (Code)node.Tag;
                string[] codeLines = codeObject.code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                statsLines += codeLines.Length;
                statsSize += codeObject.code.Length;
                if (rbtnStatsLines.Checked) val = codeLines.Length;
                else if (rbtnStatsSize.Checked) val = codeObject.code.Length;
            }
            if (rbtnStatsItems.Checked) val = node.GetNodeCount(true);
            foreach (TreeNode child in node.Nodes) {
                int childVal = GetStats(child, false);
                statsItems++;
                val += childVal;
                if (root && childVal > 0) {
                    string suffix = "";
                    if (!cbxStatsNamesOnly.Checked) {
                        if (rbtnStatsLines.Checked) suffix = " (" + childVal + " lines)";
                        else if (rbtnStatsSize.Checked) suffix = " (" + SizeToString(childVal) + ")";
                        else suffix = " (" + childVal + " items)";
                    }
                    ChartAdd(child.Text + suffix, childVal);
                }
            }
            return val;
        }
        private void ShufflePalette() {
            chartPalette.Clear();
            for (int i = 0; i < 32; i++) chartPalette.Add(Color.FromArgb(rnd.Next(127, 255), rnd.Next(127, 255), rnd.Next(127, 255)));
        }
        private void ChartAdd(string text, int val) {
            DataPoint dp = chartStats.Series[0].Points.Add(val);
            dp.Label = text;
            dp.Color = chartPalette[(chartStats.Series[0].Points.Count - 1) % chartPalette.Count];
            dp.BackSecondaryColor = ColorAdd(dp.Color, - 75);
        }
        private Color ColorAdd(Color c, int val) {
            return Color.FromArgb(c.A, Math.Max(0, c.R + val), Math.Max(0, c.G + val), Math.Max(0, c.B + val));
        }
        private string SizeToString(long val) {
            if (val < 1) return "0b";
            string[] suf = { "b", "kB", "MB", "GB", "TB", "PB" };
            int place = Convert.ToInt32(Math.Floor(Math.Log(val, 1024)));
            double num = Math.Round(val / Math.Pow(1024, place), 1);
            return num.ToString() + suf[place];
        }
        private string EventName(string n) {
            switch (n) {
                case "0": return "Create";
                case "1": return "Destroy";
                case "2": return "Alarm";
                case "3": return "Step";
                case "4": return "Collision";
                case "5": return "Key";
                case "6": return "Mouse";
                case "7": return "Other";
                case "8": return "Draw";
                case "9": return "Key Press";
                case "10": return "Key Release";
            }
            return "";
        }
        private void ChangeTab(string t) {
            if ((t == "code" || t == "") && (tab == "find" || tab == "stats")) return;
            container.Panel2.Controls.Clear();
            Size s = new Size(container.Panel2.Width - 4, container.Panel2.Height - 2);
            switch (t) {
                case "code": {
                        container.Panel2.Controls.Add(controlsCode);
                        controlsCode.Size = s;
                        break;
                    }
                case "find": {
                        container.Panel2.Controls.Add(controlsFind);
                        controlsFind.Size = s;
                        break;
                    }
                case "stats": {
                    container.Panel2.Controls.Add(controlsStats);
                    controlsStats.Size = s;
                    UpdateStats();
                    break;
                }
            }
            tab = t;
        }
        private void UpdateSel() {
            if (treeProject.SelectedNode == null) return;
            TreeNode selNode = treeProject.SelectedNode;
            Code codeObject = (Code)selNode.Tag;
            rbtnFindSelected.Text = "Find within selected item (" + selNode.Text + ")";

            if (codeObject != null) {
                ChangeTab("code");
                tbxCode.Text = codeObject.code;
            } else ChangeTab("");

            if (tab == "stats") UpdateStats();
        }
        public int LineNumber(string[] lines, int pos) {
            int c = 0, line = 0;
            while (true) {
                c += lines[line].Length + Environment.NewLine.Length;
                if (c > pos) break;
                line++;
            }
            return line;
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            foreach (DirectoryInfo subdir in dirs) {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }

        // Tools
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e) {
            if (changed && MessageBox.Show("You have unchanged changes. Do you still want to open a new project?", "GMLpal", MessageBoxButtons.YesNo) == DialogResult.No) return;
            DialogResult res = openFileBrowse.ShowDialog();
            if (res != DialogResult.OK) return;
            fileName = openFileBrowse.FileName;
            string fileExtension = Path.GetExtension(fileName);
            filePath = Path.GetDirectoryName(fileName) + @"\";
            fileDisplayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
            Text = fileDisplayName + " - GMLpal";
            
            Open(fileExtension.Equals(extGMS1) ? GMSTypes.GMS1 : GMSTypes.GMS2);
        }
        private void SaveChangesToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fileName == "") {
                MessageBox.Show("No project loaded.");
                return;
            }
            if (!changed) {
                MessageBox.Show("You haven't made any changes!");
                return;
            }
            Save();
        }
        private void ReloadToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fileName == "") {
                MessageBox.Show("No project loaded.");
                return;
            }
            Open(Path.GetExtension(fileName).Equals(extGMS1) ? GMSTypes.GMS1 : GMSTypes.GMS2);
        }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }
        private void FindToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fileName == "") {
                MessageBox.Show("No project loaded.");
                return;
            }
            ChangeTab("find");
        }
        private void ReplaceToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fileName == "") {
                MessageBox.Show("No project loaded.");
                return;
            }
            ChangeTab("replace");
        }
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e) {
            MessageBox.Show("GMLpal 1.0, 2014.06.30 by David \"Davve\" Norgren" + Environment.NewLine + "For GM:Studio v1.3.1354 by YoYoGames");
        }
        private void StatsToolStripMenuItem_Click(object sender, EventArgs e) {
            if (fileName == "") {
                MessageBox.Show("No project loaded.");
                return;
            }
            ChangeTab("stats");
        }

        // Form
        private void TreeProject_AfterSelect(object sender, TreeViewEventArgs e) {
            UpdateSel();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (changed && MessageBox.Show("You have unchanged changes. Do you still want to exit?", "GMLpal", MessageBoxButtons.YesNo) == DialogResult.No) {
                e.Cancel = true;
                this.Activate();
            }
        }
        
        // Find and replace
        private void BtnFindClose_Click(object sender, EventArgs e) {
            tab = "";
            ChangeTab("");
            UpdateSel();
        }
        private void RbtnFindSelected_CheckedChanged(object sender, EventArgs e) {
            clbxFindProject.Enabled = rbtnFindProject.Checked;
        }
        private void rbtnFindProject_CheckedChanged(object sender, EventArgs e) {
            clbxFindProject.Enabled = rbtnFindProject.Checked;
        }
        private void TbxFind_TextChanged(object sender, EventArgs e) {
            btnFind.Enabled = (tbxFind.Text != "");
        }
        private void BtnFind_Click(object sender, EventArgs e) {
            Find();
        }
        private void RbtnFindAdvanced_CheckedChanged(object sender, EventArgs e) {
            lblFind.Text = rbtnFindAdvanced.Checked ? "RegEx:" : "Find:";
            cbxRegexMultiLine.Visible = rbtnFindAdvanced.Checked;
        }
        private void RbtnFindSimple_CheckedChanged(object sender, EventArgs e) {
            lblFind.Text = rbtnFindAdvanced.Checked ? "RegEx:" : "Find:";
            cbxRegexMultiLine.Visible = rbtnFindAdvanced.Checked;
        }
        private void CbxReplace_CheckedChanged(object sender, EventArgs e) {
            tbxReplace.Enabled = cbxReplace.Checked;
            btnFind.Text = cbxReplace.Checked ? "Find and replace" : "Find";
        }

        // Stats
        private void BtnStatsClose_Click(object sender, EventArgs e) {
            tab = "";
            ChangeTab("");
            UpdateSel();
        }
        private void CmbxStatsChart_SelectedIndexChanged(object sender, EventArgs e) {
            switch (cmbxStatsChart.Text) {
                case "Column": statsChart = SeriesChartType.Column; break;
                case "Pie": statsChart = SeriesChartType.Pie; break;
                case "Doughnut": statsChart = SeriesChartType.Doughnut; break;
                case "Pyramid": statsChart = SeriesChartType.Pyramid; break;
            }
            UpdateStats();
        }
        private void BtnStatsShuffleColors_Click(object sender, EventArgs e) {
            ShufflePalette();
            UpdateStats();
        }
        private void RbtnStatsLines_CheckedChanged(object sender, EventArgs e) {
            UpdateStats();
        }
        private void RbtnStatsSize_CheckedChanged(object sender, EventArgs e) {
            UpdateStats();
        }
        private void CbxStatsNamesOnly_CheckedChanged(object sender, EventArgs e) {
            UpdateStats();
        }
    }
}
