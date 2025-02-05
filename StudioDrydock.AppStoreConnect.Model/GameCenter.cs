namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenter
	{
		public required GameCenterDetail detail { get; set; }
		public required GameCenterGroup? group { get; set; }
		public required GameCenterAchievement[] achievements { get; set; }
		public required GameCenterLeaderboard[] leaderboards { get; set; }
		public required GameCenterLeaderboardSet[] leaderboardSets { get; set; }
	}
}