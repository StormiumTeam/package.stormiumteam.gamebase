using System;

namespace GameHost.Core.IO
{
	/// <summary>
	/// Event data
	/// </summary>
	public ref struct TransportEvent
	{
		public enum EType : byte
		{
			None              = 0,
			RequestConnection = 9,
			Connect           = 10,
			Disconnect        = 15,
			Data              = 20,
		}

		public EType               Type;
		public TransportConnection Connection;
		public Span<byte>          Data;
	}

	/// <summary>
	/// Contain data about a connection
	/// </summary>
	public struct TransportConnection
	{
		public enum State : byte
		{
			Disconnected    = 0,
			Connecting      = 5,
			PendingApproval = 10,
			Connected       = 15
		}

		public uint Id;
		public uint Version;

		public bool IsCreated => Version > 0;
	}

	/// <summary>
	/// A transport channel indicate how a data should be sent.
	/// </summary>
	public struct TransportChannel
	{
		public int Id;
		public int Channel;
	}

	/// <summary>
	/// A transport address is a way to connect to other transports without knowing what type to instantiate.
	/// </summary>
	public abstract class TransportAddress
	{
		public abstract TransportDriver Connect();
	}

	/// <summary>
	/// A transport driver is an interface for sending data between connections.
	/// </summary>
	public abstract class TransportDriver : IDisposable
	{
		public abstract TransportAddress TransportAddress { get; }

		/// <summary>
		/// Accept incoming connections
		/// </summary>
		/// <returns>Return an accepted connection</returns>
		public abstract TransportConnection Accept();

		/// <summary>
		/// Update the driver.
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// Pop a driver event from a connection
		/// </summary>
		/// <returns></returns>
		public abstract TransportEvent PopEvent();

		/// <summary>
		/// Get the connection state of a connection
		/// </summary>
		/// <param name="con"></param>
		/// <returns></returns>
		public abstract TransportConnection.State GetConnectionState(TransportConnection con);

		/// <summary>
		/// Send data to a connection
		/// </summary>
		/// <param name="chan"></param>
		/// <param name="con"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract int Send(TransportChannel chan, TransportConnection con, Span<byte> data);
		
		/// <summary>
		/// Broadcast to all connections
		/// </summary>
		/// <param name="chan"></param>
		/// <param name="con"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract int Broadcast(TransportChannel chan, Span<byte> data);

		/// <summary>
		/// Dispose driver's resources.
		/// </summary>
		public abstract void Dispose();
	}
}