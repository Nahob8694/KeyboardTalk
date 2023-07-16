using System.ComponentModel;
using Controllers.InterceptKeys;
using Controllers.UserSettings;
using NAudio.Wave;

namespace KeyboardTalk
{
    public partial class Form1 : Form
    {
        private readonly List<Panel> _pages;
        private bool _isEnabled;
        private Keys _selectedKey;
        private Panel _currentPannel;
        private readonly Keys[] _forbiddenKeys = new Keys[4] { Keys.None, Keys.RWin, Keys.LWin, Keys.NumLock };
        private bool IsSettingMode { get => _currentPannel == panel2 && WindowState != FormWindowState.Minimized && Visible == true; }
        private readonly Dictionary<Keys, string> _soundPath;
        private readonly Dictionary<Keys, string> _unsavedSetting;
        private readonly List<Keys> _errorSound;

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedSingle;

            if (!UserSettings.IsExists)
            {
                InitUserSetting();
            }

            _isEnabled = UserSettings.GetValue(EUserSettingsSection.Basic, "isEnable") != false.ToString();
            _selectedKey = Keys.None;
            _currentPannel = panel1;
            _soundPath = new();
            _unsavedSetting = new();
            _errorSound = new();
            _pages = new() { panel1, panel2 };
        }

        private void InitUserSetting()
        {
            UserSettings.Write(EUserSettingsSection.Basic, "isEnable", true.ToString());
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.Return.ToString(), "Assets/Audio/原神啟動.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.LShiftKey.ToString(), "Assets/Audio/可莉噠噠噠.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.RShiftKey.ToString(), "Assets/Audio/可莉啦啦啦.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.Space.ToString(), "Assets/Audio/火力全開.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.Back.ToString(), "Assets/Audio/全都可以炸完.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.Escape.ToString(), "Assets/Audio/看我成功開溜.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.LControlKey.ToString(), "Assets/Audio/蹦蹦炸彈.mp3");
            UserSettings.Write(EUserSettingsSection.Sounds, Keys.RControlKey.ToString(), "Assets/Audio/轟轟火花.mp3");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MaximizeBox = false;
            ChangePage(panel1);
            checkBox1.Checked = _isEnabled;
            notifyIcon1.MouseClick += new MouseEventHandler(notifyIcon1_MouseClick);

