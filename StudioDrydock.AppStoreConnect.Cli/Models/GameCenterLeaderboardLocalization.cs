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
        public AppStoreClient.GameCenterLeaderboardLocalization.Attributes.FormatterOverride? formatterOverride { get; set; }
        public string formatterSuffix { get; set; }
        public string formatterSuffixSingular { get; set; }
        public GameCenterLeaderboardImage image { get; set; }

        public GameCenterLeaderboardLocalization()
		{ }

		public GameCenterLeaderboardLocalization(AppStoreClient.GameCenterLeaderboardLocalization data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.name = data.attributes.name;
			this.formatterOverride = data.attributes.formatterOverride;
            this.formatterSuffix = data.attributes.formatterSuffix;
            this.formatterSuffixSingular = data.attributes.formatterSuffixSingular;
        }

		internal void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardLocalization data)
		{
			this.id = data.id;
			this.locale = data.attributes.locale;
			this.name = data.attributes.name;
			this.formatterOverride = data.attributes.formatterOverride;
			this.formatterSuffix = data.attributes.formatterSuffix;
			this.formatterSuffixSingular = data.attributes.formatterSuffixSingular;
		}

		internal AppStoreClient.GameCenterLeaderboardLocalizationCreateRequest CreateCreateRequest(string lbId)
		{
			return new()
			{
				data = new()
				{
					attributes = new()
					{
						locale = this.locale,
						name = this.name,
						formatterOverride = this.formatterOverride.HasValue ? EnumExtensions<AppStoreClient.GameCenterLeaderboardLocalizationCreateRequest.Data.Attributes.FormatterOverride>.Convert(this.formatterOverride.Value) : null,
						formatterSuffix = this.formatterSuffix,
						formatterSuffixSingular = this.formatterSuffixSingular
					},
					relationships = new()
					{
						gameCenterLeaderboard = new()
						{
							data = new()
							{
								id = lbId
							}
						}
					}
				}
			};
		}

		internal AppStoreClient.GameCenterLeaderboardLocalizationUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = this.id,
					attributes = new()
					{
						name = this.name,
						formatterOverride = this.formatterOverride.HasValue ? EnumExtensions<AppStoreClient.GameCenterLeaderboardLocalizationUpdateRequest.Data.Attributes.FormatterOverride>.Convert(this.formatterOverride.Value) : null,
						formatterSuffix = this.formatterSuffix,
						formatterSuffixSingular = this.formatterSuffixSingular
					}
				}
			};
		}
	}
}
