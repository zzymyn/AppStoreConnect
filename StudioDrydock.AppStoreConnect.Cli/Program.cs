﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
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
var platformOption = new Option<AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterPlatform?>("--platform");
var appStoreStateOption = new Option<AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterAppStoreState?>("--state");
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
var iapStateOption = new Option<AppStoreClient.Apps_inAppPurchasesV2_getToManyRelatedFilterState?>("--state");
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

// get-app-events --appId=xxx
var getEventsCommand = new Command("get-app-events", "Get information about specific app events");
getEventsCommand.AddOption(appIdOption);
getEventsCommand.AddOption(outputOption);
getEventsCommand.SetHandler(GetAppEvents);
rootCommand.AddCommand(getEventsCommand);

// set-app-events --appId=xxx --input=file.json
var setEventsCommand = new Command("set-app-events", "Update information about specific app events. The input format matches the output of get-events.");
setEventsCommand.AddOption(appIdOption);
setEventsCommand.AddOption(inputOption);
setEventsCommand.AddOption(outputOption);
setEventsCommand.SetHandler(SetAppEvents);
rootCommand.AddCommand(setEventsCommand);

// get-game-center --appId=xxx
var getGameCenterCommand = new Command("get-game-center", "Get information about specific app Game Center");
getGameCenterCommand.AddOption(appIdOption);
getGameCenterCommand.AddOption(outputOption);
getGameCenterCommand.SetHandler(GetGameCenter);
rootCommand.AddCommand(getGameCenterCommand);

// set-game-center --appId=xxx --input=file.json
var setGameCenterCommand = new Command("set-game-center", "Update information about specific app Game Center. The input format matches the output of get-game-center.");
setGameCenterCommand.AddOption(appIdOption);
setGameCenterCommand.AddOption(inputOption);
setGameCenterCommand.AddOption(outputOption);
setGameCenterCommand.SetHandler(SetGameCenter);
rootCommand.AddCommand(setGameCenterCommand);

await rootCommand.InvokeAsync(args);

