using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using StudioDrydock.AppStoreConnect.Api;
using StudioDrydock.AppStoreConnect.Lib;
using StudioDrydock.AppStoreConnect.Lib.Files;

namespace StudioDrydock.AppStoreConnect.Cli;

internal static class Program
{
    private const string Description =
        @"Demonstration of AppStoreConnect. To set up authorization, you will need a config.json\n" +
        @"(by default in ~/.config/AppStoreConnect.json) with the following structure:\n" +
        @"\n" +
        @"  {\n" +
        @"    ""isUser"": false,\n" +
        @"    ""keyId"": ""xxxxxxxxxx"",\n" +
        @"    ""keyPath"": ""AppStoreConnect_xxxxxxxxxx.p8"",\n" +
        @"    ""issuerId"": ""xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx""\n" +
        @"  }\n" +
        @"\n" +
        @"For a user token, set isUser to true and issuerId to null. For a team token, set isUser to false\n" +
        @"and issuerId to the issuer ID of the team. The keyPath should be the name of the key file, and\n" +
        @"keyId should be the key ID. The keyPath is relative to the directory of the config file.\n" +
        @"\n" +
        @"You can obtain these values, and the key file, from the Keys section in \n" +
        @"https://appstoreconnect.apple.com/access/api";

    // --config=<config.json>
    private static readonly Option<FileInfo> ConfigOpt = new("--config")
    {
        Description = "JSON configuration file for authorization",
        DefaultValueFactory = GetDefaultConfigFile,
    };

    // --verbose/-v
    private static readonly Option<bool> VerboseOpt = new("-v", "--verbose")
    {
        Description = "Enable trace logging of HTTP requests"
    };

    // --output/-o
    private static readonly Option<FileInfo> OutputOpt = new("-o", "--output")
    {
        Description = "Specify file to write output to, defaults to stdout",
    };

    // --input/-i
    private static readonly Option<FileInfo> InputOpt = new("-i", "--input")
    {
        Description = "Specify file to read input from, defaults to stdin",
    };

    private static readonly Option<string> AppIdOpt = new("--appId")
    {
        Description = "Application ID of the app to get data for.",
        Required = true,
    };

    private static readonly Option<AppStoreClient.Apps_inAppPurchasesV2_getToManyRelatedFilterState?> IapStateOpt = new("--state")
    {
        Description = "Filter in-app purchases by state",
    };

    private static readonly Option<AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterPlatform?> PlatformOpt = new("--platform")
    {
        Description = "Filter app versions by platform",
    };

    private static readonly Option<AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterAppStoreState?> AppStoreStateOpt = new("--state")
    {
        Description = "Filter app versions by state",
    };

    private static readonly Option<int?> VersionLimitOpt = new("--limitVersions")
    {
        Description = "Limit the number of versions to fetch for each platform",
    };

    private static readonly Option<FileInfo> GoogleSecretsOpt = new("--googleClientSecrets")
    {
        Description = "Google OAuth secrets file",
        DefaultValueFactory = GetDefaultGoogleClientSecretsFile,
    };

    private static readonly Option<DirectoryInfo> GoogleDataStoreOpt = new("--googleDataStore")
    {
        Description = "Google OAuth data store directory",
        DefaultValueFactory = GetDefaultGoogleDataStoreFolder,
    };

    private static readonly Option<string?> SpreadsheetId = new("--spreadsheetId")
    {
        Description = "Google Sheets spreadsheet ID",
    };

    private static readonly Option<bool> SkipLocalizationOpt = new("--skipLocalization")
    {
        Description = "Skip uploading localization data",
        DefaultValueFactory = _ => false,
    };
    
    private static readonly Option<bool> SkipLiveAchievementsOpt = new("--skipLiveAchievements")
    {
        Description = "Skip uploading live achievements",
        DefaultValueFactory = _ => true,
    };
    
    private static readonly Option<bool> SkipLiveLeaderboardsOpt = new("--skipLiveLeaderboards")
    {
        Description = "Skip uploading live leaderboards",
        DefaultValueFactory = _ => true,
    };
    
