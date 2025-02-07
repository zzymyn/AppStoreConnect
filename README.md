# AppStoreConnect

This is a .NET implementation of the [App Store Connect API](https://developer.apple.com/documentation/appstoreconnectapi).

The authorization component is lifted directly from https://github.com/dersia/AppStoreConnect. The client APIs are generated from the [App Store Connect OpenAPI specification](https://developer.apple.com/sample-code/app-store-connect/app-store-connect-openapi-specification.zip).

This library has not been extensively tested and should not be used for production changes without first taking extreme care. _Note that misuse or a bug in this library could result in unintended pricing, availability or legal status changes to your apps._

## Repository Overview

The solution contains multiple projects:

- **StudioDrydock.AppStoreConnect.Api**\
  The actual class library that contains the API for interacting with the App Store. This is the only library you need to link against for your own projects.
- **StudioDrydock.AppStoreConnect.ApiGenerator**\
  Command-line app that regenerates the source of `StudioDrydock.AppStoreConnect.Api`. Only required when the OpenAPI specification published by Apple changes (or to fix bugs in the generation).
- **StudioDrydock.AppStoreConnect.Cli**\
  Command-line app that takes arguments and passes them to the tasks in `StudioDrydock.AppStoreConnect.Lib`.
- **StudioDrydock.AppStoreConnect.Lib**\
  Library demonstrating usage of the API, with some basic functionality.

## Authorization

The library requires an API key which you can generate at the [Users and Access](https://appstoreconnect.apple.com/access/api) page on the App Store Connect portal. You will need:

* Issuer ID
* Key ID
* Certificate (.p8 file)

To use this authorization with the sample app, save this information in `~/.config/AppStoreConnect.json`:

```json
{
  "keyId": "xxxxxxxxxx",
  "keyPath": "AppStoreConnect_xxxxxxxxxx.p8",
  "issuerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

For user-specific tokens, set `"isUser": true` and omit `"issuerId"`, for example:

```json
{
  "isUser": true,
  "keyId": "xxxxxxxxxx",
  "keyPath": "AppStoreConnect_xxxxxxxxxx.p8"
}
```

The `keyPath` is relative to the config json file.

## Sample Code

The following sample code lists all apps with their IDs and bundle IDs:

```csharp
using System.IO;
using StudioDrydock.AppStoreConnect.Api;

var tokenMaker = AppStoreClientTokenMakerFactory.CreateTeam(
    File.ReadAllText("path/to/key.p8"),
    "<key-id>",
    "<issuer-id>");
var api = new AppStoreClient(tokenMaker);

var apps = await api.Apps_getCollection();
foreach (var app in apps.data)
{
    Console.WriteLine($"{app.id}: {app.attributes.bundleId}");
}
```

## API Usage

In general, to find the API for a particular endpoint, search `AppStoreClient.g.cs` for 
the endpoint you are looking for (e.g., `/v1/apps`), and this will reveal the corresponding
API method. From there you can find the request and response object types and supported
parameters.

## CLI Usage

The accompanying CLI project is intended mostly as a testbed or demonstration, or as a starting
point for your own projects. The program outputs custom JSON to stderr, or to a file if `--output` is specified. Run with `--help` for additional information.

Currently these commands are supported:

### Get all applications

```
dotnet run -- get-apps
```

Writes summary information about all apps, including their IDs, which are required for other commands (note that an App ID is not its bundle ID).

### Get application versions and localizations

```
dotnet run -- get-app-versions --appId=12345678 --limitVersions 1 --platform=MAC_OS --output=app.json
```

Writes summary and all localized data about specific app versions matching the given criteria. The `--limitVersions` and `--platform` arguments are optional, and limit the set of returned versions (by default the API will return every version that ever existed).

### Update application versions

```
dotnet run -- set-app-versions --appId=<APP_ID> --input=<INPUT_FILE> [--output=<OUTPUT_FILE>]
```

Reads localized data from the given input (in the same format output by `get-app-versions`) and updates
any non-null fields.

For example, to use this to bulk-update translations for an app, use `get-app-versions` to create a file containing the current locale data, including the required IDs, update the translations in-place, then run `set-app-versions`.

### Get in-app purchases

```
dotnet run -- get-app-iaps --appId=<APP_ID> [--state=<STATE>] [--output=<OUTPUT_FILE>]
```

### Update in-app purchases

```
dotnet run -- set-app-iaps --appId=<APP_ID> --input=<INPUT_FILE> [--output=<OUTPUT_FILE>]
```

### Get app events

```
dotnet run -- get-app-events --appId=<APP_ID> [--output=<OUTPUT_FILE>]
```

### Update app events

```
dotnet run -- set-app-events --appId=<APP_ID> --input=<INPUT_FILE> [--output=<OUTPUT_FILE>]
```

### Get Game Center details

```sh
dotnet run -- get-game-center --appId=12345678 --output=gamecenter.json
```

### Update Game Center details

```sh
dotnet run -- set-game-center --appId=12345678 --input=gamecenter.json
```

## Google Sheets Integration

The CLI also supports exporting and updating Game Center data to and from Google Sheets. This requires a Google Cloud Platform project with the Google Sheets API enabled and OAuth 2.0 credentials set up. The credentials JSON file should be downloaded and passed to the CLI as the `--googleClientSecrets` argument.

The data store directory passed in the `--googleDataStore` argument is used to store the Google Sheets API token and credentials.

By default the CLI will look for these in `~/.config/GoogleClientSecrets.json` and `~/.config/GoogleDataStore`, respectively.

The first time you run the CLI with Google Sheets integration, your browser will open and you will be prompted to log in to your Google account and grant the CLI access to your Google Sheets.

### Export Game Center data to Google Sheets

```
dotnet run -- put-game-center-on-sheets --input=<INPUT_FILE> --googleClientSecrets=<SECRETS_FILE> --googleDataStore=<DATA_STORE_DIR> --spreadsheetId=<SPREADSHEET_ID>
```

Creates (or updates if the `--spreadsheetId` argument is provided) a Google Sheets document with the Game Center data from the given input file. Creates sheets for Achievements, and Leaderboards, and updates them with the data from the input file. The input file should be in the same format as the output of `get-game-center`.

### Update Game Center data from Google Sheets

```
dotnet run -- update-game-center-from-sheets --input=<INPUT_FILE> --output=<OUTPUT_FILE> --googleClientSecrets=<SECRETS_FILE> --googleDataStore=<DATA_STORE_DIR> --spreadsheetId=<SPREADSHEET_ID>
```

Updates the Game Center data in the input file with the data from the Google Sheets document, and outputs the updated data to the output file. The input file should be in the same format as the output of `get-game-center`.