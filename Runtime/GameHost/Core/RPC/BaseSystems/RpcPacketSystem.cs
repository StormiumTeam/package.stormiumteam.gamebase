using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameHost.Core.RPC.Interfaces;
using Newtonsoft.Json.Linq;
using StormiumTeam.GameBase.BaseSystems;

namespace GameHost.Core.RPC.BaseSystems
{
	public abstract class RpcPacketSystem<T> : AbsGameBaseSystem
		where T : IGameHostRpcPacket, new()
	{
		private GameHostConnector gameHostConnector;

		protected override void OnCreate()
		{
			base.OnCreate();

			gameHostConnector.RpcClient.SubscribeNotification<T>(OnNotification);
		}

		protected abstract void OnNotification(T notification);
	}

	public abstract class RpcPacketWithResponseSystem<T, TResponse> : AbsGameBaseSystem
		where T : IGameHostRpcWithResponsePacket<TResponse>, new()
		where TResponse : IGameHostRpcResponsePacket
	{
		private GameHostConnector gameHostConnector;

		protected override void OnCreate()
		{
			base.OnCreate();

			gameHostConnector.RpcClient.SubscribeRequest<T>(PrivateGetResponse);
		}

		private async Task<JObject> PrivateGetResponse(T request)
		{
			lastError = null;

			var response = await GetResponse(request);
			if (lastError != null)
			{
				throw new JsonRpcException(lastError.Value.Code, lastError.Value.Message);
			}

			return JObject.FromObject(response);
		}

		protected abstract UniTask<TResponse> GetResponse(T request);

		private RpcPacketError? lastError;

		protected UniTask<TResponse> WithError(RpcPacketError packet)
		{
			lastError = packet;
			return UniTask.FromResult(default(TResponse));
		}

		protected UniTask<TResponse> WithError(int code, string message)
		{
			lastError = new RpcPacketError(code, message);
			return UniTask.FromResult(default(TResponse));
		}

		protected UniTask<TResponse> WithResult(TResponse response) => UniTask.FromResult(response);

		public struct RpcPacketError
		{
			public int    Code;
			public string Message;

			public RpcPacketError(int code, string message)
			{
				Code    = code;
				Message = message;
			}
		}
	}
}