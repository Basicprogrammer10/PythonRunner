using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PythonRunner
{
    public partial class Form1 : Form
    {
        private IniFile Settings = new IniFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\connorcode\PythonRunner\settings.ini");
        private string[] formats = { "py", "txt", "nose", "ini", "md", "json", "csv", "gitignore" };
        private string path = @"";
        private string TempPath = "PyRunner.tmp";
        private string TmpPath;
        private string ExePath;
        private string PyPath;
        private string working;
        private Regex rg;

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox2.Text = Settings.Read("pythonEXE");
            ExePath = Settings.Read("pythonEXE");
            path = Settings.Read("folderPath");
            textBox3.Text = Settings.Read("folderPath");
            try {checkBox1.Checked = Boolean.Parse(Settings.Read("doDebug"));} catch { checkBox1.Checked = false; }
            if (path != "")
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (directoryInfo.Exists)
                {
                    treeView1.AfterSelect += treeView1_AfterSelect;
                    BuildTree(directoryInfo, treeView1.Nodes);
                }
                foreach (string format in formats)
                {
                    working += "(" + format + ")|";
                }
                rg = new Regex(@"\." + working.Remove(working.Length - 1));
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private class IniFile
        {
            private string Path;
            private string EXE = Assembly.GetExecutingAssembly().GetName().Name;

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

            public IniFile(string IniPath = null)
            {
                Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
            }

            public string Read(string Key, string Section = null)
            {
                var RetVal = new StringBuilder(255);
                GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
                return RetVal.ToString();
            }

            public void Write(string Key, string Value, string Section = null)
            {
                WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
            }

            public void DeleteKey(string Key, string Section = null)
            {
                Write(Key, null, Section ?? EXE);
            }

            public void DeleteSection(string Section = null)
            {
                Write(null, null, Section ?? EXE);
            }

            public bool KeyExists(string Key, string Section = null)
            {
                return Read(Key, Section).Length > 0;
            }
        }

        private void BuildTree(DirectoryInfo directoryInfo, TreeNodeCollection addInMe)
        {
            try
            {
                TreeNode curNode = addInMe.Add(directoryInfo.Name);
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    curNode.Nodes.Add(file.FullName, file.Name);
                }
                foreach (DirectoryInfo subdir in directoryInfo.GetDirectories())
                {
                    BuildTree(subdir, curNode.Nodes);
                }
            }
            catch { }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.richTextBox1.Clear();
            if (rg.Match(e.Node.Name.ToLower()).Success)
            {
                PyPath = e.Node.Name;
                StreamReader reader = new StreamReader(e.Node.Name);
                this.richTextBox1.Text = reader.ReadToEnd();
                reader.Close();
            }
        }

        private string CreateTempRun(string BaseFile)
        {
            StreamReader reader = new StreamReader(BaseFile);
            StreamWriter writer = new StreamWriter(path + "\\" + TempPath);
            writer.Write("#Sigma76's Py Runner\n" + reader.ReadToEnd() + "\ninput(\"[ PRESS RETURN TO EXIT ]\")");
            writer.Close();
            return path + "\\" + TempPath;
        }

        private void run_cmd(string cmd, string args, string ExePath)
        {
            string cmdArgs;
            if (checkBox1.Checked)
            {
                cmdArgs = "/k ";
            }
            else
            {
                cmdArgs = "/c ";
            }
            string strCmdText = cmdArgs + ExePath + " " + cmd + " " + args;
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ExePath != null)
            {
                string responce = CreateTempRun(PyPath);
                TmpPath = responce;
                run_cmd(responce, textBox1.Text, ExePath);
            }
            else
            {
                MessageBox.Show("No Python Executable!!! " + ExePath, "PyRunner", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Pick Python Executable",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "exe",
                Filter = "Python Executable (*.exe)|*.exe",
                FilterIndex = 2,
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
                ExePath = openFileDialog1.FileName;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(TmpPath))
            {
                File.Delete(TmpPath);
            }
            foreach (string path in new string[] { @"\connorcode\", @"\connorcode\PythonRunner\" })
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + path))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + path);
                    Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + path);
                }
            }
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\connorcode\PythonRunner\settings.ini"))
            {
                using (File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\connorcode\PythonRunner\settings.ini")) ;
            }
            try
            {
                Settings.Write("pythonEXE", ExePath);
                Settings.Write("folderPath", path);
                Settings.Write("doDebug", checkBox1.Checked.ToString());
            }
            catch (Exception b) { Console.WriteLine(b); }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                path = fbd.SelectedPath;
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox3.Text = fbd.SelectedPath;

                    DirectoryInfo directoryInfo = new DirectoryInfo(fbd.SelectedPath);
                    if (directoryInfo.Exists)
                    {
                        treeView1.AfterSelect += treeView1_AfterSelect;
                        BuildTree(directoryInfo, treeView1.Nodes);
                    }
                    foreach (string format in formats)
                    {
                        working += "(" + format + ")|";
                    }
                    rg = new Regex(@"\." + working.Remove(working.Length - 1));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var form2 = new Form2();
            form2.Show();
        }
    }
}