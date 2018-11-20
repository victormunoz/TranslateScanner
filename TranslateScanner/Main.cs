using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslateScanner
{
    public partial class Main : Form
    {
        HashSet<string> texts;

        public Main()
        {
            InitializeComponent();
        }

        private void ScanFile(string file)
        {
            var f = new StreamReader(file);

            var algun = false;
            while (!f.EndOfStream)
            {
                var linia = f.ReadLine();
                if (linia.Contains("__(\""))
                {
                    var txt = linia.Substring(linia.IndexOf("__(\"") + "__(\"".Length);
                    txt = txt.Substring(0, txt.IndexOf("\""));
                    texts.Add(txt);
                    algun = true;
                }
                else if (linia.Contains("__('"))
                {
                    var txt = linia.Substring(linia.IndexOf("__('") + "__('".Length);
                    txt = txt.Substring(0, txt.IndexOf("'"));
                    texts.Add(txt);
                    algun = true;
                }
            }

            lblFiles.Text = (int.Parse(lblFiles.Text) + 1).ToString();
            if (algun)
            {
                lblFiles2.Text = (int.Parse(lblFiles2.Text) + 1).ToString();
                lblTexts.Text = texts.Count.ToString();
            }

            f.Close();
        }

        private void Scan(string dir)
        {
            if (!txtExclude.Text.Split(',').Contains(Path.GetFileName(dir)))
            {
                toolStripStatusLabel1.Text = "Scanning: " + dir;
                var files = Directory.GetFiles(dir);
                var dirs = Directory.GetDirectories(dir);
                foreach (var subdir in dirs) Scan(subdir);

                foreach (var file in files)
                {
                    ScanFile(file);
                }

                lblDirs.Text = (int.Parse(lblDirs.Text) + 1).ToString();

                Application.DoEvents();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDirectory.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
            if (txtDirectory.Text.Length > 0 && Directory.Exists(txtDirectory.Text))
            {
                lblDirs.Text = "0";
                lblFiles.Text = "0";
                lblFiles2.Text = "0";
                lblTexts.Text = "0";
                texts = new HashSet<string>();
                Scan(txtDirectory.Text);
                button3.Enabled = true;
                button4.Enabled = true;
            }
        }

        private string Decode(string txt)
        {
            var htmlStr = txt;
            // Take out the extra stars
            var result = Regex.Replace(htmlStr, @"\*\*([^*]*)\*\*", "$1");
            // Unescape \x values
            result = Regex.Replace(htmlStr,
                            @"\\x([a-fA-F0-9]{2})",
                            match => char.ConvertFromUtf32(
                                Int32.Parse(match.Groups[1].Value,
                                System.Globalization.NumberStyles.HexNumber)));
            // Decode html entities
            htmlStr = WebUtility.HtmlDecode(result);
            return htmlStr;
        }

        private string CheckParams(string txt, string strad)
        {
            var trad = strad;
            if (txt.Contains("{{"))
            {
                int ii = 0;
                var lparam = new List<string>();
                while (txt.IndexOf("{{", ii) > -1)
                {
                    ii = txt.IndexOf("{{", ii);
                    var param = txt.Substring(ii + 2);
                    param = param.Substring(0, param.IndexOf("}}"));
                    lparam.Add(param);
                    ii = txt.IndexOf("}}", ii);
                }

                ii = 0;
                var idx = 0;
                while (trad.IndexOf("{{", ii) > -1)
                {
                    ii = trad.IndexOf("{{", ii);
                    var param = trad.Substring(ii + 2);
                    param = param.Substring(0, param.IndexOf("}}"));
                    var trueparam = lparam[idx++];
                    trad = trad.Replace("{{" + param + "}}", "{{" + trueparam + "}}");
                    ii = trad.IndexOf("}}", ii);
                }
            }
            return trad;
        }

        string GetTrad(string txt, string output_language)
        {
            if (chkTranslate.Checked)
            {
                var trad = TranslateText(txt, "en|" + output_language);
                trad = Decode(trad);
                trad = CheckParams(txt, trad);
                return trad;
            }
            return txt;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var filename = "output.json";
            var output_language = txtLang.Text;
            var f = new StreamWriter(filename, false);
            f.WriteLine("{");
            var l = texts.ToList();
            lblTrans.Text = "0";
            for (var i=0;i<l.Count;i++)
            {
                var trad = GetTrad(l[i], output_language);
                lblTrans.Text = (int.Parse(lblTrans.Text) + 1).ToString();
                f.WriteLine("\t" + "\"" + l[i].Replace("\"", "\\\"") + "\": \"" + trad.Replace("\"", "\\\"") + "\"" + (i<l.Count-1 ? "," : ""));
                Application.DoEvents();
            }
            f.WriteLine("}");
            f.Close();

            Process.Start("notepad++.exe", filename);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var filename = "output.po";
            var output_language = txtLang.Text;
            var f = new StreamWriter(filename, false);
            var l = texts.ToList();
            lblTrans.Text = "0";
            for (var i = 0; i < l.Count; i++)
            {
                var trad = GetTrad(l[i], output_language);
                lblTrans.Text = (int.Parse(lblTrans.Text) + 1).ToString();
                f.WriteLine("msgid \"" + l[i].Replace("\"", "\\\"") + "\"");
                f.WriteLine("msgstr \"" + trad.Replace("\"", "\\\"") + "\"");
                f.WriteLine();
                Application.DoEvents();
            }
            f.Close();

            Process.Start("notepad++.exe", filename);
        }

        public string TranslateText(string input, string languagePair)
        {
            string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.Default;
            string result = webClient.DownloadString(url);
            result = result.Substring(result.IndexOf("TRANSLATED_TEXT"));
            result = result.Substring(result.IndexOf("'")+1);
            result = result.Substring(0, result.IndexOf("'"));

            return result;
        }
    }
}
