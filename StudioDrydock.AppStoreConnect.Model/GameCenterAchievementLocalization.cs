using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenterAchievementLocalization
    {
        public string? id { get; set; }
        public string? locale { get; set; }
        public string? name { get; set; }
        public string? beforeEarnedDescription { get; set; }
        public string? afterEarnedDescription { get; set; }
        public GameCenterAchievementImage? image { get; set; }

        public GameCenterAchievementLocalization()
		{
		}

		public GameCenterAchievementLocalization(AppStoreClient.GameCenterAchievementLocalization data)
        {
            id = data.id;
            locale = data.attributes?.locale;
            name = data.attributes?.name;
            beforeEarnedDescription = data.attributes?.beforeEarnedDescription;
            afterEarnedDescription = data.attributes?.afterEarnedDescription;
        }

		public void UpdateWithResponse(AppStoreClient.GameCenterAchievementLocalization data)
        {
			id = data.id;
			locale = data.attributes?.locale;
			name = data.attributes?.name;
			beforeEarnedDescription = data.attributes?.beforeEarnedDescription;
			afterEarnedDescription = data.attributes?.afterEarnedDescription;
		}

		public AppStoreClient.GameCenterAchievementLocalizationCreateRequest CreateCreateRequest(string achievementId)
		{
			return new()
			{
				data = new()
				{
					attributes = new()
					{
						locale = locale!,
						name = name!,
						beforeEarnedDescription = beforeEarnedDescription!,
						afterEarnedDescription = afterEarnedDescription!,
					},
					relationships = new()
					{
						gameCenterAchievement = new()
						{
							data = new()
							{
								id = achievementId
							}
						}
					}
				}
			};
		}

		public AppStoreClient.GameCenterAchievementLocalizationUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = id!,
					attributes = new()
					{
						name = name,
						beforeEarnedDescription = beforeEarnedDescription,
						afterEarnedDescription = afterEarnedDescription
					}
				}
			};
		}
	}
}
