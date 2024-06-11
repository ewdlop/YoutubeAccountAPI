using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using static Google.Apis.YouTube.v3.ChannelsResource;


Console.WriteLine("Please enter the API Key:");
string? apiKey = Console.ReadLine();
string[] scopes = { YouTubeService.Scope.Youtube };

// Load or request authorization
UserCredential credential;
using (var stream = new FileStream("client_secret_282960065585-03sgbbvgl82eh5e2td7q5jqikhf6g0vb.apps.googleusercontent.com.json", FileMode.Open, FileAccess.Read))
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

    ChannelListResponse? channelResponse = await channelRequest.ExecuteAsync();

    foreach (var channel in channelResponse.Items)
    {
        Console.WriteLine($"Title: {channel.Snippet.Title}");
        Console.WriteLine($"Description: {channel.Snippet.Description}");
        Console.WriteLine($"View Count: {channel.Statistics.ViewCount}");
        Console.WriteLine($"Subscriber Count: {channel.Statistics.SubscriberCount}");
        Console.WriteLine($"Video Count: {channel.Statistics.VideoCount}");
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
    searchRequest.MaxResults = 50;
    searchRequest.Type = "short";

    var searchResponse = await searchRequest.ExecuteAsync();
    List<string> videoIds = new List<string>();

    foreach (var searchResult in searchResponse.Items)
    {
        if (searchResult.Id.Kind == "youtube#video")
        {
            videoIds.Add(searchResult.Id.VideoId);
            Console.WriteLine($"Title: {searchResult.Snippet.Title}");
            Console.WriteLine($"Description: {searchResult.Snippet.Description}");
            Console.WriteLine($"Video ID: {searchResult.Id.VideoId}");
            Console.WriteLine($"Thumbnail: {searchResult.Snippet.Thumbnails.High.Url}");
            Console.WriteLine($"Video URL: https://www.youtube.com/watch?v={searchResult.Id.VideoId}");

            Console.WriteLine();
        }
    }

    Console.WriteLine("Please Enter the title and description for the playlist:");
    string? title = Console.ReadLine();
    string? description = Console.ReadLine();

    // Create a new playlist
    var newPlaylist = new Playlist();
    newPlaylist.Snippet = new PlaylistSnippet
    {
        Title = title,
        Description = description
    };
    newPlaylist.Status = new PlaylistStatus
    {
        PrivacyStatus = "Private"
    };

    var playlistInsertRequest = youtubeService.Playlists.Insert(newPlaylist, "snippet,status");
    try
    {

        var playlistInsertResponse = await playlistInsertRequest.ExecuteAsync();
        string playlistId = playlistInsertResponse.Id;

        // Add videos to the playlist
        foreach (string videoId in videoIds)
        {
            var newPlaylistItem = new PlaylistItem();
            newPlaylistItem.Snippet = new PlaylistItemSnippet
            {
                PlaylistId = playlistId,
                ResourceId = new ResourceId
                {
                    Kind = "youtube#video",
                    VideoId = videoId
                }
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
{
    // Get the uploads playlist ID
    Console.WriteLine("Please Enter the channel ID:");
    var channelRequest = youtubeService.Channels.List("contentDetails");
    channelRequest.Id = Console.ReadLine();
    var channelResponse = await channelRequest.ExecuteAsync();

    var uploadsPlaylistId = channelResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;

    // Get the videos in the uploads playlist
    var playlistRequest = youtubeService.PlaylistItems.List("snippet");
    playlistRequest.PlaylistId = uploadsPlaylistId;
    playlistRequest.MaxResults = 50;

    var playlistResponse = await playlistRequest.ExecuteAsync();

    List<string> videoIds = new List<string>();

    foreach (var playlistItem in playlistResponse.Items)
    {
        videoIds.Add(playlistItem.Snippet.ResourceId.VideoId);
    }

    // Print the video IDs
    foreach (var videoId in videoIds)
    {
        Console.WriteLine(videoId);
    }
}
