using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;
using StudioDrydock.AppStoreConnect.Cli.Models;

var rootCommand = new RootCommand();
rootCommand.Description =
@"Demonstration of AppStoreConnect. To set up authorization, you will need a config.json
(by default in ~/.config/AppStoreConnect.json) with the following structure:

  {
    ""keyId"": ""xxxxxxxxxx"",
    ""keyPath"": ""AppStoreConnect_xxxxxxxxxx.p8"",
    ""issuerId"": ""xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx""
  }

You can obtain these values, and the key file, from the Keys section in 
https://appstoreconnect.apple.com/access/api";

// --config=<config.json>
var configOption = new Option<FileInfo>("--config",
    getDefaultValue: () => new FileInfo(Environment.ExpandEnvironmentVariables("%USERPROFILE%/.config/AppStoreConnect.json")),
    description: "JSON configuration file for authorization");
rootCommand.AddOption(configOption);

// --verbose/-v
var verboseOption = new Option<bool>("--verbose", "Enable trace logging of HTTP requests");
verboseOption.AddAlias("-v");
rootCommand.AddOption(verboseOption);

// --output/-o
var outputOption = new Option<FileInfo>("--output", "Specify file to write output to, defaults to stdout");
outputOption.AddAlias("-o");

// --input/-i
var inputOption = new Option<FileInfo>("--input", "Specify file to read input from, defaults to stdin");
inputOption.AddAlias("-i");

// get-apps
var getAppsCommand = new Command("get-apps", "Get list of all apps");
getAppsCommand.AddOption(outputOption);
getAppsCommand.SetHandler(GetApps);
rootCommand.AddCommand(getAppsCommand);

// get-app-versions --appId=xxx [--platform=xxx] [--state=xxx]
var getAppVersionsCommand = new Command("get-app-versions", "Get information about specific app versions");
var appIdOption = new Option<string>("--appId") { IsRequired = true };
var platformOption = new Option<AppStoreClient.GetAppsAppStoreVersionsFilterPlatform?>("--platform");
var appStoreStateOption = new Option<AppStoreClient.GetAppsAppStoreVersionsFilterAppStoreState?>("--state");
getAppVersionsCommand.AddOption(appIdOption);
getAppVersionsCommand.AddOption(platformOption);
getAppVersionsCommand.AddOption(appStoreStateOption);
getAppVersionsCommand.AddOption(outputOption);
getAppVersionsCommand.SetHandler(GetAppVersions);
rootCommand.AddCommand(getAppVersionsCommand);

// set-app-versions --input=file.json
var setAppVersionsCommand = new Command("set-app-versions", "Update localizations for specific app versions. The input format matches the output of get-app-versions.");
setAppVersionsCommand.AddOption(appIdOption);
setAppVersionsCommand.AddOption(inputOption);
setAppVersionsCommand.AddOption(outputOption);
setAppVersionsCommand.SetHandler(SetAppVersions);
rootCommand.AddCommand(setAppVersionsCommand);

// get-app-iaps --appId=xxx
var getAppIapsCommand = new Command("get-app-iaps", "Get information about specific app in-app purchases");
var iapStateOption = new Option<AppStoreClient.GetAppsInAppPurchasesV2FilterState?>("--state");
getAppIapsCommand.AddOption(appIdOption);
getAppIapsCommand.AddOption(iapStateOption);
getAppIapsCommand.AddOption(outputOption);
getAppIapsCommand.SetHandler(GetAppIaps);
rootCommand.AddCommand(getAppIapsCommand);

// set-app-iaps --appId=xxx --input=file.json
var setAppIapsCommand = new Command("set-app-iaps", "Update information about specific app in-app purchases. The input format matches the output of get-app-iaps.");
setAppIapsCommand.AddOption(appIdOption);
setAppIapsCommand.AddOption(inputOption);
setAppIapsCommand.AddOption(outputOption);
setAppIapsCommand.SetHandler(SetAppIaps);
rootCommand.AddCommand(setAppIapsCommand);

await rootCommand.InvokeAsync(args);

