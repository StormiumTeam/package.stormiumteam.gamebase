using System;
using System.Collections.Generic;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.BaseSystems;

namespace GameHost.Core.RPC
{
	public abstract class RpcCommandSystem : AbsGameBaseSystem
	{
		public abstract string CommandId { get; }

		protected abstract void OnReceiveRequest(GameHostCommandResponse response);
		protected abstract void OnReceiveReply(GameHostCommandResponse   response);

		private RpcCollectionSystem collectionSystem;
		private GameHostConnector   listener;

		private DataBufferWriter tempWriter;
		private bool             isWriting;
		private bool             isInRequestSection;

		protected override void OnCreate()
		{
			base.OnCreate();

			collectionSystem = World.GetExistingSystem<RpcCollectionSystem>();
			listener         = World.GetExistingSystem<GameHostConnector>();

			collectionSystem.CommandRequest += r =>
			{
				if (CommandId.AsSpan().SequenceEqual(r.Command.Span))
				{
					isInRequestSection = false;
					OnReceiveRequest(r);
					if (isWriting)
					{
						isWriting = false;
						listener.SendReply(r.Connection, r.Command, tempWriter);
						tempWriter.Length = 0;
					}
				}
			};
			collectionSystem.CommandReply += r =>
			{
				isInRequestSection = true;
				if (CommandId.AsSpan().SequenceEqual(r.Command.Span))
					OnReceiveReply(r);
			};
		}

		protected DataBufferWriter GetReplyWriter()
		{
			if (!isInRequestSection)
				throw new InvalidOperationException("Can't reply in a reply.");

			if (isWriting)
				throw new InvalidOperationException("Already writing");

			tempWriter.Length = 0;
			isWriting         = true;
			return tempWriter;
		}

		protected override void OnUpdate()
		{
		}
	}
}