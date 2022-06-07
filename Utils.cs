using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpotifyArtworkDownloader
{
    public class Utils
    {
        public static string JoinArtists(List<SimpleArtist> artists)
        {
            if (artists.Count == 0) return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < artists.Count - 1; i++)
            {
                sb.Append(artists[i].Name);
                sb.Append(", ");
            }
            sb.Append(artists.Last().Name);

            return sb.ToString();
        }

        public static string JoinArtists(List<Models.Artist> artists)
        {
            if (artists.Count == 0) return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < artists.Count - 1; i++)
            {
                sb.Append(artists[i].name);
                sb.Append(", ");
            }
            sb.Append(artists.Last().name);

            return sb.ToString();
        }

        public static string EscapeIllegalCharsInFilename(string s)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            s = r.Replace(s, "");
            return s;
        }

        public static Image FindBestImage(IEnumerable<Image> images) =>
            (from image in images
             orderby image.Width * image.Height
             select image).Last();
    }
}
