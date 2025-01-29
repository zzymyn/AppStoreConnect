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
        public GameCenterLeaderboardLocalization[] localizations { get; set; }

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
    }
}