AppStoreClient CreateClient(InvocationContext context)
{
	if (context.ParseResult.GetValueForOption(verboseOption))
		Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));

	// Read config.json
	var configFile = context.ParseResult.GetValueForOption(configOption);
	var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile.FullName)) ?? throw new Exception($"Failed to parse {configFile}");
	string configDirectory = configFile.Directory.FullName;

	var keyFile = Path.Combine(configDirectory, config.keyPath);
	var keyData = File.ReadAllText(keyFile);

	IAppStoreClientTokenMaker tokenMaker;

	if (config.isUser)
	{
		tokenMaker = AppStoreClientTokenMakerFactory.CreateUser(keyData, config.keyId);
	}
	else
	{
		tokenMaker = AppStoreClientTokenMakerFactory.CreateTeam(keyData, config.keyId, config.issuerId);
	}

	return new AppStoreClient(tokenMaker);
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

	var result = JsonSerializer.Deserialize<T>(text) ?? throw new Exception("Failed to deserialize input");
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

	var response = await api.Apps_getCollection();
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

	var info = await api.Apps_getInstance(appId);
	var appInfo = new AppInfo(info.data);

	var versions = new List<AppVersion>();

	var response = await api.Apps_appStoreVersions_getToManyRelated(appId,
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

		var localizationResponse = await api.AppStoreVersions_appStoreVersionLocalizations_getToManyRelated(version.id);
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
			var ssetsResponse = await api.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelated(loc.id);

			foreach (var ssetResponse in ssetsResponse.data)
			{
				var sset = new ScreenshotSet(ssetResponse);
				ssets.Add(sset);

				var ss = new List<Screenshot>();
				var ssResponse = await api.AppScreenshotSets_appScreenshots_getToManyRelated(ssetResponse.id, include: default);

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
			var apsetsResponse = await api.AppStoreVersionLocalizations_appPreviewSets_getToManyRelated(loc.id);

			foreach (var apsetResponse in apsetsResponse.data)
			{
				var apset = new AppPreviewSet(apsetResponse);
				apsets.Add(apset);
				var apResponse = await api.AppPreviewSets_appPreviews_getToManyRelated(apsetResponse.id, include: default);

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
				var response = await api.AppStoreVersions_createInstance(version.CreateCreateRequest(appId));
				version.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.AppStoreVersions_updateInstance(version.id, version.CreateUpdateRequest());
				version.UpdateWithResponse(response.data);
			}

			foreach (var localization in version.localizations)
			{
				if (string.IsNullOrEmpty(localization.id))
				{
					var response = await api.AppStoreVersionLocalizations_createInstance(localization.CreateCreateRequest(version.id));
					localization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.AppStoreVersionLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest());
					localization.UpdateWithResponse(response.data);
				}

				if (localization.screenshotSets != null && localization.screenshotSets.Length > 0)
				{
					// delete sets that no longer exist:
					{
						var ssSetsResponse = await api.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelated(localization.id);

						foreach (var ssSetResponse in ssSetsResponse.data)
						{
							var ssSet = localization.screenshotSets.FirstOrDefault(a => a.id == ssSetResponse.id);

							if (ssSet == null)
							{
								await api.AppScreenshotSets_deleteInstance(ssSetResponse.id);
							}
						}
					}

					foreach (var ssSet in localization.screenshotSets)
					{
						if (string.IsNullOrEmpty(ssSet.id))
						{
							var response = await api.AppScreenshotSets_createInstance(ssSet.CreateCreateRequest(localization.id));
							ssSet.UpdateWithResponse(response.data);
						}

						if (ssSet.screenshots != null && ssSet.screenshots.Length > 0)
						{
							// delete screenshots that are no longer in the set:
							{
								var ssSetResponse = await api.AppScreenshotSets_appScreenshots_getToManyRelated(ssSet.id, include: default);

								foreach (var ssResponse in ssSetResponse.data)
								{
									var ss = ssSet.screenshots.FirstOrDefault(a => a.id == ssResponse.id);

									if (ss == null)
									{
										await api.AppScreenshots_deleteInstance(ssResponse.id);
									}
								}
							}

							foreach (var ss in ssSet.screenshots)
							{
								if (string.IsNullOrEmpty(ss.id))
								{
									var fi = ad.GetFileByName(ss.fileName, out var fileHash);
									var response = await api.AppScreenshots_createInstance(ss.CreateCreateRequest(ssSet.id, (int)fi.Length, fi.Name));
									ss.UpdateWithResponse(response.data);

									await UploadFile(api, fi, response.data.attributes.uploadOperations);

									response = await api.AppScreenshots_updateInstance(ss.id, ss.CreateUploadCompleteRequest(fileHash));
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

						await api.AppScreenshotSets_appScreenshots_replaceToManyRelationship(ssSet.id, ssSet.CreateUpdateRequest());
					}
				}

				if (localization.appPreviewSets != null && localization.appPreviewSets.Length > 0)
				{
					// delete sets that no longer exist:
					{
						var apSetsResponse = await api.AppStoreVersionLocalizations_appPreviewSets_getToManyRelated(localization.id);

						foreach (var apSetResponse in apSetsResponse.data)
						{
							var apSet = localization.appPreviewSets.FirstOrDefault(a => a.id == apSetResponse.id);

							if (apSet == null)
							{
								await api.AppPreviewSets_deleteInstance(apSetResponse.id);
							}
						}
					}

					foreach (var apSet in localization.appPreviewSets)
					{
						if (string.IsNullOrEmpty(apSet.id))
						{
							var response = await api.AppPreviewSets_createInstance(apSet.CreateCreateRequest(localization.id));
							apSet.UpdateWithResponse(response.data);
						}

						if (apSet.appPreviews != null && apSet.appPreviews.Length > 0)
						{
							// delete app previews that are no longer in the set:
							{
								var apSetResponse = await api.AppPreviewSets_appPreviews_getToManyRelated(apSet.id, include: default);

								foreach (var apResponse in apSetResponse.data)
								{
									var ap = apSet.appPreviews.FirstOrDefault(a => a.id == apResponse.id);

									if (ap == null)
									{
										await api.AppPreviews_deleteInstance(apResponse.id);
									}
								}
							}

							foreach (var ap in apSet.appPreviews)
							{
								if (string.IsNullOrEmpty(ap.id))
								{
									var fi = ad.GetFileByName(ap.fileName, out var fileHash);
									var previewFrameTimeCode = ap.previewFrameTimeCode;

									var response = await api.AppPreviews_createInstance(ap.CreateCreateRequest(apSet.id, (int)fi.Length, fi.Name));
									ap.UpdateWithResponse(response.data);

									await UploadFile(api, fi, response.data.attributes.uploadOperations);

									response = await api.AppPreviews_updateInstance(ap.id, ap.CreateUploadCompleteRequest(fileHash));
									ap.UpdateWithResponse(response.data);

									// API doesn't give us these back:
									ap.sourceFileChecksum = fileHash;
									ap.previewFrameTimeCode = previewFrameTimeCode;
								}
								else if (!string.IsNullOrEmpty(ap.sourceFileChecksum))
								{
									var response = await api.AppPreviews_updateInstance(ap.id, ap.CreateUpdateRequest());
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

						await api.AppPreviewSets_appPreviews_replaceToManyRelationship(apSet.id, apSet.CreateUpdateRequest());
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

	var response = await api.Apps_inAppPurchasesV2_getToManyRelated(appId,
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
		var localizationResponse = await api.InAppPurchasesV2_inAppPurchaseLocalizations_getToManyRelated(iap.id);
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
				var response = await api.InAppPurchasesV2_createInstance(iap.CreateCreateRequest(appId));
				iap.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.InAppPurchasesV2_updateInstance(iap.id, iap.CreateUpdateRequest());
				iap.UpdateWithResponse(response.data);
			}

			foreach (var localization in iap.localizations)
			{
				if (string.IsNullOrEmpty(localization.id))
				{
					var response = await api.InAppPurchaseLocalizations_createInstance(localization.CreateCreateRequest(iap.id));
					localization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.InAppPurchaseLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest());
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

// get-app-events
async Task GetAppEvents(InvocationContext context)
{
	var api = CreateClient(context);
	var appId = context.ParseResult.GetValueForOption(appIdOption);

	var response = await api.Apps_appEvents_getToManyRelated(appId);

	var events = new List<Event>();

	events.AddRange(response.data.Select(x => new Event(x)));

	while (response.links.next != null)
	{
		response = await api.GetNextPage(response);
		events.AddRange(response.data.Select(x => new Event(x)));
	}

	foreach (var ev in events)
	{
		var evLocalizations = new List<EventLocalization>();
		var localizationResponse = await api.AppEvents_localizations_getToManyRelated(ev.id);
		evLocalizations.AddRange(localizationResponse.data.Select(x => new EventLocalization(x)));

		while (localizationResponse.links.next != null)
		{
			localizationResponse = await api.GetNextPage(localizationResponse);
			evLocalizations.AddRange(localizationResponse.data.Select(x => new EventLocalization(x)));
		}

		ev.localizations = evLocalizations.OrderBy(a => a.locale).ToArray();
	}

	Output(context, new EventList() { events = events.OrderBy(a => a.id).ToArray() });
}

// set-app-events
async Task SetAppEvents(InvocationContext context)
{
	var api = CreateClient(context);
	var appId = context.ParseResult.GetValueForOption(appIdOption);
	var events = Input<EventList>(context);

	try
	{
		foreach (var ev in events.events)
		{
			// TODO: make this a cli switch:
			switch (ev.eventState)
			{
				case EventState.WAITING_FOR_REVIEW:
				case EventState.IN_REVIEW:
				case EventState.ACCEPTED:
				case EventState.APPROVED:
				case EventState.PUBLISHED:
				case EventState.PAST:
				case EventState.ARCHIVED:
					continue;
			}

			if (string.IsNullOrEmpty(ev.id))
			{
				var response = await api.AppEvents_createInstance(ev.CreateCreateRequest(appId));
				ev.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.AppEvents_updateInstance(ev.id, ev.CreateUpdateRequest());
				ev.UpdateWithResponse(response.data);
			}

			foreach (var localization in ev.localizations)
			{
				if (string.IsNullOrEmpty(localization.id))
				{
					var response = await api.AppEventLocalizations_createInstance(localization.CreateCreateRequest(ev.id));
					localization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.AppEventLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest());
					localization.UpdateWithResponse(response.data);
				}
			}
		}
	}
	finally
	{
		Output(context, events);
	}
}

// get-game-center
async Task GetGameCenter(InvocationContext context)
{
	var api = CreateClient(context);
	var appId = context.ParseResult.GetValueForOption(appIdOption);

	var detail = await api.Apps_gameCenterDetail_getToOneRelated(appId);
	var gameCenterDetail = new GameCenterDetail(detail.data);

	GameCenterGroup gameCenterGroup = null;

	var achievementReleasesMapTask = Task.Run(() => GetAchievementReleasesMap(api, detail));
	var leaderboardReleasesMapTask = Task.Run(() => GetLeaderboardReleasesMap(api, detail));
	var leaderboardSetReleasesMapTask = Task.Run(() => GetLeaderboardSetReleasesMap(api, detail));
	var achievementReleasesMap = await achievementReleasesMapTask;
	var leaderboardReleasesMap = await leaderboardReleasesMapTask;
	var leaderboardSetReleasesMap = await leaderboardSetReleasesMapTask;

	var gameCenterAchievements = new List<GameCenterAchievement>();
	var gameCenterLeaderboardSets = new List<GameCenterLeaderboardSet>();
	var gameCenterLeaderboards = new List<GameCenterLeaderboard>();

	var group = await api.GameCenterDetails_gameCenterGroup_getToOneRelated(detail.data.id);
	if (group.data != null)
	{
		gameCenterGroup = new(group.data);

		await Task.WhenAll(
			Task.Run(async () => {
				// find achievements:
				// need limit: 200 because this endpoint doesn't paginate (boo):
				var achievements = await api.GameCenterGroups_gameCenterAchievements_getToManyRelated(
					group.data.id,
					limit: 200,
					include: new[] { AppStoreClient.GameCenterGroups_gameCenterAchievements_getToManyRelatedInclude.localizations } );
				AddGameCenterAchievements(gameCenterAchievements, achievements, achievementReleasesMap);
			}),
			Task.Run(async () => {
				// find leaderboards:
				var leaderboards = await api.GameCenterGroups_gameCenterLeaderboards_getToManyRelated(
					group.data.id,
					include: new[] { AppStoreClient.GameCenterGroups_gameCenterLeaderboards_getToManyRelatedInclude.gameCenterLeaderboardSets },
					fieldsGameCenterLeaderboardSets: Array.Empty<AppStoreClient.GameCenterGroups_gameCenterLeaderboards_getToManyRelatedFieldsGameCenterLeaderboardSets>());
				AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);

				while (leaderboards.links.next != null)
				{
					leaderboards = await api.GetNextPage(leaderboards);
					AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);
				}
			}),
			Task.Run(async () => {
				// find leaderboard sets:
				// need limit: 200 because this endpoint doesn't paginate (boo):
				var leaderboardSets = await api.GameCenterGroups_gameCenterLeaderboardSets_getToManyRelated(
					group.data.id,
					limit: 200);
				AddGameCenterLeaderboardSets(gameCenterLeaderboardSets, leaderboardSets, leaderboardSetReleasesMap);
			})
		);
	}
	else
	{
		// non-group:

		await Task.WhenAll(
			Task.Run(async () =>
			{
				// find achievements:
				// need limit: 200 because this endpoint doesn't paginate (boo):
				var achievements = await api.GameCenterDetails_gameCenterAchievements_getToManyRelated(
				detail.data.id,
				limit: 200);
				AddGameCenterAchievements(gameCenterAchievements, achievements, achievementReleasesMap);
			}),
			Task.Run(async () =>
			{
				// find leaderboards:
				var leaderboards = await api.GameCenterDetails_gameCenterLeaderboards_getToManyRelated(
				detail.data.id,
				include: new[] { AppStoreClient.GameCenterDetails_gameCenterLeaderboards_getToManyRelatedInclude.gameCenterLeaderboardSets },
				fieldsGameCenterLeaderboardSets: Array.Empty<AppStoreClient.GameCenterDetails_gameCenterLeaderboards_getToManyRelatedFieldsGameCenterLeaderboardSets>());
				AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);

				while (leaderboards.links.next != null)
				{
					leaderboards = await api.GetNextPage(leaderboards);
					AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);
				}
			}),
			Task.Run(async () =>
			{
				// find leaderboard sets:
				// need limit: 200 because this endpoint doesn't paginate (boo):
				var leaderboardSets = await api.GameCenterDetails_gameCenterLeaderboardSets_getToManyRelated(
				detail.data.id,
				limit: 200);
				AddGameCenterLeaderboardSets(gameCenterLeaderboardSets, leaderboardSets, leaderboardSetReleasesMap);
			})
		);
	}

	var locTasks = new List<Task>();

	foreach (var ach in gameCenterAchievements)
	{
		locTasks.Add(Task.Run(async () =>
		{
			var locs = new List<GameCenterAchievementLocalization>();

			var locResp = await api.GameCenterAchievements_localizations_getToManyRelated(
				ach.id,
				include: new[] { AppStoreClient.GameCenterAchievements_localizations_getToManyRelatedInclude.gameCenterAchievementImage });
			AddGameCenterAchievementLocalizations(locs, locResp);

			while (locResp.links.next != null)
			{
				locResp = await api.GetNextPage(locResp);
				AddGameCenterAchievementLocalizations(locs, locResp);
			}

			ach.localizations = locs.OrderBy(a => a.locale).ToArray();
		}));
	}

	foreach (var lb in gameCenterLeaderboards)
	{
		locTasks.Add(Task.Run(async () =>
		{
			var locs = new List<GameCenterLeaderboardLocalization>();

			var locResp = await api.GameCenterLeaderboards_localizations_getToManyRelated(
				lb.id,
				include: new[] { AppStoreClient.GameCenterLeaderboards_localizations_getToManyRelatedInclude.gameCenterLeaderboardImage });
			AddGameCenterLeaderboardLocalizations(locs, locResp);

			while (locResp.links.next != null)
			{
				locResp = await api.GetNextPage(locResp);
				AddGameCenterLeaderboardLocalizations(locs, locResp);
			}

			lb.localizations = locs.OrderBy(a => a.locale).ToArray();
		}));
	}

	foreach (var lbs in gameCenterLeaderboardSets)
	{
		locTasks.Add(Task.Run(async () =>
		{
			var locs = new List<GameCenterLeaderboardSetLocalization>();

			var locResp = await api.GameCenterLeaderboardSets_localizations_getToManyRelated(
				lbs.id,
				include: new[] { AppStoreClient.GameCenterLeaderboardSets_localizations_getToManyRelatedInclude.gameCenterLeaderboardSetImage });
			AddGameCenterLeaderboardSetLocalizations(locs, locResp);

			while (locResp.links.next != null)
			{
				locResp = await api.GetNextPage(locResp);
				AddGameCenterLeaderboardSetLocalizations(locs, locResp);
			}

			lbs.localizations = locs.OrderBy(a => a.locale).ToArray();
		}));
	}

	await Task.WhenAll(locTasks.ToArray());

	Output(context, new GameCenter()
	{
		detail = gameCenterDetail,
		group = gameCenterGroup,
		achievements = gameCenterAchievements.ToArray(),
		leaderboards = gameCenterLeaderboards.ToArray(),
		leaderboardSets = gameCenterLeaderboardSets.ToArray(),
	});
}

async Task SetGameCenter(InvocationContext context)
{
	var api = CreateClient(context);
	var appId = context.ParseResult.GetValueForOption(appIdOption);
	var gc = Input<GameCenter>(context);

	try
	{
		var detailId = gc.detail?.id;
		var groupId = gc.group?.id;

		foreach (var ach in gc.achievements)
		{
			// TODO: make this a cli switch:
			if (ach.live == true)
				continue;

			if (string.IsNullOrEmpty(ach.id))
			{
				var response = await api.GameCenterAchievements_createInstance(ach.CreateCreateRequest(detailId, groupId));
				ach.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.GameCenterAchievements_updateInstance(ach.id, ach.CreateUpdateRequest());
				ach.UpdateWithResponse(response.data);
			}

			foreach (var achLocalization in ach.localizations)
			{
				if (string.IsNullOrEmpty(achLocalization.id))
				{
					var response = await api.GameCenterAchievementLocalizations_createInstance(achLocalization.CreateCreateRequest(ach.id));
					achLocalization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.GameCenterAchievementLocalizations_updateInstance(achLocalization.id, achLocalization.CreateUpdateRequest());
					achLocalization.UpdateWithResponse(response.data);
				}
			}
		}

		foreach (var lb in gc.leaderboards)
		{
			// TODO: make this a cli switch:
			if (lb.live == true)
				continue;

			if (string.IsNullOrEmpty(lb.id))
			{
				var response = await api.GameCenterLeaderboards_createInstance(lb.CreateCreateRequest(detailId, groupId));
				lb.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.GameCenterLeaderboards_updateInstance(lb.id, lb.CreateUpdateRequest());
				lb.UpdateWithResponse(response.data);
			}

			foreach (var lbLocalization in lb.localizations)
			{
				if (string.IsNullOrEmpty(lbLocalization.id))
				{
					var response = await api.GameCenterLeaderboardLocalizations_createInstance(lbLocalization.CreateCreateRequest(lb.id));
					lbLocalization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.GameCenterLeaderboardLocalizations_updateInstance(lbLocalization.id, lbLocalization.CreateUpdateRequest());
					lbLocalization.UpdateWithResponse(response.data);
				}
			}
		}

		var postLbsetActions = new List<Func<Task>>();
		foreach (var lbs in gc.leaderboardSets)
		{
			// TODO: make this a cli switch:
			if (lbs.live == true)
				continue;

			if (string.IsNullOrEmpty(lbs.id))
			{
				var response = await api.GameCenterLeaderboardSets_createInstance(lbs.CreateCreateRequest(detailId, groupId));
				lbs.UpdateWithResponse(response.data);
			}
			else
			{
				var response = await api.GameCenterLeaderboardSets_updateInstance(lbs.id, lbs.CreateUpdateRequest());
				lbs.UpdateWithResponse(response.data);
			}

			foreach (var lbsLocalization in lbs.localizations)
			{
				if (string.IsNullOrEmpty(lbsLocalization.id))
				{
					var response = await api.GameCenterLeaderboardSetLocalizations_createInstance(lbsLocalization.CreateCreateRequest(lbs.id));
					lbsLocalization.UpdateWithResponse(response.data);
				}
				else
				{
					var response = await api.GameCenterLeaderboardSetLocalizations_updateInstance(lbsLocalization.id, lbsLocalization.CreateUpdateRequest());
					lbsLocalization.UpdateWithResponse(response.data);
				}
			}

			// add new leaderboards to leaderboard set (removal and reordering is handled later):
			var newLbIds = gc.leaderboards
				.Where(x => x.leaderboardSets?.Contains(lbs.id) == true)
				.Select(x => x.id)
				.ToArray();

			// limit 200 because this endpoint doesn't paginate (boo):
			var currentLbIdsReq = await api.GameCenterLeaderboardSets_gameCenterLeaderboards_getToManyRelationship(lbs.id, limit: 200);
			var currentLbIds = currentLbIdsReq.data.Select(x => x.id).ToArray();

			var createLbIds = newLbIds.Except(currentLbIds).ToArray();
			var deleteLbIds = currentLbIds.Except(newLbIds).ToArray();

			if (createLbIds.Length > 0)
			{
				await api.GameCenterLeaderboardSets_gameCenterLeaderboards_createToManyRelationship(lbs.id, new()
				{
					data = createLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
					{
						id = x,
					}).ToArray()
				});
			}

			if (deleteLbIds.Length > 0)
			{
				postLbsetActions.Add(async () =>
				{
					await api.GameCenterLeaderboardSets_gameCenterLeaderboards_deleteToManyRelationship(lbs.id, new()
					{
						data = deleteLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
						{
							id = x,
						}).ToArray()
					});
				});
			}

			if (!Enumerable.SequenceEqual(newLbIds, currentLbIds))
			{
				postLbsetActions.Add(async () =>
				{
					await api.GameCenterLeaderboardSets_gameCenterLeaderboards_replaceToManyRelationship(lbs.id, new()
					{
						data = newLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
						{
							id = x,
						}).ToArray()
					});
				});
			}
		}

		foreach (var a in postLbsetActions)
		{
			await a();
		}

		// update order of leaderboard sets:
		if (gc.leaderboardSets != null)
		{
			if (groupId != null)
			{
				var req = new AppStoreClient.GameCenterGroupGameCenterLeaderboardSetsLinkagesRequest
				{
					data = gc.leaderboardSets.Select(x => new AppStoreClient.GameCenterGroupGameCenterLeaderboardSetsLinkagesRequest.Data()
					{
						id = x.id,
					}).ToArray()
				};

				await api.GameCenterGroups_gameCenterLeaderboardSets_replaceToManyRelationship(groupId, req);
			}
			else
			{
				var req = new AppStoreClient.GameCenterDetailGameCenterLeaderboardSetsLinkagesRequest
				{
					data = gc.leaderboardSets.Select(x => new AppStoreClient.GameCenterDetailGameCenterLeaderboardSetsLinkagesRequest.Data()
					{
						id = x.id,
					}).ToArray()
				};

				await api.GameCenterDetails_gameCenterLeaderboardSets_replaceToManyRelationship(detailId, req);
			}
		}
	}
	finally
	{
		Output(context, gc);
	}
}

static async Task<Dictionary<string, AppStoreClient.GameCenterAchievementRelease>> GetAchievementReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
{
	var achievementReleasesMap = new Dictionary<string, AppStoreClient.GameCenterAchievementRelease>();
	var achievementReleases = await api.GameCenterDetails_achievementReleases_getToManyRelated(
		detail.data.id,
		include: new[] { AppStoreClient.GameCenterDetails_achievementReleases_getToManyRelatedInclude.gameCenterAchievement },
		fieldsGameCenterAchievements: Array.Empty<AppStoreClient.GameCenterDetails_achievementReleases_getToManyRelatedFieldsGameCenterAchievements>());
	foreach (var data in achievementReleases.data)
	{
		achievementReleasesMap.Add(data.relationships.gameCenterAchievement.data.id, data);
	}
	while (achievementReleases.links.next != null)
	{
		achievementReleases = await api.GetNextPage(achievementReleases);
		foreach (var data in achievementReleases.data)
		{
			achievementReleasesMap.Add(data.relationships.gameCenterAchievement.data.id, data);
		}
	}

	return achievementReleasesMap;
}

static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease>> GetLeaderboardReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
{
	var leaderboardReleasesMap = new Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease>();
	var leaderboardReleases = await api.GameCenterDetails_leaderboardReleases_getToManyRelated(
		detail.data.id,
		include: new[] { AppStoreClient.GameCenterDetails_leaderboardReleases_getToManyRelatedInclude.gameCenterLeaderboard },
		fieldsGameCenterLeaderboards: Array.Empty<AppStoreClient.GameCenterDetails_leaderboardReleases_getToManyRelatedFieldsGameCenterLeaderboards>());
	foreach (var data in leaderboardReleases.data)
	{
		leaderboardReleasesMap.Add(data.relationships.gameCenterLeaderboard.data.id, data);
	}
	while (leaderboardReleases.links.next != null)
	{
		leaderboardReleases = await api.GetNextPage(leaderboardReleases);
		foreach (var data in leaderboardReleases.data)
		{
			leaderboardReleasesMap.Add(data.relationships.gameCenterLeaderboard.data.id, data);
		}
	}

	return leaderboardReleasesMap;
}

static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease>> GetLeaderboardSetReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
{
	var leaderboardSetReleasesMap = new Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease>();
	var leaderboardSetReleases = await api.GameCenterDetails_leaderboardSetReleases_getToManyRelated(
		detail.data.id,
		include: new[] { AppStoreClient.GameCenterDetails_leaderboardSetReleases_getToManyRelatedInclude.gameCenterLeaderboardSet },
		fieldsGameCenterLeaderboardSets: Array.Empty<AppStoreClient.GameCenterDetails_leaderboardSetReleases_getToManyRelatedFieldsGameCenterLeaderboardSets>());
	foreach (var data in leaderboardSetReleases.data)
	{
		leaderboardSetReleasesMap.Add(data.relationships.gameCenterLeaderboardSet.data.id, data);
	}
	while (leaderboardSetReleases.links.next != null)
	{
		leaderboardSetReleases = await api.GetNextPage(leaderboardSetReleases);
		foreach (var data in leaderboardSetReleases.data)
		{
			leaderboardSetReleasesMap.Add(data.relationships.gameCenterLeaderboardSet.data.id, data);
		}
	}

	return leaderboardSetReleasesMap;
}

static void AddGameCenterAchievements(List<GameCenterAchievement> list, AppStoreClient.GameCenterAchievementsResponse resp, Dictionary<string, AppStoreClient.GameCenterAchievementRelease> achievementReleasesMap)
{
	foreach (var data in resp.data)
	{
		var gameCenterAchievement = new GameCenterAchievement(data);
		list.Add(gameCenterAchievement);

		if (achievementReleasesMap.TryGetValue(data.id, out var achievementRelease))
		{
			gameCenterAchievement.live = achievementRelease.attributes.live;
		}
	}
}

static void AddGameCenterLeaderboards(List<GameCenterLeaderboard> list, AppStoreClient.GameCenterLeaderboardsResponse resp, Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease> leaderboardReleasesMap)
{
	foreach (var data in resp.data)
	{
		var gameCenterLeaderboard = new GameCenterLeaderboard(data);
		list.Add(gameCenterLeaderboard);

		if (data.relationships?.gameCenterLeaderboardSets?.data?.Length > 0)
		{
			gameCenterLeaderboard.leaderboardSets = data.relationships.gameCenterLeaderboardSets.data.Select(x => x.id).ToArray();
		}

		if (leaderboardReleasesMap.TryGetValue(data.id, out var leaderboardRelease))
		{
			gameCenterLeaderboard.live = leaderboardRelease.attributes.live;
		}
	}
}

static void AddGameCenterLeaderboardSets(List<GameCenterLeaderboardSet> list, AppStoreClient.GameCenterLeaderboardSetsResponse resp, Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease> leaderboardSetReleasesMap)
{
	foreach (var data in resp.data)
	{
		var gameCenterLeaderboardSet = new GameCenterLeaderboardSet(data);
		list.Add(gameCenterLeaderboardSet);
		if (leaderboardSetReleasesMap.TryGetValue(data.id, out var leaderboardSetRelease))
		{
			gameCenterLeaderboardSet.live = leaderboardSetRelease.attributes.live;
		}
	}
}

static void AddGameCenterAchievementLocalizations(List<GameCenterAchievementLocalization> list, AppStoreClient.GameCenterAchievementLocalizationsResponse resp)
{
	foreach (var data in resp.data)
	{
		var gameCenterAchievementLocalization = new GameCenterAchievementLocalization(data);

		list.Add(gameCenterAchievementLocalization);

		if (data?.relationships?.gameCenterAchievementImage?.data != null)
		{
			var image = FindIncluded<AppStoreClient.GameCenterAchievementImage>(resp.included, data.relationships.gameCenterAchievementImage.data.id);
			if (image != null)
			{
				gameCenterAchievementLocalization.image = new GameCenterAchievementImage(image);
			}
		}
	}
}

static void AddGameCenterLeaderboardLocalizations(List<GameCenterLeaderboardLocalization> list, AppStoreClient.GameCenterLeaderboardLocalizationsResponse resp)
{
	foreach (var data in resp.data)
	{
		var gameCenterLeaderboardLocalization = new GameCenterLeaderboardLocalization(data);

		list.Add(gameCenterLeaderboardLocalization);

		if (data?.relationships?.gameCenterLeaderboardImage?.data != null)
		{
			var image = FindIncluded<AppStoreClient.GameCenterLeaderboardImage>(resp.included, data.relationships.gameCenterLeaderboardImage.data.id);
			if (image != null)
			{
				gameCenterLeaderboardLocalization.image = new GameCenterLeaderboardImage(image);
			}
		}
	}
}

static void AddGameCenterLeaderboardSetLocalizations(List<GameCenterLeaderboardSetLocalization> list, AppStoreClient.GameCenterLeaderboardSetLocalizationsResponse resp)
{
	foreach (var data in resp.data)
	{
		var gameCenterLeaderboardSetLocalization = new GameCenterLeaderboardSetLocalization(data);

		list.Add(gameCenterLeaderboardSetLocalization);

		if (data?.relationships?.gameCenterLeaderboardSetImage?.data != null)
		{
			var image = FindIncluded<AppStoreClient.GameCenterLeaderboardSetImage>(resp.included, data.relationships.gameCenterLeaderboardSetImage.data.id);
			if (image != null)
			{
				gameCenterLeaderboardSetLocalization.image = new GameCenterLeaderboardSetImage(image);
			}
		}
	}
}

static T FindIncluded<T>(object[] included, string id)
{
	foreach (var obj in included)
	{
		if (obj is JsonElement je && je.ValueKind == JsonValueKind.Object && je.GetProperty("id").GetString() == id)
		{
			return JsonSerializer.Deserialize<T>(je);
		}
	}

	return default;
}

static async Task UploadFile(AppStoreClient api, FileInfo fi, IReadOnlyList<AppStoreClient.UploadOperation> ops)
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
