using System.Collections.Concurrent;
using System.Text.Json;
using StudioDrydock.AppStoreConnect.Api;
using StudioDrydock.AppStoreConnect.Core;
using StudioDrydock.AppStoreConnect.Model;
using StudioDrydock.AppStoreConnect.Model.Files;

namespace StudioDrydock.AppStoreConnect.Lib;

public static class Tasks
{
    public static async Task<AppInfoList> GetAppInfoList(AppStoreClient api, INestedLog? log)
    {
        var apps = new List<AppInfo>();

        var response = await api.Apps_getCollection(log: log);
        apps.AddRange(response.data.Select(x => new AppInfo(x)));

        while (response.links.next != null)
        {
            response = await api.GetNextPage(response, log: log);
            apps.AddRange(response.data.Select(x => new AppInfo(x)));
        }

        return new AppInfoList(
            apps: [.. apps.OrderBy(a => a.bundleId)]
        );
    }

    public static async Task<App> GetApp(AppStoreClient api, AssetDatabase? ad, string appId, AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterPlatform? platform, AppStoreClient.Apps_appStoreVersions_getToManyRelatedFilterAppStoreState? appStoreState, int? limit, INestedLog? log)
    {
        var info = await api.Apps_getInstance(appId, log: log);
        var appInfo = new AppInfo(info.data);

        var versions = new List<AppVersion>();

        var response = await api.Apps_appStoreVersions_getToManyRelated(appId,
            filterAppStoreState: Filter(appStoreState),
            filterPlatform: Filter(platform),
            log: log);
        versions.AddRange(response.data.Select(x => new AppVersion(x)));

        while (response.links.next != null)
        {
            response = await api.GetNextPage(response, log: log);
            versions.AddRange(response.data.Select(x => new AppVersion(x)));
        }

        // TODO: just request the first N versions from the API:
        if (limit.HasValue)
        {
            versions = versions.GroupBy(a => a.platform).Select(a => a.First()).ToList();
        }

        await LogEx.ForEachAsyncLog(versions, log, v => $"{v.platform}/{v.versionString}", async (version, log) =>
        {
            var localizations = new List<AppVersionLocalization>();

            // need limit: 200 because this endpoint doesn't paginate (boo):
            var localizationResponse = await api.AppStoreVersions_appStoreVersionLocalizations_getToManyRelated(
                version.id!,
                limit: 200,
                log: log);
            localizations.AddRange(localizationResponse.data.Select(x => new AppVersionLocalization(x)));

            var locTasks = new List<Task>();

            await LogEx.ForEachAsyncLog(localizations, log, l => $"{l.locale}", async (loc, log) =>
            {
                var ssets = new List<ScreenshotSet>();
                var apsets = new List<AppPreviewSet>();

                var a = Task.Run(async () =>
                {
                    var ssetsResponse = await api.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelated(
                        loc.id!,
                        include: [AppStoreClient.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelatedInclude.appScreenshots],
                        log: log);
                    AddScreenshotSets(ad, ssets, ssetsResponse);

                    while (ssetsResponse.links.next != null)
                    {
                        ssetsResponse = await api.GetNextPage(ssetsResponse, log: log);
                        AddScreenshotSets(ad, ssets, ssetsResponse);
                    }
                });
                var b = Task.Run(async () =>
                {
                    var apsetsResponse = await api.AppStoreVersionLocalizations_appPreviewSets_getToManyRelated(
                        loc.id!,
                        include: [AppStoreClient.AppStoreVersionLocalizations_appPreviewSets_getToManyRelatedInclude.appPreviews],
                        log: log);
                    AddAppPreviewSets(ad, apsets, apsetsResponse);

                    while (apsetsResponse.links.next != null)
                    {
                        apsetsResponse = await api.GetNextPage(apsetsResponse, log: log);
                        AddAppPreviewSets(ad, apsets, apsetsResponse);
                    }
                });

                await Task.WhenAll(a, b);

                loc.screenshotSets = [.. ssets.OrderBy(a => a.screenshotDisplayType)];
                loc.appPreviewSets = [.. apsets.OrderBy(a => a.previewType)];
            });

            version.localizations = [.. localizations.OrderBy(a => a.locale)];
        });

        return new App(
            appId: appId,
            appInfo: appInfo,
            appVersions: [.. versions]);
    }

