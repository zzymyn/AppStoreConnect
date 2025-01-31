using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class ScreenshotSet
    {
        public string id { get; set; }
        public AppStoreClient.AppScreenshotSet.Attributes.ScreenshotDisplayType screenshotDisplayType { get; set; }
        public Screenshot[] screenshots { get; set; }

        public ScreenshotSet()
        {
        }

        public ScreenshotSet(AppStoreClient.AppScreenshotSet data)
        {
            this.id = data.id;
            this.screenshotDisplayType = data.attributes.screenshotDisplayType.Value;
        }

        internal void UpdateWithResponse(AppStoreClient.AppScreenshotSet data)
        {
            this.id = data.id;
            this.screenshotDisplayType = data.attributes.screenshotDisplayType.Value;
        }

        internal AppStoreClient.AppScreenshotSetCreateRequest CreateCreateRequest(string id)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        screenshotDisplayType = EnumExtensions<AppStoreClient.AppScreenshotSetCreateRequest.Data.Attributes.ScreenshotDisplayType>.Convert(this.screenshotDisplayType)
                    },
                    relationships = new()
                    {
                        appStoreVersionLocalization = new()
                        {
                            data = new()
                            {
                                id = id,
                            }
                        }
                    }
                }
            };
        }

        internal AppStoreClient.AppScreenshotSetAppScreenshotsLinkagesRequest CreateUpdateRequest()
        {
            return new()
            {
                data = screenshots.Select(a => new AppStoreClient.AppScreenshotSetAppScreenshotsLinkagesRequest.Data
                {
                    id = a.id
                }).ToArray(),
            };
        }
    }
}
