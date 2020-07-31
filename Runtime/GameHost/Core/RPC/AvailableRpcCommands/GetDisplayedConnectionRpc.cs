using System;
using System.Collections.Generic;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public class GetDisplayedConnectionRpc : RpcCommandSystem
	{
		public class Result
		{
			public class Connection
			{
				public string Name { get; set; }
				public string Type           { get; set; }
				public string Address        { get; set; }
			}

			public Dictionary<string, List<Connection>> ConnectionMap { get; set; }
		}

		public override string CommandId => "displayallcon";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{
			// what
		}

		public event Action<Dictionary<string, List<Result.Connection>>> OnReply;   

		protected override void OnReceiveReply(GameHostCommandResponse response)
		{
			var result = response.Deserialize<Result>();
			
			Console.WriteLine(result.ConnectionMap.Count);
			foreach (var kvp in result.ConnectionMap)
			{
				Console.WriteLine(kvp.Key);
				Console.WriteLine(kvp.Value.Count);
				foreach (var elem in kvp.Value)
				{
					Console.WriteLine(elem.Type);
					Console.WriteLine(elem.Name);
					Console.WriteLine(elem.Address);
				}
			}
			
			OnReply?.Invoke(result.ConnectionMap);
		}
	}
}