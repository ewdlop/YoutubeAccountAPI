using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

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
        var youtubeService = await InitializeYouTubeServiceAsync(clientSecretJsonFilePath);
        
        while (true)
        {
            Console.WriteLine("\nSelect an operation:");
            Console.WriteLine("1. Channel Information");
            Console.WriteLine("2. Video Search");
            Console.WriteLine("3. Playlist Operations");
            Console.WriteLine("4. Exit");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await HandleChannelOperationsAsync(youtubeService);
                    break;
                case "2":
                    await HandleVideoSearchAsync(youtubeService);
                    break;
                case "3":
                    await HandlePlaylistOperationsAsync(youtubeService);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }

    public static async Task<YouTubeService> InitializeYouTubeServiceAsync(string clientSecretJsonFilePath)
    {
        string[] scopes =[ YouTubeService.Scope.Youtube ];
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
        YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "YouTubeAPIExample"
        });

        return youtubeService;
    }

    public static async Task HandleChannelOperationsAsync(YouTubeService youtubeService)
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

                {
                    //Make Playlist Public
                    PlaylistsResource.ListRequest playlistUpdateRequest = youtubeService.Playlists.List("status");
                    //Fetch all playlists for the authenticated user
                    playlistUpdateRequest.Mine = true;
                    PlaylistListResponse playlistUpdateResponse = await playlistUpdateRequest.ExecuteAsync();
                    
                }
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
    }

    public static async Task HandleVideoSearchAsync(YouTubeService youtubeService)
    {
        // Fetch YouTube videos
        Console.WriteLine("Fetching YouTube videos...");
        var searchRequest = youtubeService.Search.List("snippet");
        searchRequest.Type = "video";
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

        int DEFAULT_MAX_RESULTS = 50;
        int DEFAULT_MIN_RESULTS = 0;
        int DEFAULT_RESULTS = 5;

        Console.WriteLine($"Please enter the max results[{DEFAULT_MIN_RESULTS}-{DEFAULT_MAX_RESULTS}]:");
        Console.WriteLine($"Press enter to skip(default to {DEFAULT_RESULTS}):");
        searchRequest.MaxResults = Int64.TryParse(Console.ReadLine(), out Int64 result) ? result : DEFAULT_RESULTS;

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
                Console.WriteLine($"Etag: {searchResult.ETag}");
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

            Console.WriteLine("Please enter a tag...");
            string? tag = Console.ReadLine();

            // Create a new playlist

            Playlist newplaylist = new Playlist();
            newplaylist.Snippet = new PlaylistSnippet
            {
                Title = title,
                Description = description,
                Tags = [tag, YouTubeService.Version, 
                    typeof(YouTubeService).Assembly.GetHashCode().ToString(), 
                    searchResponse.GetHashCode().ToString(), 
                    newplaylist.ETag,
                    searchResponse.ETag,
                    searchResponse.EventId
                ],
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
                    Description = videoId.description,
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

    public static async Task HandlePlaylistOperationsAsync(YouTubeService youtubeService)
    {
        async IAsyncEnumerable<Playlist> GetPlaylistsAsync(int maxResults = 48)
        {
            List<Playlist> results = new List<Playlist>();
            string pageToken = null;
            do
            {
                PlaylistsResource.ListRequest request = youtubeService.Playlists.List("snippet,contentDetails");
                request.MaxResults = Math.Min(maxResults, 50);
                request.PageToken = pageToken;

                PlaylistListResponse response = await request.ExecuteAsync();
                foreach (Playlist playlist in response.Items)
                {
                    yield return playlist;
                }
                maxResults -= response.Items.Count;
                pageToken = response.NextPageToken;

            } while (maxResults > 0 && !string.IsNullOrEmpty(pageToken));
        }

        async Task PrintPlaylistsAsync()
        {
            await foreach (var playlist in GetPlaylistsAsync())
            {
                Console.WriteLine($"Playlist Title: {playlist.Snippet.Title}, Playlist ID: {playlist.Id}");
            }
        }

        //Update mutiple playlists privacy status to public
        async Task UpdatePlaylistsPrivacyStatusAsync(string presentStatus, string postStatus)
        {
            await foreach (var playlist in GetPlaylistsAsync())
            {
                if (playlist.Status?.PrivacyStatus != presentStatus)
                {
                    playlist.Status = new PlaylistStatus
                    {
                        PrivacyStatus = presentStatus
                    };
                    PlaylistsResource.UpdateRequest? updateRequest = youtubeService.Playlists.Update(playlist, "status");
                    Playlist? updatedPlaylist = await updateRequest.ExecuteAsync();
                    Console.WriteLine($"Updated Playlist Title: {updatedPlaylist.Snippet.Title}, New Privacy Status: {updatedPlaylist.Status.PrivacyStatus}");
                }
            }
        }

        Console.WriteLine("Fetching your playlists...");
        Console.WriteLine("Updating all playlists to public...");
        await UpdatePlaylistsPrivacyStatusAsync("public","unlisted");

    }
}

