using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using StudioDrydock.AppStoreConnect.Api;
using StudioDrydock.AppStoreConnect.Core;
using StudioDrydock.AppStoreConnect.Lib;
using StudioDrydock.AppStoreConnect.Model.Files;

namespace StudioDrydock.AppStoreConnect.Cli;

internal static class Program
{
    private const string Description =
        @"Demonstration of AppStoreConnect. To set up authorization, you will need a config.json\n" +
        @"(by default in ~/.config/AppStoreConnect.json) with the following structure:\n" +
        @"\n" +
        @"  {\n" +
        @"    ""keyId"": ""xxxxxxxxxx"",\n" +
        @"    ""keyPath"": ""AppStoreConnect_xxxxxxxxxx.p8"",\n" +
        @"    ""issuerId"": ""xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx""\n" +
        @"  }\n" +
        @"\n" +
        @"You can obtain these values, and the key file, from the Keys section in \n" +
        @"https://appstoreconnect.apple.com/access/api";

    // --config=<config.json>
    private static readonly Option<FileInfo> ConfigOpt = new("--config",
        getDefaultValue: () => new FileInfo(Environment.ExpandEnvironmentVariables("%USERPROFILE%/.config/AppStoreConnect.json")))
    {
        Description = "JSON configuration file for authorization",
    };

    // --verbose/-v
    private static readonly Option<bool> VerboseOpt = new(["-v", "--verbose"])
    {
        Description = "Enable trace logging of HTTP requests"
    };

    // --output/-o
    private static readonly Option<FileInfo> OutputOpt = new(["-o", "--output"])
    {
        Description = "Specify file to write output to, defaults to stdout",
    };

    // --input/-i
    private static readonly Option<FileInfo> InputOpt = new(["-i", "--input"])
    {
        Description = "Specify file to read input from, defaults to stdin",
    };

    private static readonly Option<string> AppIdOpt = new("--appId")
    {
        Description = "Application ID of the app to get data for.",
        IsRequired = true,
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

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var rootCommand = new RootCommand(Description);

        rootCommand.AddOption(ConfigOpt);
        rootCommand.AddOption(VerboseOpt);

        // get-apps
        var getAppsCommand = new Command("get-apps", "Get list of all apps");
        getAppsCommand.AddOption(OutputOpt);
        getAppsCommand.SetHandler(GetApps);
        rootCommand.AddCommand(getAppsCommand);

        // get-app-versions --appId=xxx [--platform=xxx] [--state=xxx]
        var getAppVersionsCommand = new Command("get-app-versions", "Get information about specific app versions");
        getAppVersionsCommand.AddOption(AppIdOpt);
        getAppVersionsCommand.AddOption(PlatformOpt);
        getAppVersionsCommand.AddOption(AppStoreStateOpt);
        getAppVersionsCommand.AddOption(VersionLimitOpt);
        getAppVersionsCommand.AddOption(OutputOpt);
        getAppVersionsCommand.SetHandler(GetAppVersions);
        rootCommand.AddCommand(getAppVersionsCommand);

        // set-app-versions --input=file.json
        var setAppVersionsCommand = new Command("set-app-versions", "Update localizations for specific app versions. The input format matches the output of get-app-versions.");
        setAppVersionsCommand.AddOption(AppIdOpt);
        setAppVersionsCommand.AddOption(InputOpt);
        setAppVersionsCommand.AddOption(OutputOpt);
        setAppVersionsCommand.SetHandler(SetAppVersions);
        rootCommand.AddCommand(setAppVersionsCommand);

        // get-app-iaps --appId=xxx
        var getAppIapsCommand = new Command("get-app-iaps", "Get information about specific app in-app purchases");
        getAppIapsCommand.AddOption(AppIdOpt);
        getAppIapsCommand.AddOption(IapStateOpt);
        getAppIapsCommand.AddOption(OutputOpt);
        getAppIapsCommand.SetHandler(GetAppIaps);
        rootCommand.AddCommand(getAppIapsCommand);

        // set-app-iaps --appId=xxx --input=file.json
        var setAppIapsCommand = new Command("set-app-iaps", "Update information about specific app in-app purchases. The input format matches the output of get-app-iaps.");
        setAppIapsCommand.AddOption(AppIdOpt);
        setAppIapsCommand.AddOption(InputOpt);
        setAppIapsCommand.AddOption(OutputOpt);
        setAppIapsCommand.SetHandler(SetAppIaps);
        rootCommand.AddCommand(setAppIapsCommand);

        // get-app-events --appId=xxx
        var getEventsCommand = new Command("get-app-events", "Get information about specific app events");
        getEventsCommand.AddOption(AppIdOpt);
        getEventsCommand.AddOption(OutputOpt);
        getEventsCommand.SetHandler(GetAppEvents);
        rootCommand.AddCommand(getEventsCommand);

        // set-app-events --appId=xxx --input=file.json
        var setEventsCommand = new Command("set-app-events", "Update information about specific app events. The input format matches the output of get-events.");
        setEventsCommand.AddOption(AppIdOpt);
        setEventsCommand.AddOption(InputOpt);
        setEventsCommand.AddOption(OutputOpt);
        setEventsCommand.SetHandler(SetAppEvents);
        rootCommand.AddCommand(setEventsCommand);

        // get-game-center --appId=xxx
        var getGameCenterCommand = new Command("get-game-center", "Get information about specific app Game Center");
        getGameCenterCommand.AddOption(AppIdOpt);
        getGameCenterCommand.AddOption(OutputOpt);
        getGameCenterCommand.SetHandler(GetGameCenter);
        rootCommand.AddCommand(getGameCenterCommand);

        // set-game-center --appId=xxx --input=file.json
        var setGameCenterCommand = new Command("set-game-center", "Update information about specific app Game Center. The input format matches the output of get-game-center.");
        setGameCenterCommand.AddOption(AppIdOpt);
        setGameCenterCommand.AddOption(InputOpt);
        setGameCenterCommand.AddOption(OutputOpt);
        setGameCenterCommand.SetHandler(SetGameCenter);
        rootCommand.AddCommand(setGameCenterCommand);

        await rootCommand.InvokeAsync(args);
    }

