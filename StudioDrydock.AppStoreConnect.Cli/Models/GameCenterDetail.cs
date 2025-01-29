﻿using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterDetail
    {
        public string id { get; set; }
        public bool? arcadeEnabled { get; set; }
        public bool? challengeEnabled { get; set; }

        public GameCenterDetail(AppStoreClient.GameCenterDetail data)
        {
            this.id = data.id;
            this.arcadeEnabled = data.attributes.arcadeEnabled;
            this.challengeEnabled = data.attributes.challengeEnabled;
        }
    }
}
