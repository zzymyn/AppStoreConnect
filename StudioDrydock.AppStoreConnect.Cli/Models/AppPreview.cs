using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class AppPreview
    {
        public string id { get; set; }
        public int fileSize { get; set; }
        public string fileName { get; set; }
        public string sourceFileChecksum { get; set; }
        public string previewFrameTimeCode { get; set; }

        public AppPreview()
        {
        }

        public AppPreview(AppStoreClient.AppPreviewsResponse.Data data)
        {
            this.id = data.id;
            this.fileSize = data.attributes.fileSize.Value;
            this.fileName = data.attributes.fileName;
            this.sourceFileChecksum = data.attributes.sourceFileChecksum;
            this.previewFrameTimeCode = data.attributes.previewFrameTimeCode;
        }

        internal void UpdateWithResponse(AppStoreClient.AppPreviewResponse.Data data)
        {
            this.id = data.id;
            this.fileSize = data.attributes.fileSize.Value;
            this.fileName = data.attributes.fileName;
            this.sourceFileChecksum = data.attributes.sourceFileChecksum;
            this.previewFrameTimeCode = data.attributes.previewFrameTimeCode;
        }

        internal AppStoreClient.AppPreviewUpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        previewFrameTimeCode = this.previewFrameTimeCode
                    }
                }
            };
        }

        internal AppStoreClient.AppPreviewUpdateRequest CreateUploadCompleteRequest(string fileHash)
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        uploaded = true,
                        sourceFileChecksum = fileHash,
                    }
                }
            };
        }

        internal AppStoreClient.AppPreviewCreateRequest CreateCreateRequest(string setId, int fileSize, string fileName)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        fileName = fileName,
                        fileSize = fileSize,
                    },
                    relationships = new()
                    {
                        appPreviewSet = new()
                        {
                            data = new()
                            {
                                id = setId,
                            }
                        }
                    }
                }
            };
        }
    }
}