AppStoreClient CreateClient(InvocationContext context)
{
    if (context.ParseResult.GetValueForOption(verboseOption))
        Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));

    // Read config.json
    FileInfo configFile = context.ParseResult.GetValueForOption(configOption);
    var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile.FullName));
    if (config == null)
        throw new Exception($"Failed to parse {configFile}");
    string configDirectory = configFile.Directory.FullName;

    // Create client
    return new AppStoreClient(new StreamReader(Path.Combine(configDirectory, config.keyPath)), config.keyId, config.issuerId);
}

T Input<T>(InvocationContext context)
{
    FileInfo input = context.ParseResult.GetValueForOption(inputOption);
    string text;
    if (input != null)
        text = File.ReadAllText(input.FullName);
    else
    {
        using (var stream = Console.OpenStandardInput())
        using (var reader = new StreamReader(stream))
            text = reader.ReadToEnd();
    }

    var result = JsonSerializer.Deserialize<T>(text);
    if (result == null)
        throw new Exception("Failed to deserialize input");
    return result;
}

void Output(InvocationContext context, object result)
{
    string text = JsonSerializer.Serialize(result, new JsonSerializerOptions()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    });
    FileInfo output = context.ParseResult.GetValueForOption(outputOption);
    if (output == null)
        Console.WriteLine(text);
    else
        File.WriteAllText(output.FullName, text);
}

// Convenience for creating array of one item if the value is defined; otherwise null.
T[] Filter<T>(T? t)
    where T : struct
{
    if (t.HasValue)
        return new T[] { t.Value };
    else
        return null;
}

// get-apps
async Task GetApps(InvocationContext context)
{
    var apps = new List<AppInfo>();

    var api = CreateClient(context);

    var response = await api.GetApps();
    apps.AddRange(response.data.Select(x => new AppInfo(x)));

    while (response.links.next != null)
    {
        response = await api.GetNextPage(response);
        apps.AddRange(response.data.Select(x => new AppInfo(x)));
    }

    Output(context, new AppInfoList { apps = apps.OrderBy(a => a.bundleId).ToArray() });
}

// get-app-versions
async Task GetAppVersions(InvocationContext context)
{
    var wd = context.ParseResult.GetValueForOption(outputOption).Directory;
    var ad = new AssetDatabase(wd);

    var api = CreateClient(context);
    var appId = context.ParseResult.GetValueForOption(appIdOption);
    var platform = context.ParseResult.GetValueForOption(platformOption);
    var appStoreState = context.ParseResult.GetValueForOption(appStoreStateOption);

    var info = await api.GetApps(appId);
    var appInfo = new AppInfo(info.data);

    var versions = new List<AppVersion>();

    var response = await api.GetAppsAppStoreVersions(appId,
        filterAppStoreState: Filter(appStoreState),
        filterPlatform: Filter(platform));

    versions.AddRange(response.data.Select(x => new AppVersion(x)));

    while (response.links.next != null)
    {
        response = await api.GetNextPage(response);
        versions.AddRange(response.data.Select(x => new AppVersion(x)));
    }

    foreach (var version in versions)
    {
        var localizations = new List<AppVersionLocalization>();

        var localizationResponse = await api.GetAppStoreVersionsAppStoreVersionLocalizations(version.id);
        localizations.AddRange(localizationResponse.data.Select(x => new AppVersionLocalization(x)));

        // API bug: pagination doesn't seem to work for this endpoint
        //while (localizationResponse.links.next != null)
        //{
        //    localizationResponse = await api.GetNextPage(localizationResponse);
        //    localizations.AddRange(localizationResponse.data.Select(x => new AppVersionLocalization(x)));
        //}

        foreach (var loc in localizations)
        {
            var ssets = new List<ScreenshotSet>();
            var ssetsResponse = await api.GetAppStoreVersionLocalizationsAppScreenshotSets(loc.id);

            foreach (var ssetResponse in ssetsResponse.data)
            {
                var sset = new ScreenshotSet(ssetResponse);
                ssets.Add(sset);

                var ss = new List<Screenshot>();
                var ssResponse = await api.GetAppScreenshotSetsAppScreenshots(ssetResponse.id, include: default);

                foreach (var sRespons in ssResponse.data)
                {
                    var s = new Screenshot(sRespons);
                    ss.Add(s);

                    var file = ad.FindFileByHashOrName(s.sourceFileChecksum, s.fileName);

                    if (file.Exists)
                    {
                        s.fileName = file.Name;
                    }
                }

                sset.screenshots = ss.ToArray();
            }

            var apsets = new List<AppPreviewSet>();
            var apsetsResponse = await api.GetAppStoreVersionLocalizationsAppPreviewSets(loc.id);

            foreach (var apsetResponse in apsetsResponse.data)
            {
                var apset = new AppPreviewSet(apsetResponse);
                apsets.Add(apset);
                var apResponse = await api.GetAppPreviewSetsAppPreviews(apsetResponse.id, include: default);

                var aps = new List<AppPreview>();
                foreach (var apRespons in apResponse.data)
                {
                    var ap = new AppPreview(apRespons);
                    aps.Add(ap);

                    var file = ad.FindFileByHashOrName(ap.sourceFileChecksum, ap.fileName);

                    if (file.Exists)
                    {
                        ap.fileName = file.Name;
                    }
                }

                apset.appPreviews = aps.ToArray();
            }

            loc.screenshotSets = ssets.OrderBy(a => a.screenshotDisplayType).ToArray();
            loc.appPreviewSets = apsets.OrderBy(a => a.previewType).ToArray();
        }

        version.localizations = localizations.OrderBy(a => a.locale).ToArray();
    }

    Output(context, new App()
    {
        appInfo = appInfo,
        appVersions = versions.ToArray()
    });
}

