using StormiumTeam.GameBase.BaseSystems;

namespace GameHost.Core.RPC
{
	public class RpcCollectionSystem : AbsGameBaseSystem
	{
		public delegate void OnCommandRequest(GameHostCommandResponse response);

		public delegate void OnCommandReply(GameHostCommandResponse response);

		protected override void OnCreate()
		{
			base.OnCreate();
			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}

		public event OnCommandRequest CommandRequest;
		public event OnCommandRequest CommandReply;

		internal void TriggerCommandRequest(GameHostCommandResponse response)
		{
			CommandRequest?.Invoke(response);
		}

		internal void TriggerCommandReply(GameHostCommandResponse response)
		{
			CommandReply?.Invoke(response);
		}
	}
}