    private static readonly Option<bool> SkipLiveLeaderboardSetsOpt = new("--skipLiveLeaderboardSets")
    {
        Description = "Skip uploading live leaderboards sets",
        DefaultValueFactory = _ => true,
    };
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    private static FileInfo GetDefaultConfigFile(ArgumentResult argumentResult)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        Directory.CreateDirectory(folder);
        return new FileInfo(Path.Combine(folder, "AppStoreConnect.json"));
    }

    private static FileInfo GetDefaultGoogleClientSecretsFile(ArgumentResult argumentResult)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        Directory.CreateDirectory(folder);
        return new FileInfo(Path.Combine(folder, "GoogleClientSecrets.json"));
    }

    private static DirectoryInfo GetDefaultGoogleDataStoreFolder(ArgumentResult argumentResult)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        Directory.CreateDirectory(folder);
        return new DirectoryInfo(Path.Combine(folder, "GoogleDataStore"));
    }

    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // get-apps
        var getAppsCommand = new Command("get-apps", "Get list of all apps")
        {
            OutputOpt
        };
        getAppsCommand.SetAction(GetApps);

        // get-app-versions --appId=xxx --platform=xxx --state=xxx --limitVersions=xxx --output=file.json
        var getAppVersionsCommand = new Command("get-app-versions", "Get information about specific app versions")
        {
            AppIdOpt,
            PlatformOpt,
            AppStoreStateOpt,
            VersionLimitOpt,
            OutputOpt
        };
        getAppVersionsCommand.SetAction(GetAppVersions);

        // set-app-versions --appId=xxx --input=file.json --output=file.json
        var setAppVersionsCommand = new Command("set-app-versions", "Update localizations for specific app versions. The input format matches the output of get-app-versions.")
        {
            AppIdOpt,
            InputOpt,
            OutputOpt
        };
        setAppVersionsCommand.SetAction(SetAppVersions);

        // get-app-iaps --appId=xxx --state=xxx --output=file.json
        var getAppIapsCommand = new Command("get-app-iaps", "Get information about specific app in-app purchases")
        {
            AppIdOpt,
            IapStateOpt,
            OutputOpt
        };
        getAppIapsCommand.SetAction(GetAppIaps);

        // set-app-iaps --appId=xxx --input=file.json --output=file.json
        var setAppIapsCommand = new Command("set-app-iaps", "Update information about specific app in-app purchases. The input format matches the output of get-app-iaps.")
        {
            AppIdOpt,
            InputOpt,
            OutputOpt
        };
        setAppIapsCommand.SetAction(SetAppIaps);

        // get-app-events --appId=xxx --output=file.json
        var getEventsCommand = new Command("get-app-events", "Get information about specific app events")
        {
            AppIdOpt,
            OutputOpt
        };
        getEventsCommand.SetAction(GetAppEvents);

        // set-app-events --appId=xxx --input=file.json --output=file.json
        var setEventsCommand = new Command("set-app-events", "Update information about specific app events. The input format matches the output of get-events.")
        {
            AppIdOpt,
            InputOpt,
            OutputOpt
        };
        setEventsCommand.SetAction(SetAppEvents);

        // get-game-center --appId=xxx --output=file.json
        var getGameCenterCommand = new Command("get-game-center", "Get information about specific app Game Center")
        {
            AppIdOpt,
            OutputOpt
        };
        getGameCenterCommand.SetAction(GetGameCenter);

        // set-game-center --appId=xxx --input=file.json --output=file.json
        var setGameCenterCommand = new Command("set-game-center", "Update information about specific app Game Center. The input format matches the output of get-game-center.")
        {
            AppIdOpt,
            InputOpt,
            OutputOpt,
            SkipLocalizationOpt,
            SkipLiveAchievementsOpt,
            SkipLiveLeaderboardsOpt,
            SkipLiveLeaderboardSetsOpt
        };
        setGameCenterCommand.SetAction(SetGameCenter);

        // put-game-center-on-sheets --input=file.json --googleClientSecrets=file.json --googleDataStore=folder --spreadsheetId=xxx
        var putGameCenterOnSheetsCmd = new Command("put-game-center-on-sheets", "Test Google Sheets API")
        {
            InputOpt,
            GoogleSecretsOpt,
            GoogleDataStoreOpt,
            SpreadsheetId
        };
        putGameCenterOnSheetsCmd.SetAction(PutGameCenterOnSheets);

        // update-game-center-from-sheets --input=file.json --output=file.json --googleClientSecrets=file.json --googleDataStore=folder --spreadsheetId=xxx
        var updateGameCenterFromSheetsCmd = new Command("update-game-center-from-sheets", "Update Game Center data from Google Sheets")
        {
            InputOpt,
            OutputOpt,
            GoogleSecretsOpt,
            GoogleDataStoreOpt,
            SpreadsheetId
        };
        updateGameCenterFromSheetsCmd.SetAction(UpdateGameCenterFromSheets);

        var rootCommand = new RootCommand(Description)
        {
            ConfigOpt,
            VerboseOpt,

            getAppsCommand,
            getAppVersionsCommand,
            setAppVersionsCommand,
            getAppIapsCommand,
            setAppIapsCommand,
            getEventsCommand,
            setEventsCommand,
            getGameCenterCommand,
            setGameCenterCommand,
            putGameCenterOnSheetsCmd,
            updateGameCenterFromSheetsCmd
        };

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }

    private static INestedLog CreateLog(ParseResult parseResult)
    {
        INestedLog log;
        if (parseResult.GetValue(VerboseOpt))
        {
            log = new ConsoleLogger(3);
        }
        else
        {
            log = new ConsoleLogger(0);
        }

        return log;
    }

    private static AppStoreClient CreateClient(ParseResult parseResult)
    {
        var configFile = parseResult.GetValue(ConfigOpt) ?? throw new Exception("config.json is required");
        var tokenMaker = CreateTokenMaker(configFile);

        return new AppStoreClient(tokenMaker);
    }

    private static IAppStoreClientTokenMaker CreateTokenMaker(FileInfo configFile)
    {
        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile.FullName)) ?? throw new Exception($"Failed to parse {configFile}");
        var configDirectory = configFile.Directory?.FullName ?? throw new Exception($"Failed to get directory of {configFile}");

        var keyFile = Path.Combine(configDirectory, config.keyPath ?? throw new Exception("keyPath is required"));
        var keyData = File.ReadAllText(keyFile);

        IAppStoreClientTokenMaker tokenMaker;

        if (config.isUser == true)
        {
            tokenMaker = AppStoreClientTokenMakerFactory.CreateUser(
                keyData,
                config.keyId ?? throw new Exception("keyId is required"));
        }
        else
        {
            tokenMaker = AppStoreClientTokenMakerFactory.CreateTeam(
                keyData,
                config.keyId ?? throw new Exception("keyId is required"),
                config.issuerId ?? throw new Exception("issuerId is required"));
        }

        return tokenMaker;
    }

    private static T Input<T>(ParseResult parseResult)
    {
        var input = parseResult.GetValue(InputOpt);
        string text;
        if (input != null)
        {
            text = File.ReadAllText(input.FullName);
        }
        else
        {
            using var stream = Console.OpenStandardInput();
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
        }

        var result = JsonSerializer.Deserialize<T>(text) ?? throw new Exception("Failed to deserialize input");
        return result;
    }

    private static void Output(ParseResult parseResult, object result)
    {
        var text = JsonSerializer.Serialize(result, JsonSerializerOptions);
        var output = parseResult.GetValue(OutputOpt);
        if (output == null)
            Console.WriteLine(text);
        else
            File.WriteAllText(output.FullName, text);
    }

    private static AssetDatabase? CreateAssetDatabaseNextToFile(FileInfo? fi)
    {
        if (fi == null || fi.Directory == null)
            return null;
        return new AssetDatabase(fi.Directory);
    }

    // get-apps
    private static async Task GetApps(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);

        var appInfoList = await LogEx.StdLog(
            log,
            nameof(GetApps),
            log => AscTasks.GetAppInfoList(api, log));

        Output(parseResult, appInfoList);
    }

    // get-app-versions
    private static async Task GetAppVersions(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var ad = CreateAssetDatabaseNextToFile(parseResult.GetValue(OutputOpt));

        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var platform = parseResult.GetValue(PlatformOpt);
        var appStoreState = parseResult.GetValue(AppStoreStateOpt);
        var limit = parseResult.GetValue(VersionLimitOpt);

        var app = await LogEx.StdLog(
            log,
            nameof(GetAppVersions),
            log => AscTasks.GetApp(api, ad, appId, platform, appStoreState, limit, log));

        Output(parseResult, app);
    }

    // set-app-versions
    private static async Task SetAppVersions(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var ad = CreateAssetDatabaseNextToFile(parseResult.GetValue(InputOpt)) ?? throw new Exception($"{InputOpt.Name} is required");
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var versions = Input<App>(parseResult);

        try
        {
            await LogEx.StdLog(
                log,
                nameof(SetAppVersions),
                log => AscTasks.PutApp(api, ad, appId, versions, log));
        }
        finally
        {
            Output(parseResult, versions);
        }
    }

    // get-app-iaps
    private static async Task GetAppIaps(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var state = parseResult.GetValue(IapStateOpt);

        var iapList = await LogEx.StdLog(
            log,
            nameof(GetAppIaps),
            log => AscTasks.GetIaps(api, appId, state, log));

        Output(parseResult, iapList);
    }

    // set-app-iaps
    private static async Task SetAppIaps(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var iaps = Input<IapList>(parseResult);

        try
        {
            await LogEx.StdLog(
                log,
                nameof(SetAppIaps),
                log => AscTasks.PutIaps(api, appId, iaps, log));
        }
        finally
        {
            Output(parseResult, iaps);
        }
    }

    // get-app-events
    private static async Task GetAppEvents(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");

        var events = await LogEx.StdLog(
            log,
            nameof(GetAppEvents),
            log => AscTasks.GetEventList(log, api, appId));

        Output(parseResult, events);
    }

    // set-app-events
    private static async Task SetAppEvents(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var events = Input<EventList>(parseResult);

        try
        {
            await LogEx.StdLog(
                log,
                nameof(SetAppEvents),
                log => AscTasks.PutEventList(api, log, appId, events));
        }
        finally
        {
            Output(parseResult, events);
        }
    }

    // get-game-center
    private static async Task GetGameCenter(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");

        var gc = await LogEx.StdLog(
            log,
            nameof(GetGameCenter),
            log => AscTasks.GetGameCenter(log, api, appId));

        Output(parseResult, gc);
    }

    // set-game-center
    private static async Task SetGameCenter(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var api = CreateClient(parseResult);
        var ad = CreateAssetDatabaseNextToFile(parseResult.GetValue(InputOpt)) ?? throw new Exception($"{InputOpt.Name} is required");
        var appId = parseResult.GetValue(AppIdOpt) ?? throw new Exception($"{AppIdOpt.Name} is required");
        var skipLocalization = parseResult.GetValue(SkipLocalizationOpt);
        var skipLiveAchievements = parseResult.GetValue(SkipLiveAchievementsOpt);
        var skipLiveLeaderboards = parseResult.GetValue(SkipLiveLeaderboardsOpt);
        var skipLiveLeaderboardSets = parseResult.GetValue(SkipLiveLeaderboardSetsOpt);
        var gc = Input<GameCenter>(parseResult);

        try
        {
            await LogEx.StdLog(
                log,
                nameof(SetGameCenter),
                log => AscTasks.PutGameCenter(api, ad, log, appId, skipLocalization, skipLiveAchievements, skipLiveLeaderboards, skipLiveLeaderboardSets, gc));
        }
        finally
        {
            Output(parseResult, gc);
        }
    }

    // put-game-center-on-sheets
    private static async Task PutGameCenterOnSheets(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var gc = Input<GameCenter>(parseResult);
        var secrets = parseResult.GetValue(GoogleSecretsOpt) ?? throw new Exception($"{GoogleSecretsOpt.Name} is required");
        var dataStore = parseResult.GetValue(GoogleDataStoreOpt) ?? throw new Exception($"{GoogleDataStoreOpt.Name} is required");
        var spreadsheetId = parseResult.GetValue(SpreadsheetId);

        await LogEx.StdLog(
            log,
            nameof(PutGameCenterOnSheets),
            log => GoogleTasks.PutGameCenterOnSheets(gc, spreadsheetId, secrets, dataStore, log));
    }

    // update-game-center-from-sheets
    private static async Task UpdateGameCenterFromSheets(ParseResult parseResult, CancellationToken token)
    {
        var log = CreateLog(parseResult);
        var gc = Input<GameCenter>(parseResult);
        var secrets = parseResult.GetValue(GoogleSecretsOpt) ?? throw new Exception($"{GoogleSecretsOpt.Name} is required");
        var dataStore = parseResult.GetValue(GoogleDataStoreOpt) ?? throw new Exception($"{GoogleDataStoreOpt.Name} is required");
        var spreadsheetId = parseResult.GetValue(SpreadsheetId) ?? throw new Exception($"{SpreadsheetId.Name} is required");

        await LogEx.StdLog(
            log,
            nameof(UpdateGameCenterFromSheets),
            log => GoogleTasks.UpdateGameCenterFromSheets(gc, spreadsheetId, secrets, dataStore, log));

        Output(parseResult, gc);
    }
}