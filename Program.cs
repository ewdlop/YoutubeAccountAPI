using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

#if WINDOWS
using System.Runtime.InteropServices;

#endif
using static Google.Apis.YouTube.v3.ChannelsResource;

// Encrypted playlist data structure
public record EncryptedPlaylistData(DateTime CreatedAt)
{
    public string PlaylistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EncryptedTitle { get; set; } = string.Empty; // Encrypted version
    public string Description { get; set; } = string.Empty;
    public string EncryptedDescription { get; set; } = string.Empty; // Encrypted version
    public List<string> VideoIds { get; set; } = new();
    public string PrivacyStatus { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> EncryptedTags { get; set; } = new(); // Encrypted version
    public string StorageLocation { get; set; } = "Local";
    public string BackupSource { get; set; } = "YouTube API";
    public bool IsAnonymous { get; set; } = false; // Track if created anonymously
    public string CreatorHash { get; set; } = string.Empty; // Hashed creator identifier
}

// Anonymous playlist helper
public static class AnonymousPlaylistHelper
{
    private static readonly string[] AnonymousTitles = [
        "Untitled Collection",
        "Music Mix",
        "Video Collection",
        "Compilation",
        "Playlist #1",
        "Random Videos",
        "Mixed Content",
        "Entertainment Mix"
    ];

    private static readonly string[] GenericDescriptions = [
        "A collection of videos",
        "Mixed content playlist",
        "Various videos compilation",
        "Entertainment collection",
        "Video mix",
        ""
    ];

    public static string GenerateAnonymousTitle()
    {
        var random = new Random();
        var baseTitle = AnonymousTitles[random.Next(AnonymousTitles.Length)];
        var randomNumber = random.Next(1000, 9999);
        return $"{baseTitle} {randomNumber}";
    }

    public static string GenerateGenericDescription()
    {
        var random = new Random();
        return GenericDescriptions[random.Next(GenericDescriptions.Length)];
    }

    public static string CreateCreatorHash(string originalCreator)
    {
        using var sha256 = SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalCreator + "AnonymousSalt2024"));
        return Convert.ToBase64String(hashedBytes)[..12]; // First 12 characters
    }

    public static List<string> GenerateGenericTags()
    {
        return ["music", "videos", "entertainment", "collection", "mix"];
    }
}

// Enhanced encryption helper class
public static class PlaylistEncryption
{
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("YouTubePlaylistSalt2024");
    private static readonly string LocalStorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YouTubePlaylistBackups");

    static PlaylistEncryption()
    {
        // Ensure local storage directory exists
        if (!Directory.Exists(LocalStorageDirectory))
        {
            Directory.CreateDirectory(LocalStorageDirectory);
            Console.WriteLine($"📁 Created local storage directory: {LocalStorageDirectory}");
        }
    }

    public static string EncryptString(string plaintext, string password)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;
        
