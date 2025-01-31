using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
	public class GameCenterAchievementImage
	{
		public string id { get; set; }
		public int fileSize { get; set; }
		public string fileName { get; set; }

		public GameCenterAchievementImage()
		{ }

		public GameCenterAchievementImage(AppStoreClient.GameCenterAchievementImage data)
		{
			this.id = data.id;
			this.fileSize = data.attributes.fileSize.Value;
			this.fileName = data.attributes.fileName;
		}
	}
}
