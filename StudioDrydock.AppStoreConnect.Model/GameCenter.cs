namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenter(GameCenterDetail detail, GameCenterGroup? group, GameCenterAchievement[] achievements, GameCenterLeaderboard[] leaderboards, GameCenterLeaderboardSet[] leaderboardSets)
	{
		public GameCenterDetail detail { get; set; } = detail;
		public GameCenterGroup? group { get; set; } = group;
		public GameCenterAchievement[] achievements { get; set; } = achievements;
		public GameCenterLeaderboard[] leaderboards { get; set; } = leaderboards;
		public GameCenterLeaderboardSet[] leaderboardSets { get; set; } = leaderboardSets;
	}
}