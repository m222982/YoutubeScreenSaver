using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YoutubeScreenSaver
{
    public partial class FormSetting : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        string filePath = "d://youtubescreensaver.json";

        List<FormPlayer> formPlayers = new List<FormPlayer>();

        Setting _setting;

        bool _showSetup = false;

        public FormSetting(bool showSetup = false)
        {
            InitializeComponent();
            SetCueBanner(txtUrl1, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl2, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl3, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl4, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl5, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl6, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl7, "https://www.youtube.com/watch?v=xxx&list=xxx");
            SetCueBanner(txtUrl8, "https://www.youtube.com/watch?v=xxx&list=xxx");
            _showSetup = showSetup;
        }

        private void SetCueBanner(TextBox textbox, string text)
        {
            SendMessage(textbox.Handle, EM_SETCUEBANNER, 0, text);
        }

        private void FormPlayer_Load(object sender, EventArgs e)
        {
            if (!loadSetting())
                return;

            if (_showSetup)
                return;

            Screen[] screens = Screen.AllScreens;

            int index = 0;
            List<int> randList = getRandomList(_setting.Entries.Count, screens.Count());

            foreach (Screen screen in screens)
            {
                FormPlayer play;
                if (_setting.Uniform)
                {
                    if (_setting.Random)
                    {
                        play = new FormPlayer(_setting, randList[0]);
                    }
                    else
                    {
                        play = new FormPlayer(_setting, 0);
                    }

                }
                else if (_setting.Random)
                {
                    play = new FormPlayer(_setting, randList[index]);
                }
                else
                {
                    play = new FormPlayer(_setting, index);
                }
                index++;
                play.StartPosition = FormStartPosition.Manual;
                play.Location = screen?.WorkingArea.Location ?? new Point(0, 0);
                play.Show();

                formPlayers.Add(play);
                play.CloseRequest += (s, arg) =>
                {
                    saveSettingToClose();
                };
            }
        }

        public void saveSettingToClose()
        {
            if (_setting.Uniform)
            {
                for (int i = 0; i<_setting.Entries.Count; i++)
                {
                    if (_setting.Entries[i].Url == formPlayers[0].YoutubeUrl)
                    {
                        _setting.Entries[i].Elapsed = (int)(double.Parse(formPlayers[0].CurrentPlayElapse));
                        _setting.Entries[i].PlaylistItemId = formPlayers[0].ItemIdInPlaylist;
                    }
                }
            }


            try
            {
                string json = JsonConvert.SerializeObject(_setting, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("儲存 JSON 失敗: " + ex.Message);
            }

            foreach (FormPlayer player in formPlayers)
            {
                player.Close();
            }
            this.Close();
        }
        private List<int> getRandomList(int total, int count)
        {
            List<int> numbers = new List<int>();
            for (int i = 0; i < total; i++)
                numbers.Add(i);

            Random rng = new Random();
            for (int i = numbers.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (numbers[i], numbers[swapIndex]) = (numbers[swapIndex], numbers[i]);
            }

            List<int> result = numbers.GetRange(0, count);

            Console.WriteLine("取出的不重複數字：");
            foreach (var num in result)
                Console.WriteLine(num);
            return result;
        }

        private bool loadSetting()
        {
            Setting setting = loadJsonSetting(filePath);

            if (setting == null)
                return false;
            chkRandom.Checked = setting.Random;
            chkShuffle.Checked = setting.Shuffle;
            chkUniform.Checked = setting.Uniform;
            if (setting.Entries.Count == 0)
                return false;
            int count = 0;
            if (count < setting.Entries.Count)
            {
                txtUrl1.Text = setting.Entries[0].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl2.Text = setting.Entries[1].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl3.Text = setting.Entries[2].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl4.Text = setting.Entries[3].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl5.Text = setting.Entries[4].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl6.Text = setting.Entries[5].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl7.Text = setting.Entries[6].Url;
                count++;
            }
            if (count < setting.Entries.Count)
            {
                txtUrl8.Text = setting.Entries[7].Url;
                count++;
            }

            _setting = setting;
            return true;
        }


        private static Setting loadJsonSetting(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("找不到設定檔: " + path);
                    return null;
                }

                string json = File.ReadAllText(path);
                Setting config = JsonConvert.DeserializeObject<Setting>(json);
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine("載入設定失敗: " + ex.Message);
                return null;
            }
        }

        void SaveSettingToFile(string path)
        {
            List<UrlEntry> list = new List<UrlEntry>();
            if (!string.IsNullOrEmpty(txtUrl1.Text)) list.Add(new UrlEntry(txtUrl1.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl2.Text)) list.Add(new UrlEntry(txtUrl2.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl3.Text)) list.Add(new UrlEntry(txtUrl3.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl4.Text)) list.Add(new UrlEntry(txtUrl4.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl5.Text)) list.Add(new UrlEntry(txtUrl5.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl6.Text)) list.Add(new UrlEntry(txtUrl6.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl7.Text)) list.Add(new UrlEntry(txtUrl7.Text, 0, 0));
            if (!string.IsNullOrEmpty(txtUrl8.Text)) list.Add(new UrlEntry(txtUrl8.Text, 0, 0));

            Setting set = new Setting()
            {
                Entries = list,
                Random = chkRandom.Checked,
                Shuffle = chkShuffle.Checked,
                Uniform = chkUniform.Checked
            };

            try
            {
                string json = JsonConvert.SerializeObject(set, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("儲存 JSON 失敗: " + ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SaveSettingToFile(filePath);
            //this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCheck1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl1.Text))
                return;
            checkUrl(txtUrl1.Text);
        }

        private void btnCheck2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl2.Text))
                return;
            checkUrl(txtUrl2.Text);
        }

        private void btnCheck3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl3.Text))
                return;

            checkUrl(txtUrl3.Text);
        }

        private void btnCheck4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl4.Text))
                return;
            checkUrl(txtUrl4.Text);
        }

        private void btnCheck5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl5.Text))
                return;
            checkUrl(txtUrl5.Text);
        }

        private void btnCheck6_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl6.Text))
                return;
            checkUrl(txtUrl6.Text);
        }

        private void btnCheck7_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl7.Text))
                return;
            checkUrl(txtUrl7.Text);
        }

        private void btnCheck8_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl8.Text))
                return;
            checkUrl(txtUrl8.Text);
        }
        private void checkUrl(string url)
        {
            FormPlayer play = new FormPlayer(url);
            play.WindowState = FormWindowState.Normal;
            play.FormBorderStyle = FormBorderStyle.Sizable;
            play.StartPosition = FormStartPosition.CenterParent;
            play.PreviewMode = true;
            play.ShowDialog();
        }

        private void FormSetting_Shown(object sender, EventArgs e)
        {
            if (!_showSetup)
            {
                this.Hide();
            }
                
        }
    }
}