// set-app-versions
async Task SetAppVersions(InvocationContext context)
{
    var wd = context.ParseResult.GetValueForOption(outputOption).Directory;
    var ad = new AssetDatabase(wd);

    var api = CreateClient(context);
    var appId = context.ParseResult.GetValueForOption(appIdOption);
    var versions = Input<App>(context);

    try
    {
        foreach (var version in versions.appVersions)
        {
            // TODO: make this a cli switch:
            switch (version.appStoreState)
            {
                case AppStoreState.READY_FOR_SALE:
                case AppStoreState.REPLACED_WITH_NEW_VERSION:
                case AppStoreState.REMOVED_FROM_SALE:
                    continue;
            }

            if (string.IsNullOrEmpty(version.id))
            {
                var response = await api.PostAppStoreVersions(version.CreateCreateRequest(appId));
                version.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.PatchAppStoreVersions(version.id, version.CreateUpdateRequest());
                version.UpdateWithResponse(response.data);
            }

            foreach (var localization in version.localizations)
            {
                if (string.IsNullOrEmpty(localization.id))
                {
                    var response = await api.PostAppStoreVersionLocalizations(localization.CreateCreateRequest(version.id));
                    localization.UpdateWithResponse(response.data);
                }
                else
                {
                    var response = await api.PatchAppStoreVersionLocalizations(localization.id, localization.CreateUpdateRequest());
                    localization.UpdateWithResponse(response.data);
                }

                if (localization.screenshotSets != null && localization.screenshotSets.Length > 0)
                {
                    // delete sets that no longer exist:
                    {
                        var ssSetsResponse = await api.GetAppStoreVersionLocalizationsAppScreenshotSets(localization.id);

                        foreach (var ssSetResponse in ssSetsResponse.data)
                        {
                            var ssSet = localization.screenshotSets.FirstOrDefault(a => a.id == ssSetResponse.id);

                            if (ssSet == null)
                            {
                                await api.DeleteAppScreenshotSets(ssSetResponse.id);
                            }
                        }
                    }

                    foreach (var ssSet in localization.screenshotSets)
                    {
                        if (string.IsNullOrEmpty(ssSet.id))
                        {
                            var response = await api.PostAppScreenshotSets(ssSet.CreateCreateRequest(localization.id));
                            ssSet.UpdateWithResponse(response.data);
                        }

                        if (ssSet.screenshots != null && ssSet.screenshots.Length > 0)
                        {
                            // delete screenshots that are no longer in the set:
                            {
                                var ssSetResponse = await api.GetAppScreenshotSetsAppScreenshots(ssSet.id, include: default);

                                foreach (var ssResponse in ssSetResponse.data)
                                {
                                    var ss = ssSet.screenshots.FirstOrDefault(a => a.id == ssResponse.id);

                                    if (ss == null)
                                    {
                                        await api.DeleteAppScreenshots(ssResponse.id);
                                    }
                                }
                            }

                            foreach (var ss in ssSet.screenshots)
                            {
                                if (string.IsNullOrEmpty(ss.id))
                                {
                                    var fi = ad.GetFileByName(ss.fileName, out var fileHash);
                                    var response = await api.PostAppScreenshots(ss.CreateCreateRequest(ssSet.id, (int)fi.Length, fi.Name));
                                    ss.UpdateWithResponse(response.data);

                                    await UploadFile(api, fi, response.data.attributes.uploadOperations);

                                    response = await api.PatchAppScreenshots(ss.id, ss.CreateUploadCompleteRequest(fileHash));
                                    ss.UpdateWithResponse(response.data);

                                    // API doesn't give us these back:
                                    ss.sourceFileChecksum = fileHash;
                                }

                                {
                                    var file = ad.FindFileByHashOrName(ss.sourceFileChecksum, ss.fileName);

                                    if (file.Exists)
                                    {
                                        ss.fileName = file.Name;
                                    }
                                }
                            }
                        }

                        await api.PatchAppScreenshotSetsAppScreenshots(ssSet.id, ssSet.CreateUpdateRequest());
                    }
                }

                if (localization.appPreviewSets != null && localization.appPreviewSets.Length > 0)
                {
                    // delete sets that no longer exist:
                    {
                        var apSetsResponse = await api.GetAppStoreVersionLocalizationsAppPreviewSets(localization.id);

                        foreach (var apSetResponse in apSetsResponse.data)
                        {
                            var apSet = localization.appPreviewSets.FirstOrDefault(a => a.id == apSetResponse.id);

                            if (apSet == null)
                            {
                                await api.DeleteAppPreviewSets(apSetResponse.id);
                            }
                        }
                    }

                    foreach (var apSet in localization.appPreviewSets)
                    {
                        if (string.IsNullOrEmpty(apSet.id))
                        {
                            var response = await api.PostAppPreviewSets(apSet.CreateCreateRequest(localization.id));
                            apSet.UpdateWithResponse(response.data);
                        }

                        if (apSet.appPreviews != null && apSet.appPreviews.Length > 0)
                        {
                            // delete app previews that are no longer in the set:
                            {
                                var apSetResponse = await api.GetAppPreviewSetsAppPreviews(apSet.id, include: default);

                                foreach (var apResponse in apSetResponse.data)
                                {
                                    var ap = apSet.appPreviews.FirstOrDefault(a => a.id == apResponse.id);

                                    if (ap == null)
                                    {
                                        await api.DeleteAppPreviews(apResponse.id);
                                    }
                                }
                            }

                            foreach (var ap in apSet.appPreviews)
                            {
                                if (string.IsNullOrEmpty(ap.id))
                                {
                                    var fi = ad.GetFileByName(ap.fileName, out var fileHash);
                                    var previewFrameTimeCode = ap.previewFrameTimeCode;

                                    var response = await api.PostAppPreviews(ap.CreateCreateRequest(apSet.id, (int)fi.Length, fi.Name));
                                    ap.UpdateWithResponse(response.data);

                                    await UploadFile(api, fi, response.data.attributes.uploadOperations);

                                    response = await api.PatchAppPreviews(ap.id, ap.CreateUploadCompleteRequest(fileHash));
                                    ap.UpdateWithResponse(response.data);

                                    // API doesn't give us these back:
                                    ap.sourceFileChecksum = fileHash;
                                    ap.previewFrameTimeCode = previewFrameTimeCode;
                                }
                                else if (!string.IsNullOrEmpty(ap.sourceFileChecksum))
                                {
                                    var response = await api.PatchAppPreviews(ap.id, ap.CreateUpdateRequest());
                                    ap.UpdateWithResponse(response.data);
                                }
                                else
                                {
                                    // if sourceFileChecksum is null it means it hasn't finished processing yet and we can't change anything on it
                                }

                                {
                                    var file = ad.FindFileByHashOrName(ap.sourceFileChecksum, ap.fileName);

                                    if (file.Exists)
                                    {
                                        ap.fileName = file.Name;
                                    }
                                }
                            }
                        }

                        await api.PatchAppPreviewSetsAppPreviews(apSet.id, apSet.CreateUpdateRequest());
                    }
                }
            }
        }
    }
    finally
    {
        Output(context, versions);
    }
}

