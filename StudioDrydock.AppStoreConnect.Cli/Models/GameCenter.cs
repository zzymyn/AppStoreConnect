namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenter
    {
        public GameCenterDetail gameCenterDetail { get; set; }
        public GameCenterGroup gameCenterGroup { get; set; }
        public GameCenterAchievement[] gameCenterAchievements { get; set; }
        public GameCenterLeaderboard[] gameCenterLeaderboards { get; set; }
    }
}