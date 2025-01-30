using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterAchievement
    {
        public string id { get; set; }
        public string referenceName { get; set; }
        public string vendorIdentifier { get; set; }
        public int? points { get; set; }
        public bool? showBeforeEarned { get; set; }
        public bool? repeatable { get; set; }
        public bool? archived { get; set; }
        public bool? live { get; set; }
        public GameCenterAchievementLocalization[] localizations { get; set; }

        public GameCenterAchievement(AppStoreClient.GameCenterAchievement data)
        {
            this.id = data.id;
            this.referenceName = data.attributes.referenceName;
            this.vendorIdentifier = data.attributes.vendorIdentifier;
            this.points = data.attributes.points;
            this.showBeforeEarned = data.attributes.showBeforeEarned;
            this.repeatable = data.attributes.repeatable;
            this.archived = data.attributes.archived;
        }
    }
}
