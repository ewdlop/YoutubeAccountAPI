using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class YouTubeApiOAuthExample
{
    static async Task Main(string[] args)
    {
        // Specify the scope of access required
        string[] scopes = { YouTubeService.Scope.YoutubeReadonly };

        // Load or request authorization
        UserCredential credential;
        using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("YouTubeApiOAuthStore", true));
        }

        // Create the YouTube service
        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "YouTubeAPIExample"
        });

        // Call the API (same as before)
        var channelRequest = youtubeService.Channels.List("snippet,contentDetails,statistics");
        channelRequest.Mine = true;

        var channelResponse = await channelRequest.ExecuteAsync();

        foreach (var channel in channelResponse.Items)
        {
            Console.WriteLine($"Title: {channel.Snippet.Title}");
            Console.WriteLine($"Description: {channel.Snippet.Description}");
            Console.WriteLine($"View Count: {channel.Statistics.ViewCount}");
            Console.WriteLine($"Subscriber Count: {channel.Statistics.SubscriberCount}");
            Console.WriteLine($"Video Count: {channel.Statistics.VideoCount}");
        }
    }
}
