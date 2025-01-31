using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
	public class GameCenterLeaderboard
	{
		public string id { get; set; }
		public DefaultFormatter? defaultFormatter { get; set; }
		public string referenceName { get; set; }
		public string vendorIdentifier { get; set; }
		public SubmissionType? submissionType { get; set; }
		public ScoreSortType? scoreSortType { get; set; }
		public string scoreRangeStart { get; set; }
		public string scoreRangeEnd { get; set; }
		public string recurrenceStartDate { get; set; }
		public string recurrenceDuration { get; set; }
		public string recurrenceRule { get; set; }
		public bool? archived { get; set; }
		public bool? live { get; set; }
		public string[] leaderboardSets { get; set; }
		public GameCenterLeaderboardLocalization[] localizations { get; set; }

		public GameCenterLeaderboard()
		{ }

		public GameCenterLeaderboard(AppStoreClient.GameCenterLeaderboard data)
		{
			this.id = data.id;
			this.defaultFormatter = data.attributes.defaultFormatter.HasValue ? EnumExtensions<DefaultFormatter>.Convert(data.attributes.defaultFormatter.Value) : null;
			this.referenceName = data.attributes.referenceName;
			this.vendorIdentifier = data.attributes.vendorIdentifier;
			this.submissionType = data.attributes.submissionType.HasValue ? EnumExtensions<SubmissionType>.Convert(data.attributes.submissionType.Value) : null;
			this.scoreSortType = data.attributes.scoreSortType.HasValue ? EnumExtensions<ScoreSortType>.Convert(data.attributes.scoreSortType.Value) : null;
			this.scoreRangeStart = data.attributes.scoreRangeStart;
			this.scoreRangeEnd = data.attributes.scoreRangeEnd;
			this.recurrenceStartDate = data.attributes.recurrenceStartDate;
			this.recurrenceDuration = data.attributes.recurrenceDuration;
			this.recurrenceRule = data.attributes.recurrenceRule;
			this.archived = data.attributes.archived;
		}

		internal void UpdateWithResponse(AppStoreClient.GameCenterLeaderboard data)
		{
			this.id = data.id;
			this.defaultFormatter = data.attributes.defaultFormatter.HasValue ? EnumExtensions<DefaultFormatter>.Convert(data.attributes.defaultFormatter.Value) : null;
			this.referenceName = data.attributes.referenceName;
			this.vendorIdentifier = data.attributes.vendorIdentifier;
			this.submissionType = data.attributes.submissionType.HasValue ? EnumExtensions<SubmissionType>.Convert(data.attributes.submissionType.Value) : null;
			this.scoreSortType = data.attributes.scoreSortType.HasValue ? EnumExtensions<ScoreSortType>.Convert(data.attributes.scoreSortType.Value) : null;
			this.scoreRangeStart = data.attributes.scoreRangeStart;
			this.scoreRangeEnd = data.attributes.scoreRangeEnd;
			this.recurrenceStartDate = data.attributes.recurrenceStartDate;
			this.recurrenceDuration = data.attributes.recurrenceDuration;
			this.recurrenceRule = data.attributes.recurrenceRule;
			this.archived = data.attributes.archived;
		}

		internal AppStoreClient.GameCenterLeaderboardCreateRequest CreateCreateRequest(string detailId, string groupId)
		{
			var req = new AppStoreClient.GameCenterLeaderboardCreateRequest()
			{
				data = new()
				{
					attributes = new()
					{
						defaultFormatter = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.DefaultFormatter>.Convert(this.defaultFormatter.Value),
						referenceName = this.referenceName,
						vendorIdentifier = this.vendorIdentifier,
						submissionType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.SubmissionType>.Convert(this.submissionType.Value),
						scoreSortType = EnumExtensions<AppStoreClient.GameCenterLeaderboardCreateRequest.Data.Attributes.ScoreSortType>.Convert(this.scoreSortType.Value),
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

		internal AppStoreClient.GameCenterLeaderboardUpdateRequest CreateUpdateRequest()
		{
			return new()
			{
				data = new()
				{
					id = this.id,
					attributes = new()
					{
						defaultFormatter = this.defaultFormatter.HasValue ? EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.DefaultFormatter>.Convert(this.defaultFormatter.Value) : null,
						referenceName = this.referenceName,
						submissionType = this.submissionType.HasValue ? EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.SubmissionType>.Convert(this.submissionType.Value) : null,
						scoreSortType = this.scoreSortType.HasValue ? EnumExtensions<AppStoreClient.GameCenterLeaderboardUpdateRequest.Data.Attributes.ScoreSortType>.Convert(this.scoreSortType.Value) : null,
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
