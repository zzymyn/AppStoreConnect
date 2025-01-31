namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenter
    {
		public GameCenterDetail detail { get; set; }
		public GameCenterGroup? group { get; set; }
		public GameCenterAchievement[] achievements { get; set; }
		public GameCenterLeaderboard[] leaderboards { get; set; }
		public GameCenterLeaderboardSet[] leaderboardSets { get; set; }

		public GameCenter(GameCenterDetail detail, GameCenterGroup? group, GameCenterAchievement[] achievements, GameCenterLeaderboard[] leaderboards, GameCenterLeaderboardSet[] leaderboardSets)
		{
			this.detail = detail;
			this.group = group;
			this.achievements = achievements;
			this.leaderboards = leaderboards;
			this.leaderboardSets = leaderboardSets;
		}
    }
}