namespace StormiumTeam.GameBase.Utility.AssetBackend
{
	public interface IBackendReceiver
	{
		RuntimeAssetBackendBase Backend { get; set; }

		void OnBackendSet();
		void OnPresentationSystemUpdate();
	}
}