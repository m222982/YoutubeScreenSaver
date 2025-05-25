using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Policy;

namespace YoutubeScreenSaver
{
    public partial class FormPlayer : Form
    {
        Setting _setting;
        int _urlId;
        string _urlcheck = string.Empty;

        public event EventHandler CloseRequest;

        public FormPlayer(Setting setting, int urlId)
        {
            InitializeComponent();

            _setting = setting;
            _urlId = urlId;
            ItemIdInPlaylist = setting.Entries[urlId].PlaylistItemId;
            YoutubeUrl = setting.Entries[urlId].Url;
        }

        public FormPlayer(string url)
        {
            InitializeComponent();
            _urlcheck = url;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //mouseLocation = MousePosition;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void webView21_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseRequest.Invoke(this, EventArgs.Empty);
            }

            e.Handled = true;   // ✅ 這樣會阻止 KeyPress 執行
            e.SuppressKeyPress = true;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (!PreviewMode)
                Cursor.Hide();

            CenterLabel();

            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            //webView21.CoreWebView2.OpenDevToolsWindow();

            webView21.CoreWebView2.WebMessageReceived += OnWebEvent;
            webView21.CoreWebView2.DOMContentLoaded += (s, arg) =>
            {
                Debug.Print("ready");
            };

            string url;
            int elapse;

            if (string.IsNullOrEmpty(_urlcheck))
            {
                url = _setting.Entries[_urlId].Url;
                elapse = _setting.Entries[_urlId].Elapsed;
            }
            else
            {
                url = _urlcheck;
                elapse = 0;
            }

            //(string url, int elapse) = getPlayUrl();
            (string vid, string plid) = parseYouTubeUrl(url);
            this.VideoId = vid;
            this.PlaylistId = plid;
            this.VideoElapse = elapse;

            if (PlaylistId != null)
                webView21.NavigateToString(playListHtml(PlaylistId));
            else
            {
                webView21.NavigateToString(videoHtml(VideoId, VideoElapse));
            }
     
        }

        int errorCnt = 0;
        private void OnWebEvent(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            string msg = args.WebMessageAsJson;
            //Debug.Print(msg);
            msg = msg.Trim('"');
            if (msg.StartsWith("playlist"))
            {
                Playlist = ParseJsArrayString(msg);

                this.VideoId = Playlist[ItemIdInPlaylist];

                webView21.NavigateToString(videoHtml(VideoId, VideoElapse));
            }
            else if (msg.StartsWith("ready"))
            {
                this.panel1.Hide();
                this.label1.Hide();

            }else if (msg.StartsWith("current"))
            {
                CurrentPlayElapse = msg.Split(' ')[1].Trim();
            }
            else if (msg.StartsWith("PlayState"))
            {
                string state = msg.Split(' ')[1].Trim();
                if (state == "0")
                {
                    Debug.Print("find next song");
                    loadNextVideo();
                }
            }
            else if (msg.StartsWith("error")){

                string state = msg.Split(' ')[1].Trim();
                Debug.Print(state);


                errorCnt++;
                if (errorCnt > 2)
                {
                    this.label1.Text = "Unable to play video\n" + _setting.Entries[_urlId].Url;
                    this.label1.Show();
                    this.panel1.Show();
                }
                else
                {
                    webView21.NavigateToString(videoHtml(VideoId, VideoElapse));
                }
            }
        }

        private static List<string> ParseJsArrayString(string jsArray)
        {
            // 使用 Regex 找出所有 "xxx" 的內容
            var matches = Regex.Matches(jsArray, @"\\""(.*?)\\""");
            var result = new List<string>();

            foreach (Match match in matches)
            {
                result.Add(match.Groups[1].Value);
            }

            return result;
        }

        private void loadNextVideo()
        {
            string id;
            if (Playlist == null)
            {
                //no play list, loop current video
                id = VideoId;
                ItemIdInPlaylist = 0;
            }
            else if (Shuffle)
            {
                Random rand = new Random();
                ItemIdInPlaylist = rand.Next(Playlist.Count);
                id = Playlist[ItemIdInPlaylist];
            }
            else
            {
                int i = Playlist.IndexOf(VideoId);
                if (i == Playlist.Count - 1)
                    i = 0;
                else
                    i = i + 1;

                ItemIdInPlaylist = i;
                id = Playlist[i];
            }
            VideoId = id;
            Debug.Print(id);
            webView21.NavigateToString(videoHtml(VideoId, 0));
        }

        private string VideoId { set; get; }

        private int VideoElapse { set; get; }

        private string PlaylistId  { set; get; }

        private bool Shuffle { set; get; }

        public bool PreviewMode { set; get; }

        public string YoutubeUrl { set; get; }

        public string CurrentPlayElapse { set; get; }

        public int ItemIdInPlaylist { set; get; }

        private List<String> Playlist { set; get; }

