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
        public ScreenshotDisplayType screenshotDisplayType { get; set; }
        public Screenshot[] screenshots { get; set; }

        public ScreenshotSet()
        {
        }

        public ScreenshotSet(AppStoreClient.AppScreenshotSetsResponse.Data data)
        {
            this.id = data.id;
            this.screenshotDisplayType = EnumExtensions<ScreenshotDisplayType>.Convert(data.attributes.screenshotDisplayType.Value);
        }

        internal void UpdateWithResponse(AppStoreClient.AppScreenshotSetResponse.Data data)
        {
            this.id = data.id;
            this.screenshotDisplayType = EnumExtensions<ScreenshotDisplayType>.Convert(data.attributes.screenshotDisplayType.Value);
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

        internal AppStoreClient.PatchAppScreenshotSetsAppScreenshotsRequest CreateUpdateRequest()
        {
            return new()
            {
                data = screenshots.Select(a => new AppStoreClient.PatchAppScreenshotSetsAppScreenshotsRequest.Data
                {
                    id = a.id
                }).ToArray(),
            };
        }
    }
}
