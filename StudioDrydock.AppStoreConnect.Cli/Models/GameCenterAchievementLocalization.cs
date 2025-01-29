using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterAchievementLocalization
    {
        public string id { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
        public string beforeEarnedDescription { get; set; }
        public string afterEarnedDescription { get; set; }
        public GameCenterAchievementImage image { get; set; }

        public GameCenterAchievementLocalization(AppStoreClient.GameCenterAchievementLocalization data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.name = data.attributes.name;
            this.beforeEarnedDescription = data.attributes.beforeEarnedDescription;
            this.afterEarnedDescription = data.attributes.afterEarnedDescription;
        }
    }
}
