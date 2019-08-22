using System;
using Unity.Entities;

namespace StormiumTeam.GameBase.Misc
{
	public struct SetTemporaryActiveWorld : IDisposable
	{
		private readonly World m_PreviousWorld;

		public SetTemporaryActiveWorld(World newWorld)
		{
			m_PreviousWorld = World.Active;
			World.Active    = newWorld;
		}

		public void Dispose()
		{
			World.Active = m_PreviousWorld;
		}
	}
}