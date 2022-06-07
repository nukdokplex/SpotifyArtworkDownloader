using Downloader;
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
                Console.WriteLine($"{i+1}. {playlists[i].Name}");
            }

            SimplePlaylist selectedPlaylist;

            while (true) 
            {
                Console.Write("Choose your playlist: ");
                string enteredString = Console.ReadLine();

                int i = -1;

                if (Int32.TryParse(enteredString, out i) && i > 0 && i <= playlists.Count + 1)
                {
                    
                    selectedPlaylist = playlists[i-1];
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

            Dictionary<string, string> arts = new Dictionary<string, string>();

            var albums = (from track in tracksJson
                          select track.album).Distinct();

            var playlistArt = selectedPlaylist.Images.Count > 0 ? Utils.FindBestImage(selectedPlaylist.Images).Url : "";

            Console.WriteLine($"Found {albums.Count() + (string.IsNullOrEmpty(playlistArt) ? 0 : 1)} unique art(s). ");
            Console.WriteLine("Press any key to select the output directory where they will be downloaded...");

            Console.ReadKey();

            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "Choose output directory where artworks will be downloaded...";
            var result = folderBrowserDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                Console.WriteLine("Download aborted!");
                return;
            }
            try
            {
                if (!Directory.Exists(folderBrowserDialog.SelectedPath))
                {
                    Directory.CreateDirectory(folderBrowserDialog.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't create output directory! Download aborted.");
                return;
            }

            

            var downloadOpt = new DownloadConfiguration()
            {
                ChunkCount = 3, // file parts to download, default value is 1
                OnTheFlyDownload = true, // caching in-memory or not? default values is true
                ParallelDownload = true // download parts of file as parallel or not. Default value is false
            };

            using (FileStream fs = new FileStream(Path.Combine(folderBrowserDialog.SelectedPath, "trackinfo.json"), FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync<List<Models.Track>>(fs, tracksJson);
            }

            var downloader = new DownloadService();

            downloader.DownloadFileCompleted += OnFileCompleted;

            foreach (var art in arts)
            {
                string file = Path.Combine(folderBrowserDialog.SelectedPath, art.Value + ".jpg");
                string url = art.Key;
                await downloader.DownloadFileTaskAsync(file, url);
            }

            Console.WriteLine("All download tasks completed! Have a nice day!\n");
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