// get-app-iaps
async Task GetAppIaps(InvocationContext context)
{
    var api = CreateClient(context);
    var appId = context.ParseResult.GetValueForOption(appIdOption);
    var state = context.ParseResult.GetValueForOption(iapStateOption);

    var response = await api.GetAppsInAppPurchasesV2(appId,
        filterState: Filter(state));

    var iaps = new List<Iap>();

    iaps.AddRange(response.data.Select(x => new Iap(x)));

    while (response.links.next != null)
    {
        response = await api.GetNextPage(response);
        iaps.AddRange(response.data.Select(x => new Iap(x)));
    }

    foreach (var iap in iaps)
    {
        var iapLocalizations = new List<IapLocalization>();
        var localizationResponse = await api.GetInAppPurchasesInAppPurchaseLocalizations(iap.id);
        iapLocalizations.AddRange(localizationResponse.data.Select(x => new IapLocalization(x)));

        while (localizationResponse.links.next != null)
        {
            localizationResponse = await api.GetNextPage(localizationResponse);
            iapLocalizations.AddRange(localizationResponse.data.Select(x => new IapLocalization(x)));
        }

        iap.localizations = iapLocalizations.OrderBy(a => a.locale).ToArray();
    }

    Output(context, new IapList() { iaps = iaps.OrderBy(a => a.productId).ToArray() });
}

