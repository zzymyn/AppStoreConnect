using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using StudioDrydock.AppStoreConnect.Api;
using StudioDrydock.AppStoreConnect.Lib.Files;
using StudioDrydock.AppStoreConnect.Lib.Model;

namespace StudioDrydock.AppStoreConnect.Lib;

public static class GoogleTasks
{
    public static async Task PutGameCenterOnSheets(GameCenter gc, string? spreadsheetId, FileInfo clientSecretsFile, DirectoryInfo dataStoreDir, INestedLog? log)
    {
        var service = await GetService(clientSecretsFile, dataStoreDir, log);

        if (string.IsNullOrEmpty(spreadsheetId))
        {
            log?.Log(LogLevel.Note, "No spreadsheet ID provided. Creating a new spreadsheet...");
            spreadsheetId = await CreateSpreadsheet(log, service);
            var spreadsheetUri = new Uri($"https://docs.google.com/spreadsheets/d/{spreadsheetId}");
            log?.Log(LogLevel.Note, $"Spreadsheet created! ID: {spreadsheetId}");
            log?.Log(LogLevel.Note, $"Link: {spreadsheetUri}");
        }
        else
        {
            log?.Log(LogLevel.Note, $"Using existing spreadsheet: {spreadsheetId}");
        }

        var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();

        await EnsureSheetCreated(service, spreadsheetId, "Achievements", log, spreadsheet);
        await WriteSheetData(CreateAchievementTable(gc), service, spreadsheetId, "Achievements", log);

        await EnsureSheetCreated(service, spreadsheetId, "Leaderboards", log, spreadsheet);
        await WriteSheetData(CreateLeaderboardTable(gc), service, spreadsheetId, "Leaderboards", log);

        await EnsureSheetCreated(service, spreadsheetId, "LeaderboardSets", log, spreadsheet);
        await WriteSheetData(CreateLeaderboardSetTable(gc), service, spreadsheetId, "LeaderboardSets", log);
    }

    public static async Task UpdateGameCenterFromSheets(GameCenter gc, string spreadsheetId, FileInfo clientSecretsFile, DirectoryInfo dataStoreDir, INestedLog? log)
    {
        var service = await GetService(clientSecretsFile, dataStoreDir, log);

        var achievementsData = await service.Spreadsheets.Values.Get(spreadsheetId, "Achievements").ExecuteAsync();
        if (achievementsData.Values != null)
        {
            UpdateAchievementsFromTable(gc, achievementsData.Values, log);
        }

        var leaderboardsData = await service.Spreadsheets.Values.Get(spreadsheetId, "Leaderboards").ExecuteAsync();
        if (leaderboardsData.Values != null)
        {
            UpdateLeaderboardsFromTable(gc, leaderboardsData.Values, log);
        }

        var leaderboardSetsData = await service.Spreadsheets.Values.Get(spreadsheetId, "LeaderboardSets").ExecuteAsync();
        if (leaderboardSetsData.Values != null)
        {
            UpdateLeaderboardSetsFromTable(gc, leaderboardSetsData.Values, log);
        }
    }

    private static async Task<SheetsService> GetService(FileInfo clientSecretsFile, DirectoryInfo dataStoreDir, INestedLog? log)
    {
        string[] scopes = [SheetsService.Scope.DriveFile];

        var secrets = await GoogleClientSecrets.FromFileAsync(clientSecretsFile.FullName);
        var fileDataStore = new FileDataStore(dataStoreDir.FullName, true);

        log?.Log(LogLevel.VerboseNote, "Logging in...");

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets.Secrets,
            scopes,
            "user", // Identifies the user
            CancellationToken.None,
            fileDataStore);