            _soundPath.Clear();
            _unsavedSetting.Clear();
            foreach (var kp in UserSettings.GetSection(EUserSettingsSection.Sounds))
            {
                Enum.TryParse(kp.Key, out Keys key);
                _soundPath.Add(key, kp.Value);
                if (!File.Exists(kp.Value))
                {
                    _errorSound.Add(key);
                }
            }
            AttachClickEventToKeyboardButtons(this);
            SetInterceptKeysHook();
        }

        private void SetInterceptKeysHook()
        {
            InterceptKeys.Hook((key) =>
            {
                if (IsSettingMode)
                {
                    OnKeySelected(key);
                }
                else
                {
                    if (!_isEnabled) return;
                    if (!_soundPath.ContainsKey(key)) return;
                    string soundPath = _soundPath[key];
                    if (File.Exists(soundPath))
                    {
                        WaveOutEvent player = new();
                        AudioFileReader reader = new(soundPath);
                        player.Init(reader);
                        player.Play();
                        if (_errorSound.Contains(key))
                        {
                            _errorSound.Remove(key);
                        }
                        player.PlaybackStopped += (obj, args) =>
                        {
                            reader.Dispose();
                            player.Dispose();
                        };
                    }
                    else
                    {
                        if (!_errorSound.Contains(key))
                        {
                            _errorSound.Add(key);
                        }
                    }
                }
            }, (key, error) =>
            {
                if (!_errorSound.Contains(key))
                {
                    _errorSound.Add(key);
                }
            });
        }

        private void RemoveInterceptKeysHook()
        {
            InterceptKeys.Unhook();
        }

        private void AttachClickEventToKeyboardButtons(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is KeyboardButton)
                {
                    KeyboardButton keyboardButton = (KeyboardButton)child;
                    keyboardButton.Pressed += KeyboardButton_Click;
                }
                else if (child.HasChildren)
                {
                    AttachClickEventToKeyboardButtons(child);
                }
            }
        }

        private void OnKeySelected(Keys key)
        {
            if (_forbiddenKeys.Contains(key)) return;
            SelectKeyboardButton(this, key);
            _selectedKey = key;
            ResetKeySelectedText();
        }

        private void ResetKeySelectedText()
        {
            label3.Text = _selectedKey == Keys.None ? string.Empty : _selectedKey.ToString();
            string path = _unsavedSetting.GetValueOrDefault(_selectedKey) ?? _soundPath.GetValueOrDefault(_selectedKey) ?? string.Empty;
            label4.Text = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path);
        }

        private void ChangePage(Panel panel)
        {
            if (!_pages.Contains(panel)) return;
            _currentPannel = panel;
            _pages.ForEach((page) =>
            {
                if (page == panel)
                {
                    page.Show();
                }
                else
                {
                    page.Hide();
                }
            });
        }

        private void KeyboardButton_Click(object? sender, Keys key)
        {
            OnKeySelected(key);
        }

        private void SelectKeyboardButton(Control control, Keys key)
        {
            foreach (Control child in control.Controls)
            {
                if (child is KeyboardButton)
                {
                    KeyboardButton keyboardButton = (KeyboardButton)child;
                    if (keyboardButton.Key == Keys.None)
                    {
                        keyboardButton.BackColor = Color.White;
                    }
                    else if (keyboardButton.Key == key)
                    {
                        keyboardButton.BackColor = Color.Cyan;
                    }
                    else if (_errorSound.Contains(keyboardButton.Key))
                    {
                        keyboardButton.BackColor = Color.Magenta;
                    }
                    else if (_unsavedSetting.ContainsKey(keyboardButton.Key))
                    {
                        keyboardButton.BackColor = Color.Yellow;
                    }
                    else if (_soundPath.ContainsKey(keyboardButton.Key))
                    {
                        keyboardButton.BackColor = Color.GreenYellow;
                    }
                    else
                    {
                        keyboardButton.BackColor = Color.White;
                    }
                }
                else if (child.HasChildren)
                {
                    SelectKeyboardButton(child, key);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            DialogResult result = MessageBox.Show("是否要最小化視窗?", "離開", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Hide();
                notifyIcon1.Visible = true;
                e.Cancel = true;
                return;
            }

            if (_unsavedSetting.Count > 0)
            {
                DialogResult result2 = MessageBox.Show("有尚未儲存的設定，是否要直接離開?", "離開", MessageBoxButtons.YesNo);
                if (result2 == DialogResult.No) 
                {
                    e.Cancel = true;
                };
            }
        }

        private void notifyIcon1_MouseClick(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                _isEnabled = true;
                UserSettings.Write(EUserSettingsSection.Basic, "isEnable", _isEnabled.ToString());
            }
            else
            {
                _isEnabled = false;
                UserSettings.Write(EUserSettingsSection.Basic, "isEnable", _isEnabled.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectKeyboardButton(this, Keys.None);
            ChangePage(panel2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_unsavedSetting.Count > 0)
            {
                DialogResult result = MessageBox.Show("有尚未儲存的設定，是否要直接離開?", "返回", MessageBoxButtons.YesNo);
                if (result == DialogResult.No) return;
            }

            _unsavedSetting.Clear();
            ChangePage(panel1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "音訊檔案|*.wav;*.mp3;*.aiff;*.flac;*.ogg";
            openFileDialog.Title = "選擇音效";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                label4.Text = filePath;
                _unsavedSetting[_selectedKey] = filePath;
                _errorSound.Remove(_selectedKey);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("是否要重製設定?", "重製設定", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                foreach (var kp in _soundPath)
                {
                    _unsavedSetting[kp.Key] = string.Empty;
                }
            }
            SelectKeyboardButton(this, Keys.None);
            ResetKeySelectedText();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (var kp in _unsavedSetting)
            {
                if (string.IsNullOrEmpty(kp.Value))
                {
                    UserSettings.Delete(EUserSettingsSection.Sounds, kp.Key.ToString());
                    _soundPath.Remove(kp.Key);
                }
                else
                {
                    UserSettings.Write(EUserSettingsSection.Sounds, kp.Key.ToString(), kp.Value);
                    _soundPath[kp.Key] = kp.Value;
                }
            }
            _unsavedSetting.Clear();
            SelectKeyboardButton(this, Keys.None);
            ChangePage(panel1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_soundPath.ContainsKey(_selectedKey))
            {
                _unsavedSetting[_selectedKey] = string.Empty;
            }
            else
            {
                _unsavedSetting.Remove(_selectedKey);
            }
            ResetKeySelectedText();
        }
    }
}