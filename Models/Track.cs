using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace SpotifyArtworkDownloader.Models
{
    public class Track
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("album")]
        public Album album { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist> artists { get; set; }

        [JsonPropertyName("release_date")]
        public string release_date { get; set; }

        [JsonPropertyName("number")]
        public int number { get; set; }

        [JsonPropertyName("genre")]
        public string genre { get; set; }

        [JsonPropertyName("duration")]
        public int duration { get; set; }

        [JsonPropertyName("diskNumber")]
        public string diskNumber { get; set; }

        public Track(string id, string title, Album album, List<Artist> artists, string release_date, int number, string genre, int duration, string diskNumber)
        {
            this.id = id;
            this.title = title;
            this.album = album;
            this.artists = artists;
            this.release_date = release_date;
            this.number = number;
            this.genre = genre;
            this.duration = duration;
            this.diskNumber = diskNumber;
        }

        public static Track FromSpotifyFullTrack(FullTrack track)
        {
            var artists = new List<Artist>();

            foreach (var artist in track.Artists) 
                artists.Add(Artist.FromSpotifySimpleArtist(artist));

            return new Track(
                id: track.Id,
                title: track.Name,
                album: Album.FromSpotifySimpleAlbum(track.Album),
                artists: artists,
                release_date: track.Album.ReleaseDate,
                number: track.TrackNumber,
                genre: "", //TODO get genre somehow or delete
                duration: track.DurationMs,
                diskNumber: track.DiscNumber.ToString());
        }

        public override bool Equals(object obj)
        {
            return obj is Track track &&
                   id == track.id &&
                   title == track.title &&
                   EqualityComparer<Album>.Default.Equals(album, track.album) &&
                   EqualityComparer<List<Artist>>.Default.Equals(artists, track.artists) &&
                   release_date == track.release_date &&
                   number == track.number &&
                   genre == track.genre &&
                   diskNumber == track.diskNumber;
        }

        public override int GetHashCode()
        {
            int hashCode = -1264607827;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(title);
            hashCode = hashCode * -1521134295 + EqualityComparer<Album>.Default.GetHashCode(album);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Artist>>.Default.GetHashCode(artists);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(release_date);
            hashCode = hashCode * -1521134295 + number.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(genre);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(diskNumber);
            return hashCode;
        }

        public override string ToString() => $"{Utils.JoinArtists(artists)} - {title}";
    }

    public class Artist
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        public Artist(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public static Artist FromSpotifySimpleArtist(SimpleArtist artist)
        {
            return new Artist(
                id: artist.Id, 
                name: artist.Name);
        }

        public override bool Equals(object obj)
        {
            return obj is Artist artist &&
                   id == artist.id &&
                   name == artist.name;
        }

        public override int GetHashCode()
        {
            int hashCode = -48284730;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            return hashCode;
        }

        public override string ToString() => $"{name}";
    }

    public class Album
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist> artists { get; set; }

        [JsonPropertyName("imageUrl")]
        public string imageUrl { get; set; }

        public Album(string id, string title, IEnumerable<Artist> artists, string imageUrl)
        {
            this.id = id;
            this.title = title;
            this.artists = (List<Artist>)artists;
            this.imageUrl = imageUrl;
        }

        public static Album FromSpotifySimpleAlbum(SimpleAlbum album)
        {
            List<Artist> artists = new List<Artist>();

            foreach (var artist in album.Artists) artists.Add(Artist.FromSpotifySimpleArtist(artist));

            return new Album(
                id: album.Id, 
                title: album.Name, 
                artists: artists, 
                imageUrl: Utils.FindBestImage(album.Images).Url);
        }

        public override bool Equals(object obj)
        {
            return obj is Album album &&
                   id == album.id &&
                   title == album.title &&
                   EqualityComparer<List<Artist>>.Default.Equals(artists, album.artists) &&
                   imageUrl == album.imageUrl;
        }

        public override int GetHashCode()
        {
            int hashCode = 1521671499;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(title);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Artist>>.Default.GetHashCode(artists);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(imageUrl);
            return hashCode;
        }

        public override string ToString() => $"{Utils.JoinArtists(artists)} - {title}";
    }
}
