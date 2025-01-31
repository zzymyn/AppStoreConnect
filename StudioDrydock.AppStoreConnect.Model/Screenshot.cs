using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class Screenshot
    {
        public string? id { get; set; }
        public int? fileSize { get; set; }
        public string? fileName { get; set; }
        public string? sourceFileChecksum { get; set; }

        public Screenshot()
        {
        }

        public Screenshot(AppStoreClient.AppScreenshot data)
        {
            this.id = data.id;
            this.fileSize = data.attributes?.fileSize;
            this.fileName = data.attributes?.fileName;
            this.sourceFileChecksum = data.attributes?.sourceFileChecksum;
        }

		public void UpdateWithResponse(AppStoreClient.AppScreenshot data)
        {
            this.id = data.id;
            this.fileSize = data.attributes?.fileSize;
            this.fileName = data.attributes?.fileName;
            this.sourceFileChecksum = data.attributes?.sourceFileChecksum;
        }

		public AppStoreClient.AppScreenshotUpdateRequest CreateUploadCompleteRequest(string fileHash)
        {
            return new()
            {
                data = new()
                {
                    id = this.id!,
                    attributes = new()
                    {
                        uploaded = true,
                        sourceFileChecksum = fileHash,
                    }
                }
            };
        }

		public AppStoreClient.AppScreenshotCreateRequest CreateCreateRequest(string setId, int fileSize, string fileName)
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
                        appScreenshotSet = new()
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