    private static void AddScreenshotSets(AssetDatabase? ad, List<ScreenshotSet> list, AppStoreClient.AppScreenshotSetsResponse resp)
    {
        foreach (var ssetResponse in resp.data)
        {
            var sset = new ScreenshotSet(ssetResponse);
            list.Add(sset);

            if (ssetResponse?.relationships?.appScreenshots?.data?.Length > 0)
            {
                var ss = new List<Screenshot>();

                foreach (var sRel in ssetResponse.relationships.appScreenshots.data)
                {
                    var sData = FindIncluded<AppStoreClient.AppScreenshot>(resp.included, sRel.id);
                    if (sData != null)
                    {
                        var s = new Screenshot(sData);
                        ss.Add(s);

                        var file = ad?.FindFileByHashOrName(s.sourceFileChecksum, s.fileName);

                        if (file?.Exists == true)
                        {
                            s.fileName = file.Name;
                        }
                    }
                }

                sset.screenshots = [.. ss];
            }
        }
    }

    private static void AddAppPreviewSets(AssetDatabase? ad, List<AppPreviewSet> list, AppStoreClient.AppPreviewSetsResponse resp)
    {
        foreach (var apsetResponse in resp.data)
        {
            var apset = new AppPreviewSet(apsetResponse);
            list.Add(apset);

            if (apsetResponse?.relationships?.appPreviews?.data?.Length > 0)
            {
                var aps = new List<AppPreview>();

                foreach (var apRel in apsetResponse.relationships.appPreviews.data)
                {
                    var apData = FindIncluded<AppStoreClient.AppPreview>(resp.included, apRel.id);
                    if (apData != null)
                    {
                        var ap = new AppPreview(apData);
                        aps.Add(ap);

                        var file = ad?.FindFileByHashOrName(ap.sourceFileChecksum, ap.fileName);

                        if (file?.Exists == true)
                        {
                            ap.fileName = file.Name;
                        }
                    }
                }

                apset.appPreviews = [.. aps];
            }
        }
    }

    public static async Task PutApp(AppStoreClient api, AssetDatabase ad, string appId, App versions, INestedLog? log)
    {
        // TODO: parallelize this
        // TODO: improve logging
        if (appId != versions.appId)
            throw new Exception("appId mismatch");

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
                var response = await api.AppStoreVersions_createInstance(version.CreateCreateRequest(appId), log: log);
                version.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.AppStoreVersions_updateInstance(version.id, version.CreateUpdateRequest(), log: log);
                version.UpdateWithResponse(response.data);
            }

