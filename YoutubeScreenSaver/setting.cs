using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeScreenSaver
{
    public class UrlEntry
    {
        public UrlEntry(string url, int elapsed, int playlistItemId)
        {
            Url = url;
            Elapsed = elapsed;
            PlaylistItemId = playlistItemId;
        }

        public string Url { get; set; }
        public int Elapsed { get; set; }

        public int PlaylistItemId { get; set; }
    }

    public class Setting
    {
        public bool Shuffle { get; set; }
        public bool Uniform { get; set; }
        public bool Random { get; set; }
        public List<UrlEntry> Entries { get; set; }
    }
}
