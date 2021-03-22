using System;
using System.Collections.Generic;
using GameHost.Core.RPC.BaseSystems;
using GameHost.Core.RPC.Interfaces;
using JetBrains.Annotations;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public struct GetDisplayedConnectionRpc : IGameHostRpcWithResponsePacket<GetDisplayedConnectionRpc.Response>
	{
		public const string RpcMethodName = "GameHost.GetDisplayedConnection";

		public string MethodName => RpcMethodName;
		
		public struct Response : IGameHostRpcResponsePacket
		{
			public string MethodName => RpcMethodName;
			
			public struct Connection
			{
				public string Name    { get; set; }
				public string Type    { get; set; }
				public string Address { get; set; }
			}

			public Dictionary<string, List<Connection>> Connections { get; set; }
		}
	}
}