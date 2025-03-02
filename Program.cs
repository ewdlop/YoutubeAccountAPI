using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Globalization;

#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;

#endif
using static Google.Apis.YouTube.v3.ChannelsResource;



//swap targeting os on csjproj to windows in order meet this condition (right clicking and go to property then in the dropdown swap platform)
//Windows Only Functionality
#if WINDOWS
class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        string clientSecretJsonFilePath = ShowOpenFileDialog();
        APIHelper.RunAPIAsync(clientSecretJsonFilePath).GetAwaiter().GetResult();

    }

    static string ShowOpenFileDialog(string? filter = null)
    {
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = filter ??"All files (*.*)|*.*|Text files (*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }
    }
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

    static void ShowMessageBox(string message)
    {
        MessageBox(IntPtr.Zero, message, "Message", 0);
    }
}
#else
{
    Console.WriteLine("Please enter Client Secret Json FilePath: ");
    clientSecretJsonFilePath  = Console.ReadLine();
}
#endif


static class APIHelper
{
    public static async Task RunAPIAsync(string clientSecretJsonFilePath)
    {
        string[] scopes = { YouTubeService.Scope.Youtube };
        // Load or request authorization
        UserCredential credential;
        using (var stream = new FileStream(clientSecretJsonFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("localstore", false));
        }

        //Create the YouTube service
        YouTubeService? youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "YouTubeAPIExample"
        });
        {


            //Console.WriteLine("Please enter the API Key:");
            //string? apiKey = Console.ReadLine();



            //// Create the service
            //YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
            //{
            //    ApiKey = apiKey,
            //    ApplicationName = "YouTubeAPIExample"
            //});


            //try
            //{
            //    // Call the API
            //    ListRequest channelRequest = youtubeService.Channels.List("snippet,contentDetails,statistics");
            //    Console.WriteLine("Enter the username:");
            //    channelRequest.ForUsername = Console.ReadLine();

            //    ChannelListResponse? channelResponse = await channelRequest.ExecuteAsync();

            //    foreach (var channel in channelResponse.Items)
            //    {
            //        Console.WriteLine($"Title: {channel.Snippet.Title}");
            //        Console.WriteLine($"Description: {channel.Snippet.Description}");
            //        Console.WriteLine($"View Count: {channel.Statistics.ViewCount}");
            //        Console.WriteLine($"Subscriber Count: {channel.Statistics.SubscriberCount}");
            //        Console.WriteLine($"Video Count: {channel.Statistics.VideoCount}");
            //    }

            //    Console.WriteLine();
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    Console.WriteLine(ex.InnerException?.Message);
            //}
        }


        {
            // Create the service
            //YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
            //{
            //    ApiKey = apiKey,
            //    ApplicationName = "YouTubeAPIExample"
            //});

            //// Call the API
            //SearchResource.ListRequest searchRequest = youtubeService.Search.List("snippet");
            //searchRequest.Q = "ai"; // Replace with your search query
            //searchRequest.MaxResults = 10;

            //var searchResponse = await searchRequest.ExecuteAsync();

            //foreach (var searchResult in searchResponse.Items)
            //{
            //    if (searchResult.Id.Kind == "youtube#video")
            //    {
            //        Console.WriteLine($"Title: {searchResult.Snippet.Title}");
            //        Console.WriteLine($"Description: {searchResult.Snippet.Description}");
            //        Console.WriteLine($"Video ID: {searchResult.Id.VideoId}");
            //        Console.WriteLine($"Thumbnail: {searchResult.Snippet.Thumbnails.High.Url}");
            //        Console.WriteLine($"Video URL: https://www.youtube.com/watch?v={searchResult.Id.VideoId}");

            //        Console.WriteLine();
            //    }
            //}
        }
        {

            ListRequest channelRequest = youtubeService.Channels.List("snippet,contentDetails,statistics");
            Console.WriteLine("Please Enter the username:");
            channelRequest.ForUsername = Console.ReadLine();

            try
            {
                ChannelListResponse? channelResponse = await channelRequest.ExecuteAsync();

                foreach (var channel in channelResponse.Items)
                {
                    Console.WriteLine($"Title: {channel.Snippet.Title}");
                    Console.WriteLine($"Description: {channel.Snippet.Description}");
                    Console.WriteLine($"View Count: {channel.Statistics.ViewCount}");
                    Console.WriteLine($"Subscriber Count: {channel.Statistics.SubscriberCount}");
                    Console.WriteLine($"Video Count: {channel.Statistics.VideoCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException?.Message);
                Console.ReadKey();
                return;
            }
            Console.WriteLine();

            //YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
            //{
            //    ApiKey = apiKey,
            //    ApplicationName = "YouTubeAPIExample",
            //});

            // Fetch YouTube videos
            var searchRequest = youtubeService.Search.List("snippet");

            Console.WriteLine("Please Enter the search query:");
            searchRequest.Q = Console.ReadLine(); // Replace with your search query

            Console.WriteLine("Live or Upcoming? (live or upcoming):");
            Console.WriteLine("Press enter to skip:");

            searchRequest.EventType = Console.ReadLine()?.ToLower() switch
            {
                "live" => SearchResource.ListRequest.EventTypeEnum.Live,
                "upcoming" => SearchResource.ListRequest.EventTypeEnum.Upcoming,
                _ => null
            };
        

            Console.WriteLine("Please enter the max results:");
            Console.WriteLine("Press enter to skip:");
            searchRequest.MaxResults = Int64.TryParse(Console.ReadLine(), out Int64 result) ? result : 1;

            SearchListResponse searchResponse = await searchRequest.ExecuteAsync();
            List<(string id,string description)> videoIds = new List<(string id,string description)>(searchResponse.Items.Count);

            foreach (SearchResult searchResult in searchResponse.Items)
            {
                if (searchResult.Id.Kind == "youtube#video")
                {
                    videoIds.Add((searchResult.Id.VideoId, searchResult.Snippet.Description));
                    Console.WriteLine($"Title: {searchResult.Snippet.Title}");
                    Console.WriteLine($"Description: {searchResult.Snippet.Description}");
                    Console.WriteLine($"Video ID: {searchResult.Id.VideoId}");
                    Console.WriteLine($"Thumbnail: {searchResult.Snippet.Thumbnails.High.Url}");
                    Console.WriteLine($"Video URL: https://www.youtube.com/watch?v={searchResult.Id.VideoId}");

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(searchResult.Id.Kind);
                }
            }

            Console.WriteLine("Would you like to create a playlist with these videos? (yes or no):");
            string? createPlaylist = Console.ReadLine();

            string playlistId = "";
            if (createPlaylist == "yes")
            {
                Console.WriteLine("Please Enter the title and description for the playlist:");
                string? title = Console.ReadLine();
                string? description = Console.ReadLine();

                Console.WriteLine("Creating a new playlist...");

                // Create a new playlist

                Playlist newplaylist = new Playlist();
                newplaylist.Snippet = new PlaylistSnippet
                {
                    Title = title,
                    Description = description
                };

                Console.WriteLine("Please Enter the privacy status for the newplaylist (public, private, or unlisted):");

                string? privacyStatus = Console.ReadLine();

                if (privacyStatus is null)
                {
                    Console.WriteLine("Defaulting to public...");
                    privacyStatus = "public";
                }

                newplaylist.Status = new PlaylistStatus
                {
                    PrivacyStatus = privacyStatus
                };

                var newplaylistInsertRequest = youtubeService.Playlists.Insert(newplaylist, "snippet,status");

                var playlistInsertResponse = await newplaylistInsertRequest.ExecuteAsync();

                playlistId = playlistInsertResponse.Id;
            }
            else if(createPlaylist == "no")
            {
                //need bug fix
                var searchListRequest = youtubeService.Search.List("snippet");
                Console.WriteLine("Please enter the name of the playlist to search for:");
                string? playlistName = Console.ReadLine();
                searchListRequest.Q = playlistName; // The name of the playlist to search for
                searchListRequest.Type = "playlist";
                searchListRequest.MaxResults = 10;
                Console.WriteLine("Press enter to fetch your playlist:");
                searchListRequest.ForMine = true;
                // Execute the search request
                var searchListResponse = await searchListRequest.ExecuteAsync();

                foreach (var searchResult in searchListResponse.Items)
                {
                    if (searchResult.Snippet.Title.Equals(playlistName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Playlist found.");
                        playlistId = searchResult.Id.PlaylistId;
                        Console.WriteLine($"Playlist ID: {searchResult.Id.PlaylistId}");
                        Console.WriteLine($"Playlist Title: {searchResult.Snippet.Title}");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid input.");
                return;
            }
            try
            {

                // Add videos to the playlist
                foreach ((string id, string description) videoId in videoIds)
                {
                    var newPlaylistItem = new PlaylistItem();
                    newPlaylistItem.Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = playlistId,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = videoId.id
                        },
                        Description = videoId.description
                    };
                    Console.WriteLine($"Adding video with ID: {videoId} to the playlist...");
                    var playlistItemInsertRequest = youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet");
                    await playlistItemInsertRequest.ExecuteAsync();
                }

                Console.WriteLine("Playlist created and videos added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }
        //{
        //    // Get the uploads playlist ID
        //    Console.WriteLine("Please Enter the channel ID:");
        //    var channelRequest = youtubeService.Channels.List("contentDetails");
        //    channelRequest.Id = Console.ReadLine();
        //    var channelResponse = await channelRequest.ExecuteAsync();

        //    var uploadsPlaylistId = channelResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;

        //    // Get the videos in the uploads playlist
        //    var playlistRequest = youtubeService.PlaylistItems.List("snippet");
        //    playlistRequest.PlaylistId = uploadsPlaylistId;
        //    playlistRequest.MaxResults = 50;

        //    var playlistResponse = await playlistRequest.ExecuteAsync();

        //    List<string> videoIds = new List<string>();

        //    foreach (var playlistItem in playlistResponse.Items)
        //    {
        //        videoIds.Add(playlistItem.Snippet.ResourceId.VideoId);
        //    }

        //    // Print the video IDs
        //    foreach (var videoId in videoIds)
        //    {
        //        Console.WriteLine(videoId);
        //    }
        //}

    }
}
