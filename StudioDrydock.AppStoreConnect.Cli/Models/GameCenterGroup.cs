using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class GameCenterGroup
    {
        public string id { get; set; }
        public string referenceName { get; set; }

		public GameCenterGroup(AppStoreClient.GameCenterGroup data)
        {
            this.id = data.id;
            this.referenceName = data.attributes.referenceName;
        }
    }
}
