using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class AppVersion
    {
        public string id { get; set; }
        public AppStoreClient.AppStoreVersion.Attributes.Platform platform { get; set; }
        public string versionString { get; set; }
        
        public AppStoreClient.AppStoreVersion.Attributes.AppStoreState appStoreState { get; set; }
        public string copyright { get; set; }
        
        public AppStoreClient.AppStoreVersion.Attributes.ReleaseType releaseType { get; set; }
        public string earliestReleaseDate { get; set; }
        public bool downloadable { get; set; }
        public string createdDate { get; set; }

        public AppVersionLocalization[] localizations { get; set; }

        public AppVersion()
        { }

        public AppVersion(AppStoreClient.AppStoreVersion data)
        {
            this.id = data.id;
            this.platform = data.attributes.platform.Value;
            this.versionString = data.attributes.versionString;
            this.appStoreState = data.attributes.appStoreState.Value;
            this.copyright = data.attributes.copyright;
            this.releaseType = data.attributes.releaseType.Value;
            this.earliestReleaseDate = data.attributes.earliestReleaseDate;
            this.downloadable = data.attributes.downloadable.Value;
            this.createdDate = data.attributes.createdDate;
        }

        internal void UpdateWithResponse(AppStoreClient.AppStoreVersion data)
        {
            this.id = data.id;
            this.platform = data.attributes.platform.Value;
            this.versionString = data.attributes.versionString;
            this.appStoreState = data.attributes.appStoreState.Value;
            this.copyright = data.attributes.copyright;
            this.releaseType = data.attributes.releaseType.Value;
            this.earliestReleaseDate = data.attributes.earliestReleaseDate;
            this.downloadable = data.attributes.downloadable.Value;
            this.createdDate = data.attributes.createdDate;
        }

        internal AppStoreClient.AppStoreVersionCreateRequest CreateCreateRequest(string appId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        platform = EnumExtensions<AppStoreClient.AppStoreVersionCreateRequest.Data.Attributes.Platform>.Convert(this.platform),
                        versionString = this.versionString,
                        copyright = this.copyright,
                        releaseType = EnumExtensions<AppStoreClient.AppStoreVersionCreateRequest.Data.Attributes.ReleaseType>.Convert(this.releaseType),
                        earliestReleaseDate = this.earliestReleaseDate,
                    },
                    relationships = new()
                    {
                        app = new()
                        {
                            data = new()
                            {
                                id = appId,
                            }
                        }
                    },
                }
            };
        }

        internal AppStoreClient.AppStoreVersionUpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        versionString = this.versionString,
                        copyright = this.copyright,
                        releaseType = EnumExtensions< AppStoreClient.AppStoreVersionUpdateRequest.Data.Attributes.ReleaseType>.Convert(this.releaseType),
                        earliestReleaseDate = this.earliestReleaseDate,
                        downloadable = this.downloadable,
                    },
                }
            };
        }
    }
}