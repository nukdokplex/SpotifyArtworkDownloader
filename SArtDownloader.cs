using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.ComponentModel;
using System.Net;
using Id3;

namespace SpotifyArtworkDownloader
{
    public class SArtDownloader
    {
        private static EmbedIOAuthServer _server;

        public SpotifyClient spotify;

        TaskCompletionSource<bool> auth;

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Spotify API: Artwork downloader");
            Console.WriteLine("===============================\n");

            try
            {
                new SArtDownloader().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            Console.WriteLine("Initialization of authorization...\n");

            if (await Auth())
            {
                Console.WriteLine("\nAuthorization not successful! Aborting.");
            }

            Console.WriteLine("\nAuthorization successful!\n");

            var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers());

            Console.WriteLine("List of your playlists: ");

            for (int i = 0; i < playlists.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {playlists[i].Name}");
            }

            SimplePlaylist selectedPlaylist;

            while (true)
            {
                Console.Write("Choose your playlist: ");
                string enteredString = Console.ReadLine();

                int i = -1;

                if (Int32.TryParse(enteredString, out i) && i > 0 && i <= playlists.Count + 1)
                {

                    selectedPlaylist = playlists[i - 1];
                    break;
                }

                if (i == -1)
                {
                    Console.WriteLine("Please, enter valid number!");
                }
            }

            Console.WriteLine("Good choice! So, let me see...\n\n");

            var tracks = (await spotify.Playlists.Get(selectedPlaylist.Id)).Tracks.Items;

            var tracksJson = new List<Models.Track>();

            foreach (var playable in tracks)
            {
                if (playable.Track is FullTrack track)
                {

                    var trackJson = Models.Track.FromSpotifyFullTrack(track);
                    tracksJson.Add(trackJson);
                }
                //We don't support episodes for now
            }

            var albums = tracksJson
                .GroupBy(track => track.album.imageUrl)
                .Select(track => track.First().album)
                .ToList();

            var playlistArt = selectedPlaylist.Images.Count > 0 ? Utils.FindBestImage(selectedPlaylist.Images).Url : "";

            Console.WriteLine($"Found {albums.Count() + (string.IsNullOrEmpty(playlistArt) ? 0 : 1)} unique art(s). ");
            Console.Write("Enter output directory path: ");
            string selectedPath = Console.ReadLine();

            Directory.Delete(selectedPath, true);
            Directory.CreateDirectory(selectedPath);

            var tempDir = Path.Combine(Path.GetTempPath(), "/SpotifyArtDownloader/"+selectedPlaylist.Id+"/");
            
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);
            

            using (FileStream fs = new FileStream(Path.Combine(selectedPath, "trackinfo.json"), FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync<List<Models.Track>>(fs, tracksJson);
            }

            foreach (var album in albums)
            {
                string file = Path.Combine(selectedPath, $"{Utils.EscapeIllegalCharsInFilename(Utils.JoinArtists(album.artists)) } - { Utils.EscapeIllegalCharsInFilename(album.title) }.jpg");
                string url = album.imageUrl;
                DownloadFile(file, url);
                File.Copy(
                    sourceFileName: file,
                    destFileName: Path.Combine(tempDir, album.id+".jpg"));
            }

            string playlistArtFile = Path.Combine(selectedPath, $"{Utils.EscapeIllegalCharsInFilename(selectedPlaylist.Owner.DisplayName) } - { Utils.EscapeIllegalCharsInFilename(selectedPlaylist.Name) }.jpg");
            DownloadFile(playlistArtFile, playlistArt);

            int currentTrack = 1;

            List<String> tracksTxt = new List<String>();
            foreach (var track in tracksJson)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(currentTrack.ToString().PadLeft(tracksJson.Count().ToString().Count(), '0') + ". ");
                sb.Append(Utils.JoinArtists(track.artists));
                sb.Append(" - ");
                sb.Append(track.title);
                sb.Append(" (");
                sb.Append(track.duration / 60000);
                sb.Append(':');
                sb.Append(track.duration / 1000 - (track.duration / 60000 * 60));
                sb.Append(')');
                tracksTxt.Add(sb.ToString());
                currentTrack++;
            }

