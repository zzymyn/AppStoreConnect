using System.Collections.Concurrent;
using System.CommandLine;
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
	private static readonly Option<bool> VerboseOpt = new(new[] { "-v", "--verbose" })
	{
		Description = "Enable trace logging of HTTP requests"
	};

	// --output/-o
	private static readonly Option<FileInfo> OutputOpt = new(new[] { "-o", "--output" })
	{
		Description = "Specify file to write output to, defaults to stdout",
	};

	// --input/-i
	private static readonly Option<FileInfo> InputOpt = new(new[] { "-i", "--input" })
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

	private static async Task Main(string[] args)
	{
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

	private static AppStoreClient CreateClient(InvocationContext context)
	{
		if (context.ParseResult.GetValueForOption(VerboseOpt))
			Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));

		// Read config.json
		var configFile = context.ParseResult.GetValueForOption(ConfigOpt);
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

	private static T Input<T>(InvocationContext context)
	{
		FileInfo input = context.ParseResult.GetValueForOption(InputOpt);
		string text;
		if (input != null)
			text = File.ReadAllText(input.FullName);
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
		string text = JsonSerializer.Serialize(result, new JsonSerializerOptions()
		{
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
			WriteIndented = true
		});
		FileInfo output = context.ParseResult.GetValueForOption(OutputOpt);
		if (output == null)
			Console.WriteLine(text);
		else
			File.WriteAllText(output.FullName, text);
	}

	// Convenience for creating array of one item if the value is defined; otherwise null.
	private static T[] Filter<T>(T? t)
		where T : struct
	{
		if (t.HasValue)
			return new T[] { t.Value };
		else
			return null;
	}

	// get-apps
	private static async Task GetApps(InvocationContext context)
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

		Output(context, new AppInfoList
		{
			apps = apps.OrderBy(a => a.bundleId).ToArray()
		});
	}

	// get-app-versions
	private static async Task GetAppVersions(InvocationContext context)
	{
		var wd = context.ParseResult.GetValueForOption(OutputOpt).Directory;
		var ad = new AssetDatabase(wd);

		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var platform = context.ParseResult.GetValueForOption(PlatformOpt);
		var appStoreState = context.ParseResult.GetValueForOption(AppStoreStateOpt);
		var limit = context.ParseResult.GetValueForOption(VersionLimitOpt);

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

		// TODO: just request the first N versions from the API:
		if (limit.HasValue)
		{
			versions = versions.GroupBy(a => a.platform).Select(a => a.First()).ToList();
		}

		var locTasks = new List<Task>();

		foreach (var version in versions)
		{
			locTasks.Add(Task.Run(async () =>
			{
				var localizations = new List<AppVersionLocalization>();

				// need limit: 200 because this endpoint doesn't paginate (boo):
				var localizationResponse = await api.AppStoreVersions_appStoreVersionLocalizations_getToManyRelated(version.id, limit: 200);
				localizations.AddRange(localizationResponse.data.Select(x => new AppVersionLocalization(x)));

				foreach (var loc in localizations)
				{
					var ssets = new List<ScreenshotSet>();
					var apsets = new List<AppPreviewSet>();

					await Task.WhenAll(
						Task.Run(async () =>
						{
							var ssetsResponse = await api.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelated(
								loc.id,
								include: new[] { AppStoreClient.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelatedInclude.appScreenshots });
							AddScreenshotSets(ad, ssets, ssetsResponse);

							while (ssetsResponse.links.next != null)
							{
								ssetsResponse = await api.GetNextPage(ssetsResponse);
								AddScreenshotSets(ad, ssets, ssetsResponse);
							}
						}),
						Task.Run(async () =>
						{
							var apsetsResponse = await api.AppStoreVersionLocalizations_appPreviewSets_getToManyRelated(
								loc.id,
								include: new[] { AppStoreClient.AppStoreVersionLocalizations_appPreviewSets_getToManyRelatedInclude.appPreviews });
							AddAppPreviewSets(ad, apsets, apsetsResponse);

							while (apsetsResponse.links.next != null)
							{
								apsetsResponse = await api.GetNextPage(apsetsResponse);
								AddAppPreviewSets(ad, apsets, apsetsResponse);
							}
						})
					);

					loc.screenshotSets = ssets.OrderBy(a => a.screenshotDisplayType).ToArray();
					loc.appPreviewSets = apsets.OrderBy(a => a.previewType).ToArray();
				}

				version.localizations = localizations.OrderBy(a => a.locale).ToArray();
			}));
		}

		await Task.WhenAll(locTasks);

		Output(context, new App()
		{
			appInfo = appInfo,
			appVersions = versions.ToArray()
		});
	}

	private static void AddScreenshotSets(AssetDatabase ad, List<ScreenshotSet> list, AppStoreClient.AppScreenshotSetsResponse resp)
	{
		foreach (var ssetResponse in resp.data)
		{
			var sset = new ScreenshotSet(ssetResponse);
			list.Add(sset);

			if (ssetResponse.relationships?.appScreenshots?.data?.Length > 0)
			{
				var ss = new List<Screenshot>();

				foreach (var sRel in ssetResponse.relationships?.appScreenshots?.data)
				{
					var sData = FindIncluded<AppStoreClient.AppScreenshot>(resp.included, sRel.id);

					var s = new Screenshot(sData);
					ss.Add(s);

					var file = ad.FindFileByHashOrName(s.sourceFileChecksum, s.fileName);

					if (file.Exists)
					{
						s.fileName = file.Name;
					}
				}

				sset.screenshots = ss.ToArray();
			}
		}
	}

	private static void AddAppPreviewSets(AssetDatabase ad, List<AppPreviewSet> list, AppStoreClient.AppPreviewSetsResponse resp)
	{
		foreach (var apsetResponse in resp.data)
		{
			var apset = new AppPreviewSet(apsetResponse);
			list.Add(apset);

			if (apsetResponse.relationships?.appPreviews?.data?.Length > 0)
			{
				var aps = new List<AppPreview>();

				foreach (var apRel in apsetResponse.relationships?.appPreviews?.data)
				{
					var apData = FindIncluded<AppStoreClient.AppPreview>(resp.included, apRel.id);

					var ap = new AppPreview(apData);
					aps.Add(ap);

					var file = ad.FindFileByHashOrName(ap.sourceFileChecksum, ap.fileName);

					if (file.Exists)
					{
						ap.fileName = file.Name;
					}
				}

				apset.appPreviews = aps.ToArray();
			}
		}
	}

	// set-app-versions
	private static async Task SetAppVersions(InvocationContext context)
	{
		var wd = context.ParseResult.GetValueForOption(OutputOpt).Directory;
		var ad = new AssetDatabase(wd);

		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var versions = Input<App>(context);

		try
		{
			foreach (var version in versions.appVersions)
			{
				// TODO: make this a cli switch:
				switch (version.appStoreState)
				{
					case AppStoreClient.AppStoreVersion.Attributes.AppStoreState.READY_FOR_SALE:
					case AppStoreClient.AppStoreVersion.Attributes.AppStoreState.REPLACED_WITH_NEW_VERSION:
					case AppStoreClient.AppStoreVersion.Attributes.AppStoreState.REMOVED_FROM_SALE:
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
	private static async Task GetAppIaps(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var state = context.ParseResult.GetValueForOption(IapStateOpt);

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
	private static async Task SetAppIaps(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var iaps = Input<IapList>(context);

		try
		{
			foreach (var iap in iaps.iaps)
			{
				// TODO: make this a cli switch:
				if (iap.state == AppStoreClient.InAppPurchaseV2.Attributes.State.APPROVED)
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
	private static async Task GetAppEvents(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);

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
	private static async Task SetAppEvents(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var events = Input<EventList>(context);

		try
		{
			foreach (var ev in events.events)
			{
				// TODO: make this a cli switch:
				switch (ev.eventState)
				{
					case AppStoreClient.AppEvent.Attributes.EventState.WAITING_FOR_REVIEW:
					case AppStoreClient.AppEvent.Attributes.EventState.IN_REVIEW:
					case AppStoreClient.AppEvent.Attributes.EventState.ACCEPTED:
					case AppStoreClient.AppEvent.Attributes.EventState.APPROVED:
					case AppStoreClient.AppEvent.Attributes.EventState.PUBLISHED:
					case AppStoreClient.AppEvent.Attributes.EventState.PAST:
					case AppStoreClient.AppEvent.Attributes.EventState.ARCHIVED:
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
	private static async Task GetGameCenter(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);

		var detail = await api.Apps_gameCenterDetail_getToOneRelated(appId);
		var gameCenterDetail = new GameCenterDetail(detail.data);

		var achievementReleasesMapTask = Task.Run(() => GetAchievementReleasesMap(api, detail));
		var leaderboardReleasesMapTask = Task.Run(() => GetLeaderboardReleasesMap(api, detail));
		var leaderboardSetReleasesMapTask = Task.Run(() => GetLeaderboardSetReleasesMap(api, detail));
		var achievementReleasesMap = await achievementReleasesMapTask;
		var leaderboardReleasesMap = await leaderboardReleasesMapTask;
		var leaderboardSetReleasesMap = await leaderboardSetReleasesMapTask;

		var gameCenterAchievements = new List<GameCenterAchievement>();
		var gameCenterLeaderboardSets = new List<GameCenterLeaderboardSet>();
		var gameCenterLeaderboards = new List<GameCenterLeaderboard>();

		GameCenterGroup gameCenterGroup = null;

		var group = await api.GameCenterDetails_gameCenterGroup_getToOneRelated(detail.data.id);
		if (group.data != null)
		{
			gameCenterGroup = new(group.data);

			await Task.WhenAll(
				Task.Run(async () =>
				{
					// find achievements:
					// need limit: 200 because this endpoint doesn't paginate (boo):
					var achievements = await api.GameCenterGroups_gameCenterAchievements_getToManyRelated(
						group.data.id,
						limit: 200);
					AddGameCenterAchievements(gameCenterAchievements, achievements, achievementReleasesMap);
				}),
				Task.Run(async () =>
				{
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
				Task.Run(async () =>
				{
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

		await Task.WhenAll(locTasks);

		Output(context, new GameCenter()
		{
			detail = gameCenterDetail,
			group = gameCenterGroup,
			achievements = gameCenterAchievements.ToArray(),
			leaderboards = gameCenterLeaderboards.ToArray(),
			leaderboardSets = gameCenterLeaderboardSets.ToArray(),
		});
	}

	private static async Task SetGameCenter(InvocationContext context)
	{
		var api = CreateClient(context);
		var appId = context.ParseResult.GetValueForOption(AppIdOpt);
		var gc = Input<GameCenter>(context);

		try
		{
			var detailId = gc.detail?.id;
			var groupId = gc.group?.id;

			var tasks = new List<Task>();

			foreach (var ach in gc.achievements)
			{
				tasks.Add(Task.Run(async () =>
				{
					// TODO: make this a cli switch:
					if (ach.live == true)
						return;

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

					var locTasks = new List<Task>();

					foreach (var loc in ach.localizations)
					{
						locTasks.Add(Task.Run(async () =>
						{
							if (string.IsNullOrEmpty(loc.id))
							{
								var response = await api.GameCenterAchievementLocalizations_createInstance(loc.CreateCreateRequest(ach.id));
								loc.UpdateWithResponse(response.data);
							}
							else
							{
								var response = await api.GameCenterAchievementLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest());
								loc.UpdateWithResponse(response.data);
							}
						}));
					}

					await Task.WhenAll(locTasks);
				}));
			}

			foreach (var lb in gc.leaderboards)
			{
				tasks.Add(Task.Run(async () =>
				{
					// TODO: make this a cli switch:
					if (lb.live == true)
						return;

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

					var locTasks = new List<Task>();

					foreach (var loc in lb.localizations)
					{
						locTasks.Add(Task.Run(async () =>
						{
							if (string.IsNullOrEmpty(loc.id))
							{
								var response = await api.GameCenterLeaderboardLocalizations_createInstance(loc.CreateCreateRequest(lb.id));
								loc.UpdateWithResponse(response.data);
							}
							else
							{
								var response = await api.GameCenterLeaderboardLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest());
								loc.UpdateWithResponse(response.data);
							}
						}));
					}

					await Task.WhenAll(locTasks);
				}));
			}

			foreach (var lbs in gc.leaderboardSets)
			{
				tasks.Add(Task.Run(async () =>
				{
					// TODO: make this a cli switch:
					if (lbs.live == true)
						return;

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

					var locTasks = new List<Task>();

					foreach (var loc in lbs.localizations)
					{
						locTasks.Add(Task.Run(async () =>
						{
							if (string.IsNullOrEmpty(loc.id))
							{
								var response = await api.GameCenterLeaderboardSetLocalizations_createInstance(loc.CreateCreateRequest(lbs.id));
								loc.UpdateWithResponse(response.data);
							}
							else
							{
								var response = await api.GameCenterLeaderboardSetLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest());
								loc.UpdateWithResponse(response.data);
							}
						}));
					}

					await Task.WhenAll(locTasks);
				}));
			}

			// we have to wait until everything above is done before we can update the relationships:
			await Task.WhenAll(tasks);

			var postLbsetActions = new List<Func<Task>>();
			foreach (var lbs in gc.leaderboardSets)
			{
				// TODO: make this a cli switch:
				if (lbs.live == true)
					continue;

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

				postLbsetActions.Add(async () =>
				{
					if (deleteLbIds.Length > 0)
					{
						await api.GameCenterLeaderboardSets_gameCenterLeaderboards_deleteToManyRelationship(lbs.id, new()
						{
							data = deleteLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
							{
								id = x,
							}).ToArray()
						});
					}

					if (!newLbIds.SequenceEqual(currentLbIds))
					{
						await api.GameCenterLeaderboardSets_gameCenterLeaderboards_replaceToManyRelationship(lbs.id, new()
						{
							data = newLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
							{
								id = x,
							}).ToArray()
						});
					}
				});
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

	private static async Task<Dictionary<string, AppStoreClient.GameCenterAchievementRelease>> GetAchievementReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
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

	private static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease>> GetLeaderboardReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
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

	private static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease>> GetLeaderboardSetReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail)
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

	private static void AddGameCenterAchievements(List<GameCenterAchievement> list, AppStoreClient.GameCenterAchievementsResponse resp, Dictionary<string, AppStoreClient.GameCenterAchievementRelease> achievementReleasesMap)
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

	private static void AddGameCenterLeaderboards(List<GameCenterLeaderboard> list, AppStoreClient.GameCenterLeaderboardsResponse resp, Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease> leaderboardReleasesMap)
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

	private static void AddGameCenterLeaderboardSets(List<GameCenterLeaderboardSet> list, AppStoreClient.GameCenterLeaderboardSetsResponse resp, Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease> leaderboardSetReleasesMap)
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

	private static void AddGameCenterAchievementLocalizations(List<GameCenterAchievementLocalization> list, AppStoreClient.GameCenterAchievementLocalizationsResponse resp)
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

	private static void AddGameCenterLeaderboardLocalizations(List<GameCenterLeaderboardLocalization> list, AppStoreClient.GameCenterLeaderboardLocalizationsResponse resp)
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

	private static void AddGameCenterLeaderboardSetLocalizations(List<GameCenterLeaderboardSetLocalization> list, AppStoreClient.GameCenterLeaderboardSetLocalizationsResponse resp)
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

	private static T FindIncluded<T>(object[] included, string id)
	{
		foreach (var obj in included)
		{
			if (obj is JsonElement je && je.ValueKind == JsonValueKind.Object && je.GetProperty("id").GetString() == id)
			{
				return je.Deserialize<T>();
			}
		}

		return default;
	}

	private static async Task UploadFile(AppStoreClient api, FileInfo fi, IReadOnlyList<AppStoreClient.UploadOperation> ops)
	{
		using var stream = fi.OpenRead();
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