using System;
using Unity.Entities;

namespace DefaultNamespace.Utility.DOTS
{
	public struct SetTemporaryInjectionWorld : IDisposable
	{
		private readonly World m_PreviousWorld;

		public SetTemporaryInjectionWorld(World newWorld)
		{
			m_PreviousWorld                       = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld = newWorld ?? m_PreviousWorld;
		}

		public void Dispose()
		{
			World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
		}
	}
}