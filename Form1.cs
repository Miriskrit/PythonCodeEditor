using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PythonCodeEditor
{
    public partial class mainWindow : Form
    {
        private struct ShortcutData
        {
            public string Text { get; set; } = "";
            public int StepBack { get; set; } = 0;
            public ShortcutData() { }
            public ShortcutData(string text, int stepBack)
            {
                Text = text;
                StepBack = stepBack;
            }
            public override string ToString() => $"Text: {Text} StepBack: {StepBack}";
        }

        private Dictionary<string, ShortcutData> shortcuts_dict = new()
        {
            { "p", new ShortcutData("print(f\"{}\")", 3) }
        };

        private static class ErrorMessage
        {
            public static void Show(string text)
            {
                MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private readonly string path = @"__temp.py";

        private static void TryRun(string processName, string args="", string errorMessage="")
        {
            try
            {
                Process.Start(processName, args);
            }
            catch (Exception error)
            {
                ErrorMessage.Show($"{errorMessage}\n\n{error.Message}");
            }
        }

        private void RunPython()
        {
            try
            {
                File.WriteAllText(path, codeEditor.Text);

                TryRun(
                    "python.exe",
                    "-i " + Directory.GetCurrentDirectory() + @"\" + path,
                    "python.exe not found"
                    );
            }
            catch (Exception error)
            {
                ErrorMessage.Show($"Unable to create a file\n\n{error.Message}");
            }
        }

        private void ApplySettings()
        {
            codeEditor.ForeColor = Properties.Settings.Default.FontColor;
            codeEditor.BackColor = Properties.Settings.Default.BackgroundColor;
            codeEditor.Font = Properties.Settings.Default.Font;

            label_example.ForeColor = Properties.Settings.Default.FontColor;
            label_example.BackColor = Properties.Settings.Default.BackgroundColor;
            label_example.Font = Properties.Settings.Default.Font;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.FontColor = label_example.ForeColor;
            Properties.Settings.Default.BackgroundColor = label_example.BackColor;
            Properties.Settings.Default.Font = label_example.Font;
            Properties.Settings.Default.Save();
        }

        private void OffSound(KeyEventArgs e)
        {
            if (
                codeEditor.GetLineFromCharIndex(codeEditor.SelectionStart) == 0 &&
                e.KeyData == Keys.Up ||
                codeEditor.GetLineFromCharIndex(codeEditor.SelectionStart) == codeEditor.GetLineFromCharIndex(codeEditor.TextLength) &&
                e.KeyData == Keys.Down ||
                codeEditor.SelectionStart == codeEditor.TextLength &&
                e.KeyData == Keys.Right ||
                codeEditor.SelectionStart == 0 &&
                e.KeyData == Keys.Left
            ) { e.Handled = true; };
        }

        private System.Windows.Forms.Timer highlightTimer = new System.Windows.Forms.Timer();

        private void HighlightText(object sender, EventArgs e)
        {
            HighlightText();
        }

        private void HighlightText()
        {
            int pos = codeEditor.SelectionStart;
            string[] words = { "for", "in", "and", "while", "or", "def" };

            codeEditor.SelectAll();
            codeEditor.SelectionColor = Color.White;

            foreach (string word in words)
            {
                string pattern = @"(?<!\S)" + word + @"(?!\S)+";
                foreach (Match match in Regex.Matches(codeEditor.Text, pattern, RegexOptions.IgnoreCase))
                {
                    codeEditor.SelectionStart = match.Index;
                    codeEditor.SelectionLength = word.Length;
                    codeEditor.SelectionColor = Color.FromArgb(86, 156, 214);
                }
            }
            codeEditor.DeselectAll();
            codeEditor.SelectionStart = pos;
            codeEditor.ScrollToCaret();
            highlightTimer.Stop();
        }

        public mainWindow()
        {
            highlightTimer.Interval = 3000;
            highlightTimer.Tick += new EventHandler(HighlightText);
            highlightTimer.Start();
            InitializeComponent();
        }

        private void button_run_Click(object sender, EventArgs e)
        {
            RunPython();
        }

        private void button_python_Click(object sender, EventArgs e)
        {
            TryRun("python.exe", errorMessage: "python.exe not found");
        }

        private void button_console_Click(object sender, EventArgs e)
        {
            TryRun("cmd.exe", errorMessage: "cmd.exe not found");
        }

        private void codeEditor_KeyUp(object sender, KeyEventArgs e)
        {
            highlightTimer.Start();
            //HighlightText();
        }

        private void codeEditor_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            highlightTimer.Stop();
            if (e.KeyCode != Keys.Tab)
                return;

            int nLine = codeEditor.GetLineFromCharIndex(codeEditor.SelectionStart);
            string shortcut = nLine == 0? codeEditor.Text.Trim(): codeEditor.Lines[nLine].Trim();

            if (shortcuts_dict.TryGetValue(shortcut, out ShortcutData insertion))
            {
                int pos = codeEditor.SelectionStart;
                codeEditor.Text = codeEditor.Text.Remove(pos - shortcut.Length, shortcut.Length);
                codeEditor.SelectionStart = pos - shortcut.Length;
                codeEditor.SelectedText = insertion.Text;
                codeEditor.SelectionStart -= insertion.StepBack;
            }
            else
            {
                codeEditor.SelectedText = "    ";
            }
            e.IsInputKey = true;
        }

        private void codeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R && e.Modifiers == Keys.Control)
            {
                RunPython();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
            }
            OffSound(e);
        }

        private void codeEditor_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '(':
                    codeEditor.SelectedText = ")";
                    codeEditor.SelectionStart -= 1;
                    break;
                case '"':
                    codeEditor.SelectedText = "\"";
                    codeEditor.SelectionStart -= 1;
                    break;
                case '\'':
                    codeEditor.SelectedText = "\'";
                    codeEditor.SelectionStart -= 1;
                    break;
            }
        }

        private void button_editor_Click(object sender, EventArgs e)
        {
            tab_main.SelectedIndex = 0;
        }

        private void button_shortcuts_Click(object sender, EventArgs e)
        {
            tab_main.SelectedIndex = 1;
        }

        private void button_settings_Click(object sender, EventArgs e)
        {
            tab_main.SelectedIndex = 2;
        }

        private void mainWindow_Load(object sender, EventArgs e)
        {
            ApplySettings();
        }

        private void button_fontColor_Click(object sender, EventArgs e)
        {
            if (colorDialog_font.ShowDialog() == DialogResult.OK)
            {
                label_example.ForeColor = colorDialog_font.Color;
                codeEditor.ForeColor = colorDialog_font.Color;
                SaveSettings();
            }
        }

        private void button_bgColor_Click(object sender, EventArgs e)
        {
            if (colorDialog_bg.ShowDialog() == DialogResult.OK)
            {
                label_example.BackColor = colorDialog_bg.Color;
                codeEditor.BackColor = colorDialog_bg.Color;
                SaveSettings();
            }
        }

        private void button_font_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                label_example.Font = fontDialog.Font;
                codeEditor.Font = fontDialog.Font;
                SaveSettings();
            }
        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            label_example.Font = new Font("Consolas", 11);
            label_example.BackColor = Color.FromArgb(30, 30, 30);
            label_example.ForeColor = Color.FromArgb(255, 255, 255);
            SaveSettings();
            ApplySettings();
        }

        private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(File.Exists(path))
            {
                //File.Delete(path);
            }
        }
    }
}