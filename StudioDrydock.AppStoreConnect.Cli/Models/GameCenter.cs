namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenter
    {
        public GameCenterDetail detail { get; set; }
        public GameCenterGroup group { get; set; }
        public GameCenterAchievement[] achievements { get; set; }
        public GameCenterLeaderboard[] leaderboards { get; set; }
        public GameCenterLeaderboardSet[] leaderboardSets { get; set; }
    }
}