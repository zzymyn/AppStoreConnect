using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterLeaderboardSetLocalization
	{
        public string id { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
        public GameCenterLeaderboardSetImage image { get; set; }

        public GameCenterLeaderboardSetLocalization(AppStoreClient.GameCenterLeaderboardSetLocalization data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.name = data.attributes.name;
        }

        internal void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardSetLocalization data)
		{
			this.id = data.id;
			this.locale = data.attributes.locale;
			this.name = data.attributes.name;
		}

		internal AppStoreClient.GameCenterLeaderboardSetLocalizationCreateRequest CreateCreateRequest(string lbsetId)
		{
			return new()
			{
				data = new()
				{
					attributes = new()
					{
						locale = this.locale,
						name = this.name
					},
					relationships = new()
					{
						gameCenterLeaderboardSet = new()
						{
							data = new()
							{
								id = lbsetId
							}
						}
					}
				}
			};
		}

		internal AppStoreClient.GameCenterLeaderboardSetLocalizationUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = this.id,
					attributes = new()
					{
						name = this.name
					}
				}
			};
		}
	}
}
