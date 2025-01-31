using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class AppPreviewSet
    {
        public string id { get; set; }
        public AppStoreClient.AppPreviewSet.Attributes.PreviewType previewType { get; set; }
        public AppPreview[] appPreviews { get; set; }

        public AppPreviewSet()
        {
        }

        public AppPreviewSet(AppStoreClient.AppPreviewSet data)
        {
            this.id = data.id;
            this.previewType = data.attributes.previewType.Value;
        }

        internal void UpdateWithResponse(AppStoreClient.AppPreviewSet data)
        {
            this.id = data.id;
            this.previewType = data.attributes.previewType.Value;
        }

        internal AppStoreClient.AppPreviewSetCreateRequest CreateCreateRequest(string id)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        previewType = EnumExtensions<AppStoreClient.AppPreviewSetCreateRequest.Data.Attributes.PreviewType>.Convert(this.previewType)
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

        internal AppStoreClient.AppPreviewSetAppPreviewsLinkagesRequest CreateUpdateRequest()
        {
            return new()
            {
                data = appPreviews.Select(a => new AppStoreClient.AppPreviewSetAppPreviewsLinkagesRequest.Data
                {
                    id = a.id
                }).ToArray(),
            };
        }
    }
}