            File.WriteAllLines(Path.Combine(selectedPath, "tracklist.txt"), tracksTxt);

            /*Console.Write("All download tasks completed!\nMaybe you want to update the ID3 tags of your mp3 files? (Y/n): ");
            var key = Console.ReadKey();

            if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Y)
            {
                Console.WriteLine("Okay, have a nice day!");
                return;
            }

            Console.WriteLine("Enter the path to the folder containing your mp3 files.\nJust be aware that the tool sorts them by file name, so name each file so \nthat when sorted they are in the same order as in your Spotify playlist.");
            var mp3dir = "";
            while (true) 
            {
                Console.Write("Path to your directory: ");
                mp3dir = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(mp3dir))
                {
                    continue;
                }
                if (!Directory.Exists(mp3dir))
                {
                    Console.WriteLine("Can't found this folder!");
                    continue;
                }
                var files = Directory.GetFiles(mp3dir);
                if (files.Length == 0)
                {
                    Console.WriteLine("This folder is empty!");
                    continue;
                }
                bool hasMp3 = false;
                foreach (var file in files)
                {
                    if (Path.HasExtension(file) && Path.GetExtension(file).ToLower() == ".mp3")
                    {
                        hasMp3 = true;
                        break;
                    }
                }
                if (!hasMp3)
                {
                    Console.WriteLine("There is no mp3 files!");
                    continue;
                }
                break;
            }

            Console.WriteLine("Working...");

            var mp3files = new List<string>(Directory.GetFiles(mp3dir));
            mp3files.Sort();

            int currentPlaylistTrack = 0;

            foreach (var file in mp3files)
            {
                if (!File.Exists(file)) continue;
                if (!Path.HasExtension(file) || Path.GetExtension(file).ToLower() != ".mp3") continue;
                using (var mp3 = new Mp3(file))
                {
                    Id3Tag tag = mp3.GetTag(Id3TagFamily.Version2X);

                    tag.Title       = tracksJson[currentPlaylistTrack].title;

                    tag.Artists.Value.Clear();
                    var artists = (from artist in tracksJson[currentPlaylistTrack].artists
                                   select artist.name).ToList();
                    foreach (var artist in artists)
                        tag.Artists.Value.Add(artist);

                    tag.Album.Value = tracksJson[currentPlaylistTrack].album.title;

                    tag.Year = Int32.Parse(tracksJson[currentPlaylistTrack].release_date.Substring(0, 4));
                    
                    tag.Track = tracksJson[currentPlaylistTrack].number;
                    
                    tag.Pictures.Clear();
                    Id3.Frames.PictureFrame pictureFrame = new Id3.Frames.PictureFrame();
                    pictureFrame.PictureType = Id3.Frames.PictureType.FrontCover;
                    byte[] cover = File.ReadAllBytes(Path.Combine(tempDir, tracksJson[currentPlaylistTrack].album.id + ".jpg"));
                    pictureFrame.PictureData = cover;
                    tag.Pictures.Add(pictureFrame);

                    mp3.WriteTag(tag);
                }
                currentPlaylistTrack++;
            }*/
            
        }



        private static async Task DownloadFile(string file, string url)
        {
            var webClient = new WebClient();
            try
            {
                await webClient.DownloadFileTaskAsync(new Uri(url), file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't get this url: {url}. Error: {ex.Message}");
            }
        }

        private void OnFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine(e.Error.Message);
            }
        }

        private async Task<bool> Auth()
        {
            auth = new TaskCompletionSource<bool>();

            _server = new EmbedIOAuthServer(new Uri("http://localhost:33727/callback"), 33727);

            await _server.Start();

            _server.ImplictGrantReceived += OnImplicitGrantReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, "22fb770b881a4e60b18e745530f4cc88", LoginRequest.ResponseType.Token)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.UserReadPrivate }
            };

            BrowserUtil.Open(request.ToUri());
            return await auth.Task; 
        }

        private async Task OnImplicitGrantReceived(object sender, ImplictGrantResponse response)
        {
            await _server.Stop();
            spotify = new SpotifyClient(response.AccessToken);

            auth?.TrySetResult(true);
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();

            auth?.TrySetResult(false);
        }
    }

    
}
