﻿namespace GameHost.Core.RPC
{
	public enum RpcMessageType
	{
		Custom  = 0,
		Command = 1,
	}

	public enum RpcCommandType
	{
		Send  = 0,
		Reply = 1
	}
}