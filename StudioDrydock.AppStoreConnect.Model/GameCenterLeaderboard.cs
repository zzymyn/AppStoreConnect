using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
	public class GameCenterLeaderboard
	{
		public string? id { get; set; }
		public AppStoreClient.GameCenterLeaderboard.Attributes.DefaultFormatter? defaultFormatter { get; set; }
		public string? referenceName { get; set; }
		public string? vendorIdentifier { get; set; }
		public AppStoreClient.GameCenterLeaderboard.Attributes.SubmissionType? submissionType { get; set; }
		public AppStoreClient.GameCenterLeaderboard.Attributes.ScoreSortType? scoreSortType { get; set; }
		public string? scoreRangeStart { get; set; }
		public string? scoreRangeEnd { get; set; }
		public string? recurrenceStartDate { get; set; }
		public string? recurrenceDuration { get; set; }
		public string? recurrenceRule { get; set; }
		public bool? archived { get; set; }
		public bool? live { get; set; }
		public string[]? leaderboardSets { get; set; }
		public GameCenterLeaderboardLocalization[]? localizations { get; set; }

		public GameCenterLeaderboard()
		{
		}

		public GameCenterLeaderboard(AppStoreClient.GameCenterLeaderboard data)
		{
			this.id = data.id;
			this.defaultFormatter = data.attributes?.defaultFormatter;
			this.referenceName = data.attributes?.referenceName;
			this.vendorIdentifier = data.attributes?.vendorIdentifier;
			this.submissionType = data.attributes?.submissionType;
			this.scoreSortType = data.attributes?.scoreSortType;
			this.scoreRangeStart = data.attributes?.scoreRangeStart;
			this.scoreRangeEnd = data.attributes?.scoreRangeEnd;
			this.recurrenceStartDate = data.attributes?.recurrenceStartDate;
			this.recurrenceDuration = data.attributes?.recurrenceDuration;
			this.recurrenceRule = data.attributes?.recurrenceRule;
			this.archived = data.attributes?.archived;
		}

		public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboard data)
		{
			this.id = data.id;
			this.defaultFormatter = data.attributes?.defaultFormatter;
			this.referenceName = data.attributes?.referenceName;
			this.vendorIdentifier = data.attributes?.vendorIdentifier;
			this.submissionType = data.attributes?.submissionType;
			this.scoreSortType = data.attributes?.scoreSortType;
			this.scoreRangeStart = data.attributes?.scoreRangeStart;
			this.scoreRangeEnd = data.attributes?.scoreRangeEnd;
			this.recurrenceStartDate = data.attributes?.recurrenceStartDate;
			this.recurrenceDuration = data.attributes?.recurrenceDuration;
			this.recurrenceRule = data.attributes?.recurrenceRule;
			this.archived = data.attributes?.archived;
		}

		public AppStoreClient.GameCenterLeaderboardCreateRequest CreateCreateRequest(string detailId, string? groupId)
		{
			var req = new AppStoreClient.GameCenterLeaderboardCreateRequest()
			{
				data = new()
				{
					attributes = new()
					{
						defaultFormatter = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.DefaultFormatter>.Convert(this.defaultFormatter)!.Value,
						referenceName = this.referenceName!,
						vendorIdentifier = this.vendorIdentifier!,
						submissionType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.SubmissionType>.Convert(this.submissionType)!.Value,
						scoreSortType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.ScoreSortType>.Convert(this.scoreSortType)!.Value,
						scoreRangeStart = this.scoreRangeStart,
						scoreRangeEnd = this.scoreRangeEnd,
						recurrenceStartDate = this.recurrenceStartDate,
						recurrenceDuration = this.recurrenceDuration,
						recurrenceRule = this.recurrenceRule,
					},
					relationships = new()
					{
					},
				},
			};
			if (leaderboardSets?.Length > 0)
			{
				req.data.relationships.gameCenterLeaderboardSets = new()
				{
					data = leaderboardSets.Select(x => new AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Relationships.GameCenterLeaderboardSets.Data()
					{
						id = x,
					}).ToArray(),
				};
			}
			else if (groupId != null)
			{
				req.data.relationships.gameCenterGroup = new()
				{
					data = new()
					{
						id = groupId,
					}
				};
			}
			else
			{
				req.data.relationships.gameCenterDetail = new()
				{
					data = new()
					{
						id = detailId,
					}
				};
			}
			return req;
		}

		public AppStoreClient.GameCenterLeaderboardUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = this.id!,
					attributes = new()
					{
						defaultFormatter = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.DefaultFormatter>.Convert(this.defaultFormatter),
						referenceName = this.referenceName,
						submissionType = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.SubmissionType>.Convert(this.submissionType),
						scoreSortType = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.ScoreSortType>.Convert(this.scoreSortType),
						scoreRangeStart = this.scoreRangeStart,
						scoreRangeEnd = this.scoreRangeEnd,
						recurrenceStartDate = this.recurrenceStartDate,
						recurrenceDuration = this.recurrenceDuration,
						recurrenceRule = this.recurrenceRule,
						archived = this.archived,
					},
				}
			};
		}
	}
}
