using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenterLeaderboardImage
    {
        public string? id { get; set; }
        public int? fileSize { get; set; }
        public string? fileName { get; set; }

        public GameCenterLeaderboardImage()
        {
        }

        public GameCenterLeaderboardImage(AppStoreClient.GameCenterLeaderboardImage data)
        {
            id = data.id;
            fileSize = data.attributes?.fileSize;
            fileName = data.attributes?.fileName;
        }
    }
}
