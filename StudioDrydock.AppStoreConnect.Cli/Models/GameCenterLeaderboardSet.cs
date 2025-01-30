using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
	public class GameCenterLeaderboardSet
	{
		public string id { get; set; }
		public string referenceName { get; set; }
		public string vendorIdentifier { get; set; }
		public bool? live { get; set; }
		public GameCenterLeaderboardSetLocalization[] localizations { get; set; }

		public GameCenterLeaderboardSet(AppStoreClient.GameCenterLeaderboardSet data)
		{
			this.id = data.id;
			this.referenceName = data.attributes.referenceName;
			this.vendorIdentifier = data.attributes.vendorIdentifier;
		}

		internal void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardSet data)
		{
			this.id = data.id;
			this.referenceName = data.attributes.referenceName;
			this.vendorIdentifier = data.attributes.vendorIdentifier;
		}

		internal AppStoreClient.GameCenterLeaderboardSetCreateRequest CreateCreateRequest(string detailId, string groupId)
		{
			var req = new AppStoreClient.GameCenterLeaderboardSetCreateRequest()
			{
				data = new()
				{
					attributes = new()
					{
						referenceName = this.referenceName,
						vendorIdentifier = this.vendorIdentifier,
					},
					relationships = new()
					{
						gameCenterDetail = new()
						{
							data = new()
							{
								id = detailId,
							}
						},
					},
				},
			};
			if (groupId != null)
			{
				req.data.relationships.gameCenterGroup = new()
				{
					data = new()
					{
						id = groupId,
					}
				};
			}
			return req;
		}

		internal AppStoreClient.GameCenterLeaderboardSetUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = this.id,
					attributes = new()
					{
						referenceName = this.referenceName,
					}
				}
			};
		}
	}
}