            if (version.localizations != null)
            {
                foreach (var localization in version.localizations)
                {
                    if (string.IsNullOrEmpty(localization.id))
                    {
                        var response = await api.AppStoreVersionLocalizations_createInstance(localization.CreateCreateRequest(version.id!), log: log);
                        localization.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.AppStoreVersionLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest(), log: log);
                        localization.UpdateWithResponse(response.data);
                    }

                    if (localization.screenshotSets != null && localization.screenshotSets.Length > 0)
                    {
                        // delete sets that no longer exist:
                        {
                            var ssSetsResponse = await api.AppStoreVersionLocalizations_appScreenshotSets_getToManyRelated(localization.id!, log: log);

                            foreach (var ssSetResponse in ssSetsResponse.data)
                            {
                                var ssSet = localization.screenshotSets.FirstOrDefault(a => a.id == ssSetResponse.id);

                                if (ssSet == null)
                                {
                                    await api.AppScreenshotSets_deleteInstance(ssSetResponse.id, log: log);
                                }
                            }
                        }

                        foreach (var ssSet in localization.screenshotSets)
                        {
                            if (string.IsNullOrEmpty(ssSet.id))
                            {
                                var response = await api.AppScreenshotSets_createInstance(ssSet.CreateCreateRequest(localization.id!), log: log);
                                ssSet.UpdateWithResponse(response.data);
                            }

                            if (ssSet.screenshots != null && ssSet.screenshots.Length > 0)
                            {
                                // delete screenshots that are no longer in the set:
                                {
                                    var ssSetResponse = await api.AppScreenshotSets_appScreenshots_getToManyRelated(ssSet.id!, include: default, log: log);

                                    foreach (var ssResponse in ssSetResponse.data)
                                    {
                                        var ss = ssSet.screenshots.FirstOrDefault(a => a.id == ssResponse.id);

                                        if (ss == null)
                                        {
                                            await api.AppScreenshots_deleteInstance(ssResponse.id, log: log);
                                        }
                                    }
                                }

                                foreach (var ss in ssSet.screenshots)
                                {
                                    if (string.IsNullOrEmpty(ss.id))
                                    {
                                        if (string.IsNullOrEmpty(ss.fileName))
                                            throw new Exception("fileName is required");

                                        var fi = ad.GetFileByName(ss.fileName, out var fileHash);
                                        var response = await api.AppScreenshots_createInstance(ss.CreateCreateRequest(ssSet.id!, (int)fi.Length, fi.Name), log: log);
                                        ss.UpdateWithResponse(response.data);

                                        await UploadFile(api, fi, response.data.attributes!.uploadOperations!);

                                        response = await api.AppScreenshots_updateInstance(ss.id!, ss.CreateUploadCompleteRequest(fileHash), log: log);
                                        ss.UpdateWithResponse(response.data);

                                        // API doesn't give us these back:
                                        ss.sourceFileChecksum = fileHash;
                                    }

                                    {
                                        var file = ad.FindFileByHashOrName(ss.sourceFileChecksum, ss.fileName);

                                        if (file?.Exists == true)
                                        {
                                            ss.fileName = file.Name;
                                        }
                                    }
                                }
                            }

                            await api.AppScreenshotSets_appScreenshots_replaceToManyRelationship(ssSet.id!, ssSet.CreateUpdateRequest(), log: log);
                        }
                    }

                    if (localization.appPreviewSets != null && localization.appPreviewSets.Length > 0)
                    {
                        // delete sets that no longer exist:
                        {
                            var apSetsResponse = await api.AppStoreVersionLocalizations_appPreviewSets_getToManyRelated(localization.id!, log: log);

                            foreach (var apSetResponse in apSetsResponse.data)
                            {
                                var apSet = localization.appPreviewSets.FirstOrDefault(a => a.id == apSetResponse.id);

                                if (apSet == null)
                                {
                                    await api.AppPreviewSets_deleteInstance(apSetResponse.id, log: log);
                                }
                            }
                        }

                        foreach (var apSet in localization.appPreviewSets)
                        {
                            if (string.IsNullOrEmpty(apSet.id))
                            {
                                var response = await api.AppPreviewSets_createInstance(apSet.CreateCreateRequest(localization.id!), log: log);
                                apSet.UpdateWithResponse(response.data);
                            }

                            if (apSet.appPreviews != null && apSet.appPreviews.Length > 0)
                            {
                                // delete app previews that are no longer in the set:
                                {
                                    var apSetResponse = await api.AppPreviewSets_appPreviews_getToManyRelated(apSet.id!, include: default, log: log);

                                    foreach (var apResponse in apSetResponse.data)
                                    {
                                        var ap = apSet.appPreviews.FirstOrDefault(a => a.id == apResponse.id);

                                        if (ap == null)
                                        {
                                            await api.AppPreviews_deleteInstance(apResponse.id, log: log);
                                        }
                                    }
                                }

                                foreach (var ap in apSet.appPreviews)
                                {
                                    if (string.IsNullOrEmpty(ap.id))
                                    {
                                        if (string.IsNullOrEmpty(ap.fileName))
                                            throw new Exception("fileName is required");

                                        var fi = ad.GetFileByName(ap.fileName, out var fileHash);
                                        var previewFrameTimeCode = ap.previewFrameTimeCode;

                                        var response = await api.AppPreviews_createInstance(ap.CreateCreateRequest(apSet.id!, (int)fi.Length, fi.Name), log: log);
                                        ap.UpdateWithResponse(response.data);

                                        await UploadFile(api, fi, response.data.attributes!.uploadOperations!);

                                        response = await api.AppPreviews_updateInstance(ap.id!, ap.CreateUploadCompleteRequest(fileHash), log: log);
                                        ap.UpdateWithResponse(response.data);

                                        // API doesn't give us these back:
                                        ap.sourceFileChecksum = fileHash;
                                        ap.previewFrameTimeCode = previewFrameTimeCode;
                                    }
                                    else if (!string.IsNullOrEmpty(ap.sourceFileChecksum))
                                    {
                                        var response = await api.AppPreviews_updateInstance(ap.id, ap.CreateUpdateRequest(), log: log);
                                        ap.UpdateWithResponse(response.data);
                                    }
                                    else
                                    {
                                        // if sourceFileChecksum is null it means it hasn't finished # yet and we can't change anything on it
                                    }

                                    {
                                        var file = ad.FindFileByHashOrName(ap.sourceFileChecksum, ap.fileName);

                                        if (file?.Exists == true)
                                        {
                                            ap.fileName = file.Name;
                                        }
                                    }
                                }
                            }

                            await api.AppPreviewSets_appPreviews_replaceToManyRelationship(apSet.id!, apSet.CreateUpdateRequest(), log: log);
                        }
                    }
                }
            }
        }
    }

    public static async Task<IapList> GetIaps(AppStoreClient api, string appId, AppStoreClient.Apps_inAppPurchasesV2_getToManyRelatedFilterState? state, INestedLog? log)
    {
        var iaps = new List<Iap>();

        var response = await api.Apps_inAppPurchasesV2_getToManyRelated(appId,
            filterState: Filter(state),
            log: log);
        iaps.AddRange(response.data.Select(x => new Iap(x)));

        while (response.links.next != null)
        {
            response = await api.GetNextPage(response, log: log);
            iaps.AddRange(response.data.Select(x => new Iap(x)));
        }

        await LogEx.ForEachAsyncLog(iaps, log, i => $"{i.name}", async (iap, log) =>
        {
            var iapLocalizations = new List<IapLocalization>();
            var localizationResponse = await api.InAppPurchasesV2_inAppPurchaseLocalizations_getToManyRelated(iap.id!, log: log);
            iapLocalizations.AddRange(localizationResponse.data.Select(x => new IapLocalization(x)));

            while (localizationResponse.links.next != null)
            {
                localizationResponse = await api.GetNextPage(localizationResponse, log: log);
                iapLocalizations.AddRange(localizationResponse.data.Select(x => new IapLocalization(x)));
            }

            iap.localizations = [.. iapLocalizations.OrderBy(a => a.locale)];
        });

        return new IapList(
            appId: appId,
            iaps: [.. iaps.OrderBy(a => a.productId)]
        );
    }

    public static async Task PutIaps(AppStoreClient api, string appId, IapList iaps, INestedLog? log)
    {
        // TODO: parallelize this
        // TODO: improve logging

        if (appId != iaps.appId)
            throw new Exception("appId mismatch");

        foreach (var iap in iaps.iaps)
        {
            // TODO: make this a cli switch:
            if (iap.state == AppStoreClient.InAppPurchaseV2.Attributes.State.APPROVED)
                continue;

            if (string.IsNullOrEmpty(iap.id))
            {
                var response = await api.InAppPurchasesV2_createInstance(iap.CreateCreateRequest(appId), log: log);
                iap.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.InAppPurchasesV2_updateInstance(iap.id, iap.CreateUpdateRequest(), log: log);
                iap.UpdateWithResponse(response.data);
            }

            if (iap.localizations != null)
            {
                foreach (var localization in iap.localizations)
                {
                    if (string.IsNullOrEmpty(localization.id))
                    {
                        var response = await api.InAppPurchaseLocalizations_createInstance(localization.CreateCreateRequest(iap.id!), log: log);
                        localization.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.InAppPurchaseLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest(), log: log);
                        localization.UpdateWithResponse(response.data);
                    }
                }
            }
        }
    }

    public static async Task<EventList> GetEventList(INestedLog? log, AppStoreClient api, string appId)
    {
        var events = new List<Event>();

        var response = await api.Apps_appEvents_getToManyRelated(appId, log: log);
        events.AddRange(response.data.Select(x => new Event(x)));

        while (response.links.next != null)
        {
            response = await api.GetNextPage(response, log: log);
            events.AddRange(response.data.Select(x => new Event(x)));
        }

        await LogEx.ForEachAsyncLog(events, log, e => $"{e.referenceName}", async (ev, log) =>
        {
            var evLocalizations = new List<EventLocalization>();
            var localizationResponse = await api.AppEvents_localizations_getToManyRelated(ev.id!, log: log);
            evLocalizations.AddRange(localizationResponse.data.Select(x => new EventLocalization(x)));

            while (localizationResponse.links.next != null)
            {
                localizationResponse = await api.GetNextPage(localizationResponse, log: log);
                evLocalizations.AddRange(localizationResponse.data.Select(x => new EventLocalization(x)));
            }

            ev.localizations = [.. evLocalizations.OrderBy(a => a.locale)];
        });

        return new EventList(
            appId: appId,
            events: [.. events.OrderBy(a => a.id)]
        );
    }

    public static async Task PutEventList(AppStoreClient api, INestedLog? log, string appId, EventList events)
    {
        // TODO: parallelize this
        // TODO: improve logging

        if (appId != events.appId)
            throw new Exception("appId mismatch");

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
                var response = await api.AppEvents_createInstance(ev.CreateCreateRequest(appId), log: log);
                ev.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.AppEvents_updateInstance(ev.id, ev.CreateUpdateRequest(), log: log);
                ev.UpdateWithResponse(response.data);
            }

            if (ev.localizations != null)
            {
                foreach (var localization in ev.localizations)
                {
                    if (string.IsNullOrEmpty(localization.id))
                    {
                        var response = await api.AppEventLocalizations_createInstance(localization.CreateCreateRequest(ev.id!), log: log);
                        localization.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.AppEventLocalizations_updateInstance(localization.id, localization.CreateUpdateRequest(), log: log);
                        localization.UpdateWithResponse(response.data);
                    }
                }
            }
        }
    }

    public static async Task<GameCenter> GetGameCenter(INestedLog? log, AppStoreClient api, string appId)
    {
        var detail = await api.Apps_gameCenterDetail_getToOneRelated(appId, log: log);
        var gameCenterDetail = new GameCenterDetail(detail.data);

        var achievementReleasesMapTask = Task.Run(() => GetAchievementReleasesMap(api, detail, log));
        var leaderboardReleasesMapTask = Task.Run(() => GetLeaderboardReleasesMap(api, detail, log));
        var leaderboardSetReleasesMapTask = Task.Run(() => GetLeaderboardSetReleasesMap(api, detail, log));
        var achievementReleasesMap = await achievementReleasesMapTask;
        var leaderboardReleasesMap = await leaderboardReleasesMapTask;
        var leaderboardSetReleasesMap = await leaderboardSetReleasesMapTask;

        var gameCenterAchievements = new List<GameCenterAchievement>();
        var gameCenterLeaderboardSets = new List<GameCenterLeaderboardSet>();
        var gameCenterLeaderboards = new List<GameCenterLeaderboard>();

        GameCenterGroup? gameCenterGroup = null;

        var group = await api.GameCenterDetails_gameCenterGroup_getToOneRelated(detail.data.id, log: log);
        if (group.data != null)
        {
            gameCenterGroup = new(group.data);
        }

        var a = LogEx.StdLog(log?.SubPath("Achievements"), async log =>
        {
            if (group.data != null)
            {
                // find achievements:
                // need limit: 200 because this endpoint doesn't paginate (boo):
                var achievements = await api.GameCenterGroups_gameCenterAchievements_getToManyRelated(
                    group.data.id,
                    limit: 200,
                    log: log);
                AddGameCenterAchievements(gameCenterAchievements, achievements, achievementReleasesMap);
            }
            else
            {
                // find achievements:
                // need limit: 200 because this endpoint doesn't paginate (boo):
                var achievements = await api.GameCenterDetails_gameCenterAchievements_getToManyRelated(
                    detail.data.id,
                    limit: 200,
                    log: log);
                AddGameCenterAchievements(gameCenterAchievements, achievements, achievementReleasesMap);
            }

            await LogEx.ForEachAsyncLog(gameCenterAchievements, log, a => $"{a.referenceName}", async (ach, log) =>
            {
                var locs = new List<GameCenterAchievementLocalization>();

                var locResp = await api.GameCenterAchievements_localizations_getToManyRelated(
                    ach.id!,
                    include: [AppStoreClient.GameCenterAchievements_localizations_getToManyRelatedInclude.gameCenterAchievementImage],
                    log: log);
                AddGameCenterAchievementLocalizations(locs, locResp);

                while (locResp.links.next != null)
                {
                    locResp = await api.GetNextPage(locResp, log: log);
                    AddGameCenterAchievementLocalizations(locs, locResp);
                }

                ach.localizations = [.. locs.OrderBy(a => a.locale)];
            });
        });
        var b = LogEx.StdLog(log?.SubPath("Leaderboards"), async log =>
        {
            if (group.data != null)
            {
                // find leaderboards:
                var leaderboards = await api.GameCenterGroups_gameCenterLeaderboards_getToManyRelated(
                    group.data.id,
                    include: [AppStoreClient.GameCenterGroups_gameCenterLeaderboards_getToManyRelatedInclude.gameCenterLeaderboardSets],
                    fieldsGameCenterLeaderboardSets: [],
                    log: log);
                AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);

                while (leaderboards.links.next != null)
                {
                    leaderboards = await api.GetNextPage(leaderboards, log: log);
                    AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);
                }
            }
            else
            {
                // find leaderboards:
                var leaderboards = await api.GameCenterDetails_gameCenterLeaderboards_getToManyRelated(
                    detail.data.id,
                    include: [AppStoreClient.GameCenterDetails_gameCenterLeaderboards_getToManyRelatedInclude.gameCenterLeaderboardSets],
                    fieldsGameCenterLeaderboardSets: [],
                    log: log);
                AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);

                while (leaderboards.links.next != null)
                {
                    leaderboards = await api.GetNextPage(leaderboards, log: log);
                    AddGameCenterLeaderboards(gameCenterLeaderboards, leaderboards, leaderboardReleasesMap);
                }
            }

            await LogEx.ForEachAsyncLog(gameCenterLeaderboards, log, l => $"{l.referenceName}", async (lb, log) =>
            {
                var locs = new List<GameCenterLeaderboardLocalization>();

                var locResp = await api.GameCenterLeaderboards_localizations_getToManyRelated(
                    lb.id!,
                    include: [AppStoreClient.GameCenterLeaderboards_localizations_getToManyRelatedInclude.gameCenterLeaderboardImage],
                    log: log);
                AddGameCenterLeaderboardLocalizations(locs, locResp);

                while (locResp.links.next != null)
                {
                    locResp = await api.GetNextPage(locResp, log: log);
                    AddGameCenterLeaderboardLocalizations(locs, locResp);
                }

                lb.localizations = [.. locs.OrderBy(a => a.locale)];
            });
        });
        var c = LogEx.StdLog(log?.SubPath("LeaderboardSets"), async log =>
        {
            if (group.data != null)
            {
                // find leaderboard sets:
                // need limit: 200 because this endpoint doesn't paginate (boo):
                var leaderboardSets = await api.GameCenterGroups_gameCenterLeaderboardSets_getToManyRelated(
                    group.data.id,
                    limit: 200,
                    log: log);
                AddGameCenterLeaderboardSets(gameCenterLeaderboardSets, leaderboardSets, leaderboardSetReleasesMap);
            }
            else
            {
                // find leaderboard sets:
                // need limit: 200 because this endpoint doesn't paginate (boo):
                var leaderboardSets = await api.GameCenterDetails_gameCenterLeaderboardSets_getToManyRelated(
                    detail.data.id,
                    limit: 200,
                    log: log);
                AddGameCenterLeaderboardSets(gameCenterLeaderboardSets, leaderboardSets, leaderboardSetReleasesMap);
            }

            await LogEx.ForEachAsyncLog(gameCenterLeaderboardSets, log, s => $"{s.referenceName}", async (lbs, log) =>
            {
                var locs = new List<GameCenterLeaderboardSetLocalization>();

                var locResp = await api.GameCenterLeaderboardSets_localizations_getToManyRelated(
                    lbs.id!,
                    include: [AppStoreClient.GameCenterLeaderboardSets_localizations_getToManyRelatedInclude.gameCenterLeaderboardSetImage],
                    log: log);
                AddGameCenterLeaderboardSetLocalizations(locs, locResp);

                while (locResp.links.next != null)
                {
                    locResp = await api.GetNextPage(locResp, log: log);
                    AddGameCenterLeaderboardSetLocalizations(locs, locResp);
                }

                lbs.localizations = [.. locs.OrderBy(a => a.locale)];
            });
        });

        await Task.WhenAll(a, b, c);

        return new GameCenter(
            appId: appId,
            detail: gameCenterDetail,
            group: gameCenterGroup,
            achievements: [.. gameCenterAchievements],
            leaderboards: [.. gameCenterLeaderboards],
            leaderboardSets: [.. gameCenterLeaderboardSets]
        );
    }

    public static async Task PutGameCenter(AppStoreClient api, INestedLog? log, string appId, GameCenter gc)
    {
        if (appId != gc.appId)
            throw new Exception("appId mismatch");

        var detailId = gc.detail.id!;
        var groupId = gc.group?.id;

        var a = LogEx.ForEachAsyncLog(gc.achievements, log, a => $"{a.referenceName}", async (ach, log) =>
        {
            // TODO: make this a cli switch:
            if (ach.live == true)
                return;

            if (string.IsNullOrEmpty(ach.id))
            {
                var response = await api.GameCenterAchievements_createInstance(ach.CreateCreateRequest(detailId, groupId), log: log);
                ach.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.GameCenterAchievements_updateInstance(ach.id, ach.CreateUpdateRequest(), log: log);
                ach.UpdateWithResponse(response.data);
            }

            if (ach.localizations != null)
            {
                await LogEx.ForEachAsyncLog(ach.localizations, log, l => $"{l.locale}", async (loc, log) =>
                {
                    if (string.IsNullOrEmpty(loc.id))
                    {
                        var response = await api.GameCenterAchievementLocalizations_createInstance(loc.CreateCreateRequest(ach.id!), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.GameCenterAchievementLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest(), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                });
            }
        });
        var b = LogEx.ForEachAsyncLog(gc.leaderboards, log, l => $"{l.referenceName}", async (lb, log) =>
        {
            // TODO: make this a cli switch:
            if (lb.live == true)
                return;

            if (string.IsNullOrEmpty(lb.id))
            {
                var response = await api.GameCenterLeaderboards_createInstance(lb.CreateCreateRequest(detailId, groupId), log: log);
                lb.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.GameCenterLeaderboards_updateInstance(lb.id, lb.CreateUpdateRequest(), log: log);
                lb.UpdateWithResponse(response.data);
            }

            if (lb.localizations != null)
            {
                await LogEx.ForEachAsyncLog(lb.localizations, log, l => $"{l.locale}", async (loc, log) =>
                {
                    if (string.IsNullOrEmpty(loc.id))
                    {
                        var response = await api.GameCenterLeaderboardLocalizations_createInstance(loc.CreateCreateRequest(lb.id!), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.GameCenterLeaderboardLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest(), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                });
            }
        });
        var c = LogEx.ForEachAsyncLog(gc.leaderboardSets, log, s => $"{s.referenceName}", async (lbs, log) =>
        {
            // TODO: make this a cli switch:
            if (lbs.live == true)
                return;

            if (string.IsNullOrEmpty(lbs.id))
            {
                var response = await api.GameCenterLeaderboardSets_createInstance(lbs.CreateCreateRequest(detailId, groupId), log: log);
                lbs.UpdateWithResponse(response.data);
            }
            else
            {
                var response = await api.GameCenterLeaderboardSets_updateInstance(lbs.id, lbs.CreateUpdateRequest(), log: log);
                lbs.UpdateWithResponse(response.data);
            }

            if (lbs.localizations != null)
            {
                await LogEx.ForEachAsyncLog(lbs.localizations, log, l => $"{l.locale}", async (loc, log) =>
                {
                    if (string.IsNullOrEmpty(loc.id))
                    {
                        var response = await api.GameCenterLeaderboardSetLocalizations_createInstance(loc.CreateCreateRequest(lbs.id!), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                    else
                    {
                        var response = await api.GameCenterLeaderboardSetLocalizations_updateInstance(loc.id, loc.CreateUpdateRequest(), log: log);
                        loc.UpdateWithResponse(response.data);
                    }
                });
            }
        });

        await Task.WhenAll(a, b, c);

        var postLbsetActions = new ConcurrentBag<Func<Task>>();

        await LogEx.ForEachAsyncLog(gc.leaderboardSets, log, s => $"{s.referenceName}", async (lbs, log) =>
        {
            // TODO: make this a cli switch:
            if (lbs.live == true)
                return;

            // add new leaderboards to leaderboard set (removal and reordering is handled later):
            var newLbIds = gc.leaderboards
                .Where(x => x.leaderboardSets?.Contains(lbs.id) == true)
                .Select(x => x.id!)
                .ToArray();

            // limit 200 because this endpoint doesn't paginate (boo):
            var currentLbIdsReq = await api.GameCenterLeaderboardSets_gameCenterLeaderboards_getToManyRelationship(
                lbs.id!,
                limit: 200,
                log: log);
            var currentLbIds = currentLbIdsReq.data.Select(x => x.id).ToArray();

            var createLbIds = newLbIds.Except(currentLbIds).ToArray();
            var deleteLbIds = currentLbIds.Except(newLbIds).ToArray();

            if (createLbIds.Length > 0)
            {
                await api.GameCenterLeaderboardSets_gameCenterLeaderboards_createToManyRelationship(lbs.id!, new()
                {
                    data = createLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
                    {
                        id = x,
                    }).ToArray()
                }, log: log);
            }

            postLbsetActions.Add(async () =>
            {
                if (deleteLbIds.Length > 0)
                {
                    await api.GameCenterLeaderboardSets_gameCenterLeaderboards_deleteToManyRelationship(lbs.id!, new()
                    {
                        data = deleteLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
                        {
                            id = x,
                        }).ToArray()
                    }, log: log);
                }

                if (!newLbIds.SequenceEqual(currentLbIds))
                {
                    await api.GameCenterLeaderboardSets_gameCenterLeaderboards_replaceToManyRelationship(lbs.id!, new()
                    {
                        data = newLbIds.Select(x => new AppStoreClient.GameCenterLeaderboardSetGameCenterLeaderboardsLinkagesRequest.Data()
                        {
                            id = x,
                        }).ToArray()
                    }, log: log);
                }
            });
        });

        await LogEx.ForEachAsync(postLbsetActions.ToArray(), a => a());

        // update order of leaderboard sets:
        if (gc.leaderboardSets != null)
        {
            if (groupId != null)
            {
                var req = new AppStoreClient.GameCenterGroupGameCenterLeaderboardSetsLinkagesRequest
                {
                    data = gc.leaderboardSets.Select(x => new AppStoreClient.GameCenterGroupGameCenterLeaderboardSetsLinkagesRequest.Data()
                    {
                        id = x.id!,
                    }).ToArray()
                };

                await api.GameCenterGroups_gameCenterLeaderboardSets_replaceToManyRelationship(groupId, req, log: log);
            }
            else
            {
                var req = new AppStoreClient.GameCenterDetailGameCenterLeaderboardSetsLinkagesRequest
                {
                    data = gc.leaderboardSets.Select(x => new AppStoreClient.GameCenterDetailGameCenterLeaderboardSetsLinkagesRequest.Data()
                    {
                        id = x.id!,
                    }).ToArray()
                };

                await api.GameCenterDetails_gameCenterLeaderboardSets_replaceToManyRelationship(detailId, req, log: log);
            }
        }
    }

    private static async Task<Dictionary<string, AppStoreClient.GameCenterAchievementRelease>> GetAchievementReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail, INestedLog? log)
    {
        var achievementReleasesMap = new Dictionary<string, AppStoreClient.GameCenterAchievementRelease>();
        var achievementReleases = await api.GameCenterDetails_achievementReleases_getToManyRelated(
            detail.data.id,
            include: [AppStoreClient.GameCenterDetails_achievementReleases_getToManyRelatedInclude.gameCenterAchievement],
            fieldsGameCenterAchievements: [],
            log: log);
        foreach (var data in achievementReleases.data)
        {
            achievementReleasesMap.Add(data.relationships!.gameCenterAchievement!.data!.id, data);
        }
        while (achievementReleases.links.next != null)
        {
            achievementReleases = await api.GetNextPage(achievementReleases, log: log);
            foreach (var data in achievementReleases.data)
            {
                achievementReleasesMap.Add(data.relationships!.gameCenterAchievement!.data!.id, data);
            }
        }

        return achievementReleasesMap;
    }

    private static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease>> GetLeaderboardReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail, INestedLog? log)
    {
        var leaderboardReleasesMap = new Dictionary<string, AppStoreClient.GameCenterLeaderboardRelease>();
        var leaderboardReleases = await api.GameCenterDetails_leaderboardReleases_getToManyRelated(
            detail.data.id,
            include: [AppStoreClient.GameCenterDetails_leaderboardReleases_getToManyRelatedInclude.gameCenterLeaderboard],
            fieldsGameCenterLeaderboards: [],
            log: log);
        foreach (var data in leaderboardReleases.data)
        {
            leaderboardReleasesMap.Add(data.relationships!.gameCenterLeaderboard!.data!.id, data);
        }
        while (leaderboardReleases.links.next != null)
        {
            leaderboardReleases = await api.GetNextPage(leaderboardReleases, log: log);
            foreach (var data in leaderboardReleases.data)
            {
                leaderboardReleasesMap.Add(data.relationships!.gameCenterLeaderboard!.data!.id, data);
            }
        }

        return leaderboardReleasesMap;
    }

    private static async Task<Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease>> GetLeaderboardSetReleasesMap(AppStoreClient api, AppStoreClient.GameCenterDetailResponse detail, INestedLog? log)
    {
        var leaderboardSetReleasesMap = new Dictionary<string, AppStoreClient.GameCenterLeaderboardSetRelease>();
        var leaderboardSetReleases = await api.GameCenterDetails_leaderboardSetReleases_getToManyRelated(
            detail.data.id,
            include: [AppStoreClient.GameCenterDetails_leaderboardSetReleases_getToManyRelatedInclude.gameCenterLeaderboardSet],
            fieldsGameCenterLeaderboardSets: [],
            log: log);
        foreach (var data in leaderboardSetReleases.data)
        {
            leaderboardSetReleasesMap.Add(data.relationships!.gameCenterLeaderboardSet!.data!.id, data);
        }
        while (leaderboardSetReleases.links.next != null)
        {
            leaderboardSetReleases = await api.GetNextPage(leaderboardSetReleases, log: log);
            foreach (var data in leaderboardSetReleases.data)
            {
                leaderboardSetReleasesMap.Add(data.relationships!.gameCenterLeaderboardSet!.data!.id, data);
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
                gameCenterAchievement.live = achievementRelease.attributes?.live;
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
                gameCenterLeaderboard.live = leaderboardRelease.attributes?.live;
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
                gameCenterLeaderboardSet.live = leaderboardSetRelease.attributes?.live;
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

    private static T[]? Filter<T>(T? t)
        where T : struct
    {
        if (t.HasValue)
            return [t.Value];
        else
            return null;
    }

    private static T? FindIncluded<T>(object[]? included, string id)
    {
        if (included == null)
            return default;

        foreach (var obj in included)
        {
            if (obj is JsonElement je && je.ValueKind == JsonValueKind.Object && je.GetProperty("id").GetString() == id)
            {
                return je.Deserialize<T>();
            }
        }

        return default;
    }

    private static async Task UploadFile(AppStoreClient api, FileInfo fi, IReadOnlyList<AppStoreClient.UploadOperation> ops, INestedLog? log = null)
    {
        using var stream = fi.OpenRead();
        foreach (var op in ops)
        {
            var data = new byte[op.length!.Value];
            stream.Seek(op.offset!.Value, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(data);
            if (bytesRead != data.Length)
                throw new Exception("Failed to read all bytes from file.");
            await api.UploadPortion(op.method!, op.url!, data, op.requestHeaders!.ToDictionary(a => a.name!, a => a.value!), log);
        }
    }
}
