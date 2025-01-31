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
			id = data.id;
			defaultFormatter = data.attributes?.defaultFormatter;
			referenceName = data.attributes?.referenceName;
			vendorIdentifier = data.attributes?.vendorIdentifier;
			submissionType = data.attributes?.submissionType;
			scoreSortType = data.attributes?.scoreSortType;
			scoreRangeStart = data.attributes?.scoreRangeStart;
			scoreRangeEnd = data.attributes?.scoreRangeEnd;
			recurrenceStartDate = data.attributes?.recurrenceStartDate;
			recurrenceDuration = data.attributes?.recurrenceDuration;
			recurrenceRule = data.attributes?.recurrenceRule;
			archived = data.attributes?.archived;
		}

		public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboard data)
		{
			id = data.id;
			defaultFormatter = data.attributes?.defaultFormatter;
			referenceName = data.attributes?.referenceName;
			vendorIdentifier = data.attributes?.vendorIdentifier;
			submissionType = data.attributes?.submissionType;
			scoreSortType = data.attributes?.scoreSortType;
			scoreRangeStart = data.attributes?.scoreRangeStart;
			scoreRangeEnd = data.attributes?.scoreRangeEnd;
			recurrenceStartDate = data.attributes?.recurrenceStartDate;
			recurrenceDuration = data.attributes?.recurrenceDuration;
			recurrenceRule = data.attributes?.recurrenceRule;
			archived = data.attributes?.archived;
		}

		public AppStoreClient.GameCenterLeaderboardCreateRequest CreateCreateRequest(string detailId, string? groupId)
		{
			var req = new AppStoreClient.GameCenterLeaderboardCreateRequest()
			{
				data = new()
				{
					attributes = new()
					{
						defaultFormatter = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.DefaultFormatter>.Convert(defaultFormatter)!.Value,
						referenceName = referenceName!,
						vendorIdentifier = vendorIdentifier!,
						submissionType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.SubmissionType>.Convert(submissionType)!.Value,
						scoreSortType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.ScoreSortType>.Convert(scoreSortType)!.Value,
						scoreRangeStart = scoreRangeStart,
						scoreRangeEnd = scoreRangeEnd,
						recurrenceStartDate = recurrenceStartDate,
						recurrenceDuration = recurrenceDuration,
						recurrenceRule = recurrenceRule,
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
					id = id!,
					attributes = new()
					{
						defaultFormatter = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.DefaultFormatter>.Convert(defaultFormatter),
						referenceName = referenceName,
						submissionType = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.SubmissionType>.Convert(submissionType),
						scoreSortType = EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.ScoreSortType>.Convert(scoreSortType),
						scoreRangeStart = scoreRangeStart,
						scoreRangeEnd = scoreRangeEnd,
						recurrenceStartDate = recurrenceStartDate,
						recurrenceDuration = recurrenceDuration,
						recurrenceRule = recurrenceRule,
						archived = archived,
					},
				}
			};
		}
	}
}