        log?.Log(LogLevel.Note, $"Successfully logged in as {credential.UserId}");

        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "AppStoreConnectTool",
        });
        return service;
    }

    private static async Task<string> CreateSpreadsheet(INestedLog? log, SheetsService service)
    {
        var spreadsheet = new Spreadsheet
        {
            Properties = new()
            {
                Title = "Test Spreadsheet"
            }
        };

        var request = service.Spreadsheets.Create(spreadsheet);
        var response = await request.ExecuteAsync();

        return response.SpreadsheetId;
    }

    private static async Task EnsureSheetCreated(SheetsService service, string spreadsheetId, string sheetName, INestedLog? log, Spreadsheet spreadsheet)
    {
        log?.Log(LogLevel.Note, $"Checking if sheet exists: {sheetName}");
        var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);

        if (sheet == null)
        {
            log?.Log(LogLevel.Note, $"Creating sheet: {sheetName}");

            var addSheetRequest = new Request
            {
                AddSheet = new()
                {
                    Properties = new()
                    {
                        Title = sheetName
                    }
                }
            };
            var batchUpdateAdd = new BatchUpdateSpreadsheetRequest()
            {
                Requests = [addSheetRequest]
            };

            await service.Spreadsheets.BatchUpdate(batchUpdateAdd, spreadsheetId).ExecuteAsync();

            log?.Log(LogLevel.Note, $"Sheet created: {sheetName}");
        }
        else
        {
            log?.Log(LogLevel.Note, $"Sheet already exists, no need to create: {sheetName}");
        }
    }

    private static async Task WriteSheetData(IList<IList<object>> data, SheetsService service, string spreadsheetId, string sheetName, INestedLog? log)
    {
        log?.Log(LogLevel.Note, $"Writing data to {sheetName}...");

        var range = $"{sheetName}"; // overwrite whole sheet

        var valueRange = new ValueRange
        {
            Values = data
        };

        var clearRequest = service.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, range);
        await clearRequest.ExecuteAsync();

        var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await updateRequest.ExecuteAsync();

        log?.Log(LogLevel.Note, $"Data written to {sheetName}");
    }

    private static List<IList<object>> CreateAchievementTable(GameCenter gc)
    {
        var table = new List<IList<object>>();

        var allLocales = gc.achievements
            .Where(a => a.localizations != null).SelectMany(a => a.localizations!)
            .Where(loc => loc.locale != null).Select(loc => loc.locale!)
            .Distinct().OrderBy(a => a).ToList();

        var row0 = new List<object> { "", "", "", "", "", };
        var row1 = new List<object> {
            nameof(GameCenterAchievement.referenceName),
            nameof(GameCenterAchievement.vendorIdentifier),
            nameof(GameCenterAchievement.points),
            nameof(GameCenterAchievement.showBeforeEarned),
            nameof(GameCenterAchievement.repeatable),
        };
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterAchievementLocalization.name));
        }
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterAchievementLocalization.beforeEarnedDescription));
        }
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterAchievementLocalization.afterEarnedDescription));
        }
        table.Add(row0);
        table.Add(row1);

        foreach (var achievement in gc.achievements)
        {
            var row = new List<object>
            {
                achievement.referenceName ?? "",
                achievement.vendorIdentifier ?? "",
                achievement.points?.ToString() ?? "",
                achievement.showBeforeEarned?.ToString() ?? "",
                achievement.repeatable?.ToString() ?? "",
            };
            foreach (var locale in allLocales)
            {
                var loc = achievement.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.name ?? "");
            }
            foreach (var locale in allLocales)
            {
                var loc = achievement.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.beforeEarnedDescription ?? "");
            }
            foreach (var locale in allLocales)
            {
                var loc = achievement.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.afterEarnedDescription ?? "");
            }
            table.Add(row);
        }

        return table;
    }

    private static void UpdateAchievementsFromTable(GameCenter gc, IList<IList<object>> data, INestedLog? log)
    {
        var allLocales = data[0].OfType<string>().Where(s => s != "").ToHashSet();
        var newAchievements = new List<GameCenterAchievement>();

        for (var r = 2; r < data.Count; ++r)
        {
            var vendorIdentifier = ReadDataValue(data, r, nameof(GameCenterAchievement.vendorIdentifier), "");

            if (vendorIdentifier == null)
            {
                log?.Log(LogLevel.Warning, $"Row {r + 1} has no vendorIdentifier, skipping.");
                continue;
            }

            var achievement = gc.achievements.FirstOrDefault(a => a.vendorIdentifier == vendorIdentifier);

            achievement ??= new()
            {
                vendorIdentifier = vendorIdentifier
            };

            newAchievements.Add(achievement);

            achievement.referenceName = ReadDataValue(data, r, nameof(GameCenterAchievement.referenceName), "");
            achievement.points = ParseInt(ReadDataValue(data, r, nameof(GameCenterAchievement.points), ""));
            achievement.showBeforeEarned = ParseBool(ReadDataValue(data, r, nameof(GameCenterAchievement.showBeforeEarned), ""));
            achievement.repeatable = ParseBool(ReadDataValue(data, r, nameof(GameCenterAchievement.repeatable), ""));

            var newLocales = new List<GameCenterAchievementLocalization>();

            foreach (var locale in allLocales)
            {
                var name = ReadDataValue(data, r, nameof(GameCenterAchievementLocalization.name), locale);

                var loc = achievement.localizations?.FirstOrDefault(l => l.locale == locale);
                loc ??= new()
                {
                    locale = locale
                };
                newLocales.Add(loc);

                loc.name = name;
                loc.beforeEarnedDescription = ReadDataValue(data, r, nameof(GameCenterAchievementLocalization.beforeEarnedDescription), locale);
                loc.afterEarnedDescription = ReadDataValue(data, r, nameof(GameCenterAchievementLocalization.afterEarnedDescription), locale);
            }

            achievement.localizations = [.. newLocales.OrderBy(l => l.locale)];
        }

        gc.achievements = newAchievements;
    }

    private static List<IList<object>> CreateLeaderboardTable(GameCenter gc)
    {
        var table = new List<IList<object>>();

        var allLocales = gc.leaderboards
            .Where(a => a.localizations != null).SelectMany(a => a.localizations!)
            .Where(loc => loc.locale != null).Select(loc => loc.locale!)
            .Distinct().OrderBy(a => a).ToList();

        var row0 = new List<object> { "", "", "", "", "", };
        var row1 = new List<object> {
            nameof(GameCenterLeaderboard.referenceName),
            nameof(GameCenterLeaderboard.vendorIdentifier),
            nameof(GameCenterLeaderboard.defaultFormatter),
            nameof(GameCenterLeaderboard.submissionType),
            nameof(GameCenterLeaderboard.scoreSortType),
            nameof(GameCenterLeaderboard.leaderboardSets),
        };
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterLeaderboardLocalization.name));
        }
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterLeaderboardLocalization.formatterOverride));
        }
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterLeaderboardLocalization.formatterSuffix));
        }
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterLeaderboardLocalization.formatterSuffixSingular));
        }
        table.Add(row0);
        table.Add(row1);

        foreach (var leaderboard in gc.leaderboards)
        {
            var row = new List<object>
            {
                leaderboard.referenceName ?? "",
                leaderboard.vendorIdentifier ?? "",
                leaderboard.defaultFormatter?.ToString() ?? "",
                leaderboard.submissionType?.ToString() ?? "",
                leaderboard.scoreSortType?.ToString() ?? "",
            };

            var lbSetsStr = "";
            if (leaderboard.leaderboardSets != null)
            {
                foreach (var lbSetId in leaderboard.leaderboardSets)
                {
                    var lbSet = gc.leaderboardSets.FirstOrDefault(l => l.vendorIdentifier == lbSetId);
                    if (lbSet != null)
                    {
                        if (lbSetsStr != "")
                            lbSetsStr += "\n";
                        lbSetsStr += lbSet.referenceName;
                    }
                }
            }
            row.Add(lbSetsStr);

            foreach (var locale in allLocales)
            {
                var loc = leaderboard.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.name ?? "");
            }
            foreach (var locale in allLocales)
            {
                var loc = leaderboard.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.formatterOverride?.ToString() ?? "");
            }
            foreach (var locale in allLocales)
            {
                var loc = leaderboard.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.formatterSuffix ?? "");
            }
            foreach (var locale in allLocales)
            {
                var loc = leaderboard.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.formatterSuffixSingular ?? "");
            }
            table.Add(row);
        }

        return table;
    }

    private static void UpdateLeaderboardsFromTable(GameCenter gc, IList<IList<object>> data, INestedLog? log)
    {
        var allLocales = data[0].OfType<string>().Where(s => s != "").ToHashSet();
        var newLeaderboards = new List<GameCenterLeaderboard>();

        for (var r = 2; r < data.Count; ++r)
        {
            var vendorIdentifier = ReadDataValue(data, r, nameof(GameCenterLeaderboard.vendorIdentifier), "");

            if (vendorIdentifier == null)
            {
                log?.Log(LogLevel.Warning, $"Row {r + 1} has no vendorIdentifier, skipping.");
                continue;
            }

            var leaderboard = gc.leaderboards.FirstOrDefault(a => a.vendorIdentifier == vendorIdentifier);
            leaderboard ??= new()
            {
                vendorIdentifier = vendorIdentifier
            };
            newLeaderboards.Add(leaderboard);

            leaderboard.referenceName = ReadDataValue(data, r, nameof(GameCenterLeaderboard.referenceName), "");
            leaderboard.defaultFormatter = ParseEnum<AppStoreClient.GameCenterLeaderboard.Attributes.DefaultFormatter>(ReadDataValue(data, r, nameof(GameCenterLeaderboard.defaultFormatter), ""));
            leaderboard.submissionType = ParseEnum<AppStoreClient.GameCenterLeaderboard.Attributes.SubmissionType>(ReadDataValue(data, r, nameof(GameCenterLeaderboard.submissionType), ""));
            leaderboard.scoreSortType = ParseEnum<AppStoreClient.GameCenterLeaderboard.Attributes.ScoreSortType>(ReadDataValue(data, r, nameof(GameCenterLeaderboard.scoreSortType), ""));

            var newLeaderboardSets = new List<string>();
            var lbSetsStr = ReadDataValue(data, r, nameof(GameCenterLeaderboard.leaderboardSets), "");
            if (!string.IsNullOrEmpty(lbSetsStr))
            {
                var lbSetVIds = lbSetsStr.Split('\n');
                foreach (var lbSetVId in lbSetVIds)
                {
                    var lbSet = gc.leaderboardSets.FirstOrDefault(l => l.vendorIdentifier == lbSetVId);
                    if (lbSet != null)
                    {
                        newLeaderboardSets.Add(lbSet.id!);
                    }
                }
            }
            leaderboard.leaderboardSets = [.. newLeaderboardSets];

            var newLocales = new List<GameCenterLeaderboardLocalization>();
            foreach (var locale in allLocales)
            {
                var name = ReadDataValue(data, r, nameof(GameCenterLeaderboardLocalization.name), locale);

                var loc = leaderboard.localizations?.FirstOrDefault(l => l.locale == locale);
                loc ??= new()
                {
                    locale = locale
                };

                newLocales.Add(loc);

                loc.name = name;
                loc.formatterOverride = ParseEnum<AppStoreClient.GameCenterLeaderboardLocalization.Attributes.FormatterOverride>(ReadDataValue(data, r, nameof(GameCenterLeaderboardLocalization.formatterOverride), locale));
                loc.formatterSuffix = ReadDataValue(data, r, nameof(GameCenterLeaderboardLocalization.formatterSuffix), locale);
                loc.formatterSuffixSingular = ReadDataValue(data, r, nameof(GameCenterLeaderboardLocalization.formatterSuffixSingular), locale);
            }
            leaderboard.localizations = [.. newLocales.OrderBy(l => l.locale)];
        }
        gc.leaderboards = newLeaderboards;
    }

    private static List<IList<object>> CreateLeaderboardSetTable(GameCenter gc)
    {
        var table = new List<IList<object>>();

        var allLocales = gc.leaderboardSets
            .Where(a => a.localizations != null).SelectMany(a => a.localizations!)
            .Where(loc => loc.locale != null).Select(loc => loc.locale!)
            .Distinct().OrderBy(a => a).ToList();

        var row0 = new List<object> { "", "", };
        var row1 = new List<object> {
            nameof(GameCenterLeaderboardSet.referenceName),
            nameof(GameCenterLeaderboardSet.vendorIdentifier),
        };
        foreach (var locale in allLocales)
        {
            row0.Add(locale);
            row1.Add(nameof(GameCenterLeaderboardSetLocalization.name));
        }
        table.Add(row0);
        table.Add(row1);

        foreach (var leaderboardSet in gc.leaderboardSets)
        {
            var row = new List<object>
            {
                leaderboardSet.referenceName ?? "",
                leaderboardSet.vendorIdentifier ?? "",
            };
            foreach (var locale in allLocales)
            {
                var loc = leaderboardSet.localizations?.FirstOrDefault(l => l.locale == locale);
                row.Add(loc?.name ?? "");
            }
            table.Add(row);
        }

        return table;
    }

    private static void UpdateLeaderboardSetsFromTable(GameCenter gc, IList<IList<object>> data, INestedLog? log)
    {
        var allLocales = data[0].OfType<string>().Where(s => s != "").ToHashSet();
        var newLeaderboardSets = new List<GameCenterLeaderboardSet>();

        for (var r = 2; r < data.Count; ++r)
        {
            var vendorIdentifier = ReadDataValue(data, r, nameof(GameCenterLeaderboardSet.vendorIdentifier), "");

            if (vendorIdentifier == null)
            {
                log?.Log(LogLevel.Warning, $"Row {r + 1} has no vendorIdentifier, skipping.");
                continue;
            }

            var leaderboardSet = gc.leaderboardSets.FirstOrDefault(a => a.vendorIdentifier == vendorIdentifier);
            leaderboardSet ??= new()
            {
                vendorIdentifier = vendorIdentifier
            };
            newLeaderboardSets.Add(leaderboardSet);

            leaderboardSet.referenceName = ReadDataValue(data, r, nameof(GameCenterLeaderboardSet.referenceName), "");

            var newLocales = new List<GameCenterLeaderboardSetLocalization>();
            foreach (var locale in allLocales)
            {
                var name = ReadDataValue(data, r, nameof(GameCenterLeaderboardSetLocalization.name), locale);

                var loc = leaderboardSet.localizations?.FirstOrDefault(l => l.locale == locale);
                loc ??= new()
                {
                    locale = locale
                };

                newLocales.Add(loc);

                loc.name = name;
            }
            leaderboardSet.localizations = [.. newLocales.OrderBy(l => l.locale)];
        }
        gc.leaderboardSets = newLeaderboardSets;
    }

    private static string? ReadDataValue(IList<IList<object>> data, int r, string name, string locale)
    {
        var cMax = Math.Max(Math.Max(data[0].Count, data[1].Count), data[r].Count);
        for (var c = 0; c < cMax; ++c)
        {
            var h0 = data[0]?.ElementAtOrDefault(c)?.ToString() ?? "";
            var h1 = data[1]?.ElementAtOrDefault(c)?.ToString() ?? "";
            var hV = data[r]?.ElementAtOrDefault(c)?.ToString() ?? "";
            if (locale == h0 && name == h1)
            {
                // return null if the cell is empty:
                if (string.IsNullOrEmpty(hV))
                    return null;
                return hV;
            }
        }

        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (value == null)
            return null;

        return int.Parse(value);
    }

    private static bool? ParseBool(string? value)
    {
        if (value == null)
            return null;
        return bool.Parse(value);
    }

    private static T? ParseEnum<T>(string? value)
        where T : struct
    {
        if (value == null)
            return null;
        return Enum.Parse<T>(value);
    }
}
