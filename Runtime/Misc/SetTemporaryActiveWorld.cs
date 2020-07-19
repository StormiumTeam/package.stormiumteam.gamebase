using System;
using Unity.Entities;

namespace StormiumTeam.GameBase.Misc
{
	public struct SetTemporaryActiveWorld : IDisposable
	{
		private readonly World m_PreviousWorld;

		public SetTemporaryActiveWorld(World newWorld)
		{
			m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld    = newWorld;
		}

		public void Dispose()
		{
			World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
		}
	}
}