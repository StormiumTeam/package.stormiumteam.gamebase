namespace GameHost.Core.RPC.Interfaces
{
	public interface IGameHostRpcPacket
	{
		public string MethodName { get; }
	}
	
	public interface IGameHostRpcWithResponsePacket<TResponse> : IGameHostRpcPacket
		where TResponse : IGameHostRpcResponsePacket
	{
	}

	public interface IGameHostRpcResponsePacket : IGameHostRpcPacket
	{
	}
}