// set-app-iaps
async Task SetAppIaps(InvocationContext context)
{
    var api = CreateClient(context);
    var appId = context.ParseResult.GetValueForOption(appIdOption);
    var iaps = Input<IapList>(context);

    try
    {
        foreach (var iap in iaps.iaps)
        {
            // TODO: make this a cli switch:
            if (iap.state == InAppPurchaseState.APPROVED)
                continue;

            if (string.IsNullOrEmpty(iap.id))
            {
                var response = await api.PostInAppPurchases(iap.CreateCreateRequest(appId));
                iap.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.PatchInAppPurchases(iap.id, iap.CreateUpdateRequest());
                iap.UpdateWithResponse(response.data);
            }

            foreach (var localization in iap.localizations)
            {
                if (string.IsNullOrEmpty(localization.id))
                {
                    var response = await api.PostInAppPurchaseLocalizations(localization.CreateCreateRequest(iap.id));
                    localization.UpdateWithResponse(response.data);
                }
                else
                {
                    var response = await api.PatchInAppPurchaseLocalizations(localization.id, localization.CreateUpdateRequest());
                    localization.UpdateWithResponse(response.data);
                }
            }
        }
    }
    finally
    {
        Output(context, iaps);
    }
}

static async Task UploadFile(AppStoreClient api, FileInfo fi, IReadOnlyList<AppStoreClient.IUploadOperations> ops)
{
    using (var stream = fi.OpenRead())
    {
        foreach (var op in ops)
        {
            var data = new byte[op.length.Value];
            stream.Seek(op.offset.Value, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(data);
            if (bytesRead != data.Length)
                throw new Exception("Failed to read all bytes from file.");
            await api.UploadPortion(op.method, op.url, data, op.requestHeaders.ToDictionary(a => a.name, a => a.value));
        }
    }
}