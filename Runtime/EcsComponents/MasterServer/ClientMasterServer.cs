using Unity.Collections;
using Unity.Entities;

namespace EcsComponents.MasterServer
{
	public struct ConnectedMasterServerClient : IComponentData
	{
		/// NEVER SHARE IT
		public NativeString64 Token;

		public int            ClientId;
		public ulong          UserId;
		public NativeString64 UserLogin;
	}

	public struct MasterServerIsPlayer : IComponentData
	{
	}

	public struct MasterServerIsServer : IComponentData
	{
	}
}