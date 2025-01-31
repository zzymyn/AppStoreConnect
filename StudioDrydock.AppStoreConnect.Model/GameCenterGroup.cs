using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class GameCenterGroup
    {
        public string? id { get; set; }
        public string? referenceName { get; set; }

        public GameCenterGroup()
        {
        }

		public GameCenterGroup(AppStoreClient.GameCenterGroup data)
        {
            this.id = data.id;
            this.referenceName = data.attributes?.referenceName;
        }
    }
}
