﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenterLeaderboardSetLocalization
	{
        public string? id { get; set; }
        public string? locale { get; set; }
        public string? name { get; set; }
        public GameCenterLeaderboardSetImage? image { get; set; }

		public GameCenterLeaderboardSetLocalization()
		{
		}

		public GameCenterLeaderboardSetLocalization(AppStoreClient.GameCenterLeaderboardSetLocalization data)
        {
            id = data.id;
            locale = data.attributes?.locale;
            name = data.attributes?.name;
        }

		public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardSetLocalization data)
		{
			id = data.id;
			locale = data.attributes?.locale;
			name = data.attributes?.name;
		}

		public AppStoreClient.GameCenterLeaderboardSetLocalizationCreateRequest CreateCreateRequest(string lbsetId)
		{
			return new()
			{
				data = new()
				{
					attributes = new()
					{
						locale = locale!,
						name = name!,
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

		public AppStoreClient.GameCenterLeaderboardSetLocalizationUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = id!,
					attributes = new()
					{
						name = name
					}
				}
			};
		}
	}
}