    private static AppStoreClient CreateClient(InvocationContext context, out INestedLog log)
    {
        if (context.ParseResult.GetValueForOption(VerboseOpt))
        {
            log = new ConsoleLogger(3);
        }
        else
        {
            log = new ConsoleLogger(0);
        }

        var configFile = context.ParseResult.GetValueForOption(ConfigOpt) ?? throw new Exception("config.json is required");
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

    private static T Input<T>(InvocationContext context)
    {
        var input = context.ParseResult.GetValueForOption(InputOpt);
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

    private static void Output(InvocationContext context, object result)
    {
        var text = JsonSerializer.Serialize(result, JsonSerializerOptions);
        var output = context.ParseResult.GetValueForOption(OutputOpt);
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
    private static async Task GetApps(InvocationContext context)
    {
        var api = CreateClient(context, out var log);

        var appInfoList = await LogEx.StdLog(
            log?.SubPath(nameof(GetApps)),
            log => Tasks.GetAppInfoList(api, log)
        );

        Output(context, appInfoList);
    }

    // get-app-versions
    private static async Task GetAppVersions(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var ad = CreateAssetDatabaseNextToFile(context.ParseResult.GetValueForOption(OutputOpt));

        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var platform = context.ParseResult.GetValueForOption(PlatformOpt);
        var appStoreState = context.ParseResult.GetValueForOption(AppStoreStateOpt);
        var limit = context.ParseResult.GetValueForOption(VersionLimitOpt);

        var app = await LogEx.StdLog(
            log?.SubPath(nameof(GetAppVersions)),
            log => Tasks.GetApp(api, ad, appId, platform, appStoreState, limit, log)
        );

        Output(context, app);
    }

    // set-app-versions
    private static async Task SetAppVersions(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var ad = CreateAssetDatabaseNextToFile(context.ParseResult.GetValueForOption(InputOpt))!;
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var versions = Input<App>(context);

        try
        {
            await LogEx.StdLog(
                log?.SubPath(nameof(SetAppVersions)),
                log => Tasks.PutApp(api, ad, appId, versions, log)
            );
        }
        finally
        {
            Output(context, versions);
        }
    }

    // get-app-iaps
    private static async Task GetAppIaps(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var state = context.ParseResult.GetValueForOption(IapStateOpt);

        var iapList = await LogEx.StdLog(
            log?.SubPath(nameof(GetAppIaps)),
            log => Tasks.GetIaps(api, appId, state, log)
        );

        Output(context, iapList);
    }

    // set-app-iaps
    private static async Task SetAppIaps(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var iaps = Input<IapList>(context);

        try
        {
            await LogEx.StdLog(
                log?.SubPath(nameof(SetAppIaps)),
                log => Tasks.PutIaps(api, appId, iaps, log)
            );
        }
        finally
        {
            Output(context, iaps);
        }
    }

    // get-app-events
    private static async Task GetAppEvents(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;

        var events = await LogEx.StdLog(
            log?.SubPath(nameof(GetAppEvents)),
            log => Tasks.GetEventList(log, api, appId)
        );

        Output(context, events);
    }

    // set-app-events
    private static async Task SetAppEvents(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var events = Input<EventList>(context);

        try
        {
            await LogEx.StdLog(
                log?.SubPath(nameof(SetAppEvents)),
                log => Tasks.PutEventList(api, log, appId, events)
            );
        }
        finally
        {
            Output(context, events);
        }
    }

    // get-game-center
    private static async Task GetGameCenter(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;

        var gc = await LogEx.StdLog(
            log?.SubPath(nameof(GetGameCenter)),
            log => Tasks.GetGameCenter(log, api, appId)
        );

        Output(context, gc);
    }

    private static async Task SetGameCenter(InvocationContext context)
    {
        var api = CreateClient(context, out var log);
        var appId = context.ParseResult.GetValueForOption(AppIdOpt)!;
        var gc = Input<GameCenter>(context);

        try
        {
            await LogEx.StdLog(
                log?.SubPath(nameof(SetGameCenter)),
                log => Tasks.PutGameCenter(api, log, appId, gc)
            );
        }
        finally
        {
            Output(context, gc);
        }
    }
}