        private static (string videoId, string playlistId) parseYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (null, null);

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return (null, null);

            string videoId = null;
            string playlistId = null;

            if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
            {
                var query = HttpUtility.ParseQueryString(uri.Query);

                // youtu.be 短網址
                if (uri.Host.Contains("youtu.be") && uri.AbsolutePath.Length > 1)
                    videoId = uri.AbsolutePath.TrimStart('/');

                // 標準 watch?v=
                if (query["v"] != null)
                    videoId = query["v"];

                // 播放清單
                if (query["list"] != null)
                    playlistId = query["list"];

                // /playlist?list=xxx 無 v
                if (uri.AbsolutePath.Contains("/playlist") && query["list"] != null && videoId == null)
                    playlistId = query["list"];
            }

            return (videoId, playlistId);
        }
        private static string playListHtml(string playlistId)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        html, body {{
            margin: 0;
            padding: 0;
            background-color: black;
            height: 100%;
            overflow: hidden;
        }}
        #player {{
            position: absolute;
            top: 0;
            left: 0;
            width: 100vw;
            height: 100vh;
        }}
    </style>
    <script>
        var player;
        function onYouTubeIframeAPIReady() {{
            player = new YT.Player('player', {{
                height: '100%',
                width: '100%',
                playerVars: {{
                  autoplay: 0,
                  controls: 1
                }},
                events: {{
                  onReady: onPlayerReady,
                  onStateChange: onPlayerStateChange
                }}
            }});
        }}
        function onPlayerReady(event) {{
          event.target.cuePlaylist({{
            listType: 'playlist',
            list: '{playlistId}'
          }});
        }}


    function onPlayerStateChange(event) {{
     
        if (event.data == YT.PlayerState.CUED) {{
            var playlist = event.target.getPlaylist();

            window.chrome.webview.postMessage('playlist:' + JSON.stringify(playlist));
            console.log(playlist);
        }}
    }}

        var tag = document.createElement('script');
        tag.src = 'https://www.youtube.com/iframe_api';
        document.head.appendChild(tag);
    </script>
</head>
<body>
    <div id='player'></div>
</body>
</html>";
        }

        private static string videoHtml(string videoId, int startTime)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                html, body {{
                    margin: 0;
                    padding: 0;
                    background-color: black;
                    height: 100%;
                    overflow: hidden;
                }}
                #player {{
                    position: absolute;
                    top: 0;
                    left: 0;
                    width: 100vw;
                    height: 100vh;
                    pointer-events: none; /* 防止滑鼠觸發 UI */
                }}
                </style>
                <script>
                var player;
                function onYouTubeIframeAPIReady() {{
                    player = new YT.Player('player', {{
                        videoId: '{videoId}',
                        playerVars: {{
                            playlist: '{videoId}',
                            start: {startTime},
						    autoplay: 1,
                            mute: 1,
                            controls: 1,
                            modestbranding: 1,
                            rel: 0,
                            loop: 0,
                            showinfo: 0,
                            fs: 0,
                            cc_load_policy: 0,
                            iv_load_policy: 3,
                            enablejsapi: 1,
                            disablekb: 1,
                        }},
                      events: {{
                        onStateChange: onPlayerStateChange,
                        onReady: function (event) {{
                          window.chrome.webview.postMessage(""ready"");
                          setInterval(function () {{
                            if (player && player.getCurrentTime) {{
                              var time = player.getCurrentTime();
                              console.log(""Time:"", time);
                              window.chrome.webview.postMessage(""currentTime "" + time);
                            }}
                          }}, 1000); // 每 1 秒
                        }},
                        onError: function (event) {{
                              console.log(""error:"", event.data); // 打印當前狀態
                              window.chrome.webview.postMessage(""error "" + event.data);
                        }}
                      }}
                    }});
                }}

                // 提供給 C# 主動呼叫的 function
                function reportCurrentTime() {{
                    if (player && player.getCurrentTime) {{
                    var time = player.getCurrentTime();
                    chrome.webview.postMessage(time.toFixed(2));
                    }}
                }}

                function onPlayerStateChange(event) {{
                    console.log(""Player State Change:"", event.data); // 打印當前狀態
                    window.chrome.webview.postMessage(""PlayState "" + event.data);
                }}

                // 載入 YouTube API
                var tag = document.createElement('script');
                tag.src = 'https://www.youtube.com/iframe_api';
                document.head.appendChild(tag);
                </script>
            </head>
            <body>
                <div id='player'></div>
            </body>
            </html>";

        }

        private void webView21_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
            }
        }

        private void CenterLabel()
        {
            int centerX = (this.ClientSize.Width - label1.Width) / 2;
            int centerY = (this.ClientSize.Height - label1.Height) / 2;
            label1.Location = new Point(centerX, centerY);
        }

        private void FormPlayer_Resize(object sender, EventArgs e)
        {
            CenterLabel();
        }
    }

}
