namespace StudioDrydock.AppStoreConnect.Model.Files;

public class GameCenter(string appId, GameCenterDetail detail, GameCenterGroup? group, List<GameCenterAchievement> achievements, List<GameCenterLeaderboard> leaderboards, List<GameCenterLeaderboardSet> leaderboardSets)
{
    public string appId { get; set; } = appId;
    public GameCenterDetail detail { get; set; } = detail;
    public GameCenterGroup? group { get; set; } = group;
    public List<GameCenterAchievement> achievements { get; set; } = achievements;
    public List<GameCenterLeaderboard> leaderboards { get; set; } = leaderboards;
    public List<GameCenterLeaderboardSet> leaderboardSets { get; set; } = leaderboardSets;
}