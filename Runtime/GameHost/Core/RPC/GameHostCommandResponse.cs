using System;
using System.Runtime.CompilerServices;
using GameHost.Core.IO;
 using GameHost.Native;
 using GameHost.Native;
using Newtonsoft.Json;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	public ref struct GameHostCommandResponse
	{
		public TransportConnection Connection;
		public CharBuffer128        Command;
		public DataBufferReader    Data;

		public static GameHostCommandResponse GetResponse(TransportConnection connection, DataBufferReader data)
		{
			return new GameHostCommandResponse
			{
				Connection = connection,
				Command    = data.ReadBuffer<CharBuffer128>(),
				Data       = new DataBufferReader(data, data.CurrReadIndex, data.Length)
			};
		}

		public unsafe T Deserialize<T>(JsonSerializerSettings options = null)
		{
			return JsonConvert.DeserializeObject<T>(Data.ReadString(), options);
		}
	}
}