        byte[] dataBytes = Encoding.UTF8.GetBytes(plaintext);
        using var aes = Aes.Create();
        var key = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(dataBytes, 0, dataBytes.Length);
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string DecryptString(string encryptedText, string password)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
        
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(encryptedText);
            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            return "[Decryption Failed]";
        }
    }

    public static string EncryptPlaylistData(EncryptedPlaylistData playlistData, string password)
    {
        string jsonData = JsonSerializer.Serialize(playlistData, new JsonSerializerOptions { WriteIndented = true });
        byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

        using var aes = Aes.Create();
        var key = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
        aes.Key = key.GetBytes(32);
        aes.IV = key.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(dataBytes, 0, dataBytes.Length);
        cs.FlushFinalBlock();

        return Convert.ToBase64String(ms.ToArray());
    }

    public static EncryptedPlaylistData? DecryptPlaylistData(string encryptedData, string password)
    {
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            string jsonData = sr.ReadToEnd();
            return JsonSerializer.Deserialize<EncryptedPlaylistData>(jsonData);
        }
        catch
        {
            return null;
        }
    }

    public static void SaveEncryptedPlaylist(EncryptedPlaylistData playlistData, string password, string fileName)
    {
        string fullPath = Path.Combine(LocalStorageDirectory, fileName);
        string encryptedData = EncryptPlaylistData(playlistData, password);
        File.WriteAllText(fullPath, encryptedData);
        
        Console.WriteLine($"✅ Encrypted playlist saved locally to: {fullPath}");
        Console.WriteLine($"💾 Local storage location: {LocalStorageDirectory}");
        Console.WriteLine($"🔐 Data encrypted with AES-256 encryption");
        Console.WriteLine($"📍 Storage type: Local file system (offline backup)");
        if (playlistData.IsAnonymous)
        {
            Console.WriteLine($"🥷 Anonymous playlist - sensitive data encrypted");
        }
    }

    public static EncryptedPlaylistData? LoadEncryptedPlaylist(string filePath, string password)
    {
        // If only filename provided, look in local storage directory
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(LocalStorageDirectory, filePath);
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("❌ Encrypted playlist file not found in local storage.");
            Console.WriteLine($"📁 Searched in: {LocalStorageDirectory}");
            return null;
        }

        string encryptedData = File.ReadAllText(filePath);
        var decryptedData = DecryptPlaylistData(encryptedData, password);
        
        if (decryptedData != null)
        {
            Console.WriteLine("✅ Playlist successfully decrypted and loaded from local storage.");
            Console.WriteLine($"📍 Loaded from: {filePath}");
        }
        else
        {
            Console.WriteLine("❌ Failed to decrypt playlist. Invalid password or corrupted data.");
        }

        return decryptedData;
    }

    public static void ShowLocalStorageInfo()
    {
        Console.WriteLine("\n📁 Local Storage Information:");
        Console.WriteLine($"Storage Directory: {LocalStorageDirectory}");
        
        if (Directory.Exists(LocalStorageDirectory))
        {
            var files = Directory.GetFiles(LocalStorageDirectory, "*.enc");
            Console.WriteLine($"Encrypted files found: {files.Length}");
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                Console.WriteLine($"  • {Path.GetFileName(file)} ({fileInfo.Length / 1024.0:F1} KB) - {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm}");
            }
        }
        Console.WriteLine("🔒 All data is stored locally on your device");
        Console.WriteLine("🌐 No data is uploaded to external servers");
    }
}

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
            openFileDialog.Filter = filter ??"All files (*.*)|*.*|Text files (*.txt)|*.txt|Encrypted files (*.enc)|*.enc";
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
            Console.WriteLine("4. Create Anonymous Playlist");
            Console.WriteLine("5. Encrypt Playlist (Local Storage)");
            Console.WriteLine("6. Decrypt Playlist (Local Storage)");
            Console.WriteLine("7. Export All Playlists (Local Encrypted Backup)");
            Console.WriteLine("8. View Local Storage Info");
            Console.WriteLine("9. Exit");
            
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
                    await HandleCreateAnonymousPlaylistAsync(youtubeService);
                    break;
                case "5":
                    await HandleEncryptPlaylistAsync(youtubeService);
                    break;
                case "6":
                    await HandleDecryptPlaylistAsync();
                    break;
                case "7":   
                    await HandleExportEncryptedPlaylistAsync(youtubeService);
                    break;
                case "8":
                    PlaylistEncryption.ShowLocalStorageInfo();
                    break;
                case "9":
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

        YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "YouTubeAPIExample"
        });

        return youtubeService;
    }

    public static async Task HandleCreateAnonymousPlaylistAsync(YouTubeService youtubeService)
    {
        Console.WriteLine("🥷 Create Anonymous Playlist");
        Console.WriteLine("This will create a playlist with generic/randomized metadata");
        Console.WriteLine("Your real title and description will be encrypted and stored locally");
        
        Console.WriteLine("\nEnter your REAL playlist title (will be encrypted locally):");
        string? realTitle = Console.ReadLine();
        
        Console.WriteLine("Enter your REAL playlist description (will be encrypted locally):");
        string? realDescription = Console.ReadLine();
        
        Console.WriteLine("Enter tags separated by commas (will be encrypted locally):");
        string? realTagsInput = Console.ReadLine();
        var realTags = realTagsInput?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>();

        // Generate anonymous metadata for YouTube
        string anonymousTitle = AnonymousPlaylistHelper.GenerateAnonymousTitle();
        string anonymousDescription = AnonymousPlaylistHelper.GenerateGenericDescription();
        var anonymousTags = AnonymousPlaylistHelper.GenerateGenericTags();

        Console.WriteLine($"\nPublic title will be: {anonymousTitle}");
        Console.WriteLine($"Public description: {anonymousDescription}");
        Console.WriteLine("Proceed? (yes/no):");
        
        if (Console.ReadLine()?.ToLower() != "yes")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        try
        {
            // Create the playlist on YouTube with anonymous data
            var playlist = new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = anonymousTitle,
                    Description = anonymousDescription,
                    Tags = anonymousTags
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private" // Anonymous playlists should be private
                }
            };

            var insertRequest = youtubeService.Playlists.Insert(playlist, "snippet,status");
            var response = await insertRequest.ExecuteAsync();

            Console.WriteLine($"✅ Anonymous playlist created with ID: {response.Id}");

            // Encrypt and store the real metadata locally
            Console.WriteLine("Enter password to encrypt real metadata:");
            string? password = Console.ReadLine();

            if (!string.IsNullOrEmpty(password))
            {
                var encryptedData = new EncryptedPlaylistData(DateTime.Now)
                {
                    PlaylistId = response.Id,
                    Title = anonymousTitle, // Public title
                    EncryptedTitle = PlaylistEncryption.EncryptString(realTitle ?? "", password),
                    Description = anonymousDescription, // Public description
                    EncryptedDescription = PlaylistEncryption.EncryptString(realDescription ?? "", password),
                    PrivacyStatus = "private",
                    Tags = anonymousTags, // Public tags
                    EncryptedTags = realTags.Select(tag => PlaylistEncryption.EncryptString(tag, password)).ToList(),
                    VideoIds = new List<string>(),
                    StorageLocation = "Local Device",
                    BackupSource = "YouTube Data API v3",
                    IsAnonymous = true,
                    CreatorHash = AnonymousPlaylistHelper.CreateCreatorHash(Environment.UserName)
                };

                string fileName = $"anonymous_playlist_{response.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
                PlaylistEncryption.SaveEncryptedPlaylist(encryptedData, password, fileName);
                
                Console.WriteLine("🔐 Real metadata encrypted and stored locally");
                Console.WriteLine("🥷 Anonymous playlist successfully created");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating anonymous playlist: {ex.Message}");
        }
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
        Console.WriteLine("Fetching YouTube videos...");
        var searchRequest = youtubeService.Search.List("snippet");
        searchRequest.Type = "video";
        Console.WriteLine("Please Enter the search query:");
        searchRequest.Q = Console.ReadLine();

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
                Console.WriteLine($"🎥 YouTube Video: {searchResult.Snippet.Title}");
                Console.WriteLine($"📝 Description: {searchResult.Snippet.Description}");
                Console.WriteLine($"🆔 Video ID: {searchResult.Id.VideoId}");
                Console.WriteLine($"🔗 YouTube URL: https://www.youtube.com/watch?v={searchResult.Id.VideoId}");
                Console.WriteLine($"📅 Published: {searchResult.Snippet.PublishedAt}");
                Console.WriteLine($"📺 Channel: {searchResult.Snippet.ChannelTitle}");
                Console.WriteLine("─" + new string('─', 60));
            }
            else
            {
                Console.WriteLine(searchResult.Id.Kind);
            }
        }

        Console.WriteLine("\n📊 Data provided by YouTube Data API v3");
        Console.WriteLine($"⏰ Results fetched at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        Console.WriteLine("Would you like to create a playlist with these videos? (yes/no/anonymous):");
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

            Playlist newplaylist = new Playlist();
            newplaylist.Snippet = new PlaylistSnippet
            {
                Title = title,
                Description = $"{description}\n\nVideos sourced from YouTube via YouTube Data API v3",
                Tags = [tag, "YouTube", "YouTube-API", "Generated-Playlist", YouTubeService.Version, 
                    typeof(YouTubeService).Assembly.GetHashCode().ToString(), 
                    searchResponse.GetHashCode().ToString()],
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
        else if (createPlaylist == "anonymous")
        {
            // Create anonymous playlist with found videos
            string anonymousTitle = AnonymousPlaylistHelper.GenerateAnonymousTitle();
            string anonymousDescription = AnonymousPlaylistHelper.GenerateGenericDescription();
            var anonymousTags = AnonymousPlaylistHelper.GenerateGenericTags();

            var playlist = new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = anonymousTitle,
                    Description = anonymousDescription,
                    Tags = anonymousTags
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private"
                }
            };

            var insertRequest = youtubeService.Playlists.Insert(playlist, "snippet,status");
            var response = await insertRequest.ExecuteAsync();
            playlistId = response.Id;
            
            Console.WriteLine($"✅ Anonymous playlist created: {anonymousTitle}");
        }
        else if(createPlaylist == "no")
        {
            var searchListRequest = youtubeService.Search.List("snippet");
            Console.WriteLine("Please enter the name of the playlist to search for:");
            string? playlistName = Console.ReadLine();
            searchListRequest.Q = playlistName;
            searchListRequest.Type = "playlist";
            searchListRequest.MaxResults = 10;
            Console.WriteLine("Press enter to fetch your playlist:");
            searchListRequest.ForMine = true;
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
            string pageToken = null;
            do
            {
                PlaylistsResource.ListRequest request = youtubeService.Playlists.List("snippet,contentDetails");
                request.MaxResults = Math.Min(maxResults, 50);
                request.PageToken = pageToken;
                request.Mine = true;

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

        async Task UpdatePlaylistsPrivacyStatusAsync(string targetStatus)
        {
            await foreach (var playlist in GetPlaylistsAsync())
            {
                if (playlist.Status?.PrivacyStatus != targetStatus)
                {
                    playlist.Status = new PlaylistStatus
                    {
                        PrivacyStatus = targetStatus
                    };
                    PlaylistsResource.UpdateRequest? updateRequest = youtubeService.Playlists.Update(playlist, "status");
                    Playlist? updatedPlaylist = await updateRequest.ExecuteAsync();
                    Console.WriteLine($"Updated Playlist Title: {updatedPlaylist.Snippet.Title}, New Privacy Status: {updatedPlaylist.Status.PrivacyStatus}");
                }
            }
        }
    
        Console.WriteLine("Fetching your playlists...");
        await PrintPlaylistsAsync();
        
        Console.WriteLine("\nWould you like to update playlist privacy status? (yes/no):");
        if (Console.ReadLine()?.ToLower() == "yes")
        {
            Console.WriteLine("Enter target privacy status (public/private/unlisted):");
            string targetStatus = Console.ReadLine() ?? "public";
            await UpdatePlaylistsPrivacyStatusAsync(targetStatus);
        }
    }

    public static async Task HandleEncryptPlaylistAsync(YouTubeService youtubeService)
    {
        Console.WriteLine("🔒 Encrypt Playlist Data (Local Storage)");
        Console.WriteLine("Enter the Playlist ID to encrypt:");
        string? playlistId = Console.ReadLine();

        if (string.IsNullOrEmpty(playlistId))
        {
            Console.WriteLine("❌ Invalid playlist ID.");
            return;
        }

        try
        {
            // Fetch playlist details
            var playlistRequest = youtubeService.Playlists.List("snippet,status");
            playlistRequest.Id = playlistId;
            var playlistResponse = await playlistRequest.ExecuteAsync();

            if (playlistResponse.Items.Count == 0)
            {
                Console.WriteLine("❌ Playlist not found.");
                return;
            }

            var playlist = playlistResponse.Items[0];

            // Fetch playlist items
            var itemsRequest = youtubeService.PlaylistItems.List("snippet");
            itemsRequest.PlaylistId = playlistId;
            itemsRequest.MaxResults = 50;
            var itemsResponse = await itemsRequest.ExecuteAsync();

            Console.WriteLine("Enter encryption password:");
            string? password = Console.ReadLine();

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("❌ Password cannot be empty.");
                return;
            }

            var encryptedData = new EncryptedPlaylistData(DateTime.Now)
            {
                PlaylistId = playlist.Id,
                Title = playlist.Snippet.Title,
                EncryptedTitle = PlaylistEncryption.EncryptString(playlist.Snippet.Title, password),
                Description = playlist.Snippet.Description,
                EncryptedDescription = PlaylistEncryption.EncryptString(playlist.Snippet.Description ?? "", password),
                PrivacyStatus = playlist.Status?.PrivacyStatus ?? "unknown",
                Tags = playlist.Snippet.Tags?.ToList() ?? new List<string>(),
                EncryptedTags = (playlist.Snippet.Tags ?? new List<string>())
                    .Select(tag => PlaylistEncryption.EncryptString(tag, password)).ToList(),
                VideoIds = itemsResponse.Items.Select(item => item.Snippet.ResourceId.VideoId).ToList(),
                StorageLocation = "Local Device",
                BackupSource = "YouTube Data API v3",
                IsAnonymous = false,
                CreatorHash = AnonymousPlaylistHelper.CreateCreatorHash(Environment.UserName)
            };

            string fileName = $"encrypted_playlist_{playlist.Snippet.Title?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
            PlaylistEncryption.SaveEncryptedPlaylist(encryptedData, password, fileName);

            Console.WriteLine($"📋 Encrypted playlist contains {encryptedData.VideoIds.Count} videos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error encrypting playlist: {ex.Message}");
        }
    }

    public static async Task HandleDecryptPlaylistAsync()
    {
        Console.WriteLine("🔓 Decrypt Playlist Data (Local Storage)");
        Console.WriteLine("Available options:");
        Console.WriteLine("1. Enter filename only (will search in local storage directory)");
        Console.WriteLine("2. Enter full file path");
        Console.WriteLine("Enter the encrypted file name or path:");
        string? filePath = Console.ReadLine();

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("❌ File path cannot be empty.");
            return;
        }

        Console.WriteLine("Enter decryption password:");
        string? password = Console.ReadLine();

        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("❌ Password cannot be empty.");
            return;
        }

        var decryptedData = PlaylistEncryption.LoadEncryptedPlaylist(filePath, password);

        if (decryptedData != null)
        {
            Console.WriteLine("\n📋 Decrypted Playlist Information (Local Backup):");
            Console.WriteLine($"Playlist ID: {decryptedData.PlaylistId}");
            
            if (decryptedData.IsAnonymous)
            {
                Console.WriteLine("🥷 ANONYMOUS PLAYLIST DETECTED");
                Console.WriteLine($"Public Title: {decryptedData.Title}");
                Console.WriteLine($"Real Title: {PlaylistEncryption.DecryptString(decryptedData.EncryptedTitle, password)}");
                Console.WriteLine($"Public Description: {decryptedData.Description}");
                Console.WriteLine($"Real Description: {PlaylistEncryption.DecryptString(decryptedData.EncryptedDescription, password)}");
                Console.WriteLine($"Real Tags: {string.Join(", ", decryptedData.EncryptedTags.Select(tag => PlaylistEncryption.DecryptString(tag, password)))}");
            }
            else
            {
                Console.WriteLine($"Title: {decryptedData.Title}");
                Console.WriteLine($"Description: {decryptedData.Description}");
                Console.WriteLine($"Tags: {string.Join(", ", decryptedData.Tags)}");
            }
            
            Console.WriteLine($"Privacy Status: {decryptedData.PrivacyStatus}");
            Console.WriteLine($"Created: {decryptedData.CreatedAt}");
            Console.WriteLine($"Video Count: {decryptedData.VideoIds.Count}");
            Console.WriteLine($"Storage: {decryptedData.StorageLocation}");
            Console.WriteLine($"Source: {decryptedData.BackupSource}");
            Console.WriteLine($"Creator Hash: {decryptedData.CreatorHash}");
            
            Console.WriteLine("\nVideos:");
            foreach (var videoId in decryptedData.VideoIds)
            {
                Console.WriteLine($"  • https://www.youtube.com/watch?v={videoId}");
            }
        }
    }

    public static async Task HandleExportEncryptedPlaylistAsync(YouTubeService youtubeService)
    {
        Console.WriteLine("📤 Export All Playlists (Local Encrypted Backup)");
        Console.WriteLine("Enter encryption password:");
        string? password = Console.ReadLine();

        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("❌ Password cannot be empty.");
            return;
        }

        try
        {
            var playlistsRequest = youtubeService.Playlists.List("snippet,status");
            playlistsRequest.Mine = true;
            playlistsRequest.MaxResults = 50;
            var playlistsResponse = await playlistsRequest.ExecuteAsync();

            Console.WriteLine($"🔄 Processing {playlistsResponse.Items.Count} playlists for local backup...");

            foreach (var playlist in playlistsResponse.Items)
            {
                var itemsRequest = youtubeService.PlaylistItems.List("snippet");
                itemsRequest.PlaylistId = playlist.Id;
                itemsRequest.MaxResults = 50;
                var itemsResponse = await itemsRequest.ExecuteAsync();

                var encryptedData = new EncryptedPlaylistData(DateTime.Now)
                {
                    PlaylistId = playlist.Id,
                    Title = playlist.Snippet.Title,
                    EncryptedTitle = PlaylistEncryption.EncryptString(playlist.Snippet.Title, password),
                    Description = playlist.Snippet.Description,
                    EncryptedDescription = PlaylistEncryption.EncryptString(playlist.Snippet.Description ?? "", password),
                    PrivacyStatus = playlist.Status?.PrivacyStatus ?? "unknown",
                    Tags = playlist.Snippet.Tags?.ToList() ?? new List<string>(),
                    EncryptedTags = (playlist.Snippet.Tags ?? new List<string>())
                        .Select(tag => PlaylistEncryption.EncryptString(tag, password)).ToList(),
                    VideoIds = itemsResponse.Items.Select(item => item.Snippet.ResourceId.VideoId).ToList(),
                    StorageLocation = "Local Device",
                    BackupSource = "YouTube Data API v3",
                    IsAnonymous = false,
                    CreatorHash = AnonymousPlaylistHelper.CreateCreatorHash(Environment.UserName)
                };

                string fileName = $"encrypted_playlist_{playlist.Snippet.Title?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
                PlaylistEncryption.SaveEncryptedPlaylist(encryptedData, password, fileName);
            }

            Console.WriteLine($"✅ Successfully exported {playlistsResponse.Items.Count} encrypted playlists to local storage");
            Console.WriteLine("🔐 All data is encrypted and stored locally on your device");
            Console.WriteLine("🌐 No data has been uploaded to external servers");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error exporting playlists: {ex.Message}");
        }
    }
}

