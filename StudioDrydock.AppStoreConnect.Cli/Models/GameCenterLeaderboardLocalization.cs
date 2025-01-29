using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterLeaderboardLocalization
    {
        public string id { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
        public FormatterOverride? formatterOverride { get; set; }
        public string formatterSuffix { get; set; }
        public string formatterSuffixSingular { get; set; }
        public GameCenterLeaderboardImage image { get; set; }

        public GameCenterLeaderboardLocalization(AppStoreClient.GameCenterLeaderboardLocalization data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.name = data.attributes.name;
            this.formatterOverride = data.attributes.formatterOverride.HasValue ? EnumExtensions<FormatterOverride>.Convert(data.attributes.formatterOverride.Value) : null;
            this.formatterSuffix = data.attributes.formatterSuffix;
            this.formatterSuffixSingular = data.attributes.formatterSuffixSingular;
        }
    }
}
