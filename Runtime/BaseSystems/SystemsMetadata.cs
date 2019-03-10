using System;
using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Runtime.SharedSystems
{
	public abstract class InstanceSystemMetadata<TSystem>
		where TSystem : ComponentSystem
	{
		public static readonly InstanceSystemMetadata<TSystem> Instance;
		
		private List<Type> m_RestrictedGameLoopTypes;
		
		protected virtual void MetadataInit()
		{
		}

		protected void RestrictTo<T1>(T1 gameLoop1)
			where T1 : IGameLoop
		{
			m_RestrictedGameLoopTypes.Clear();
			m_RestrictedGameLoopTypes.Add(typeof(T1));
		}

		protected void RestrictTo<T1, T2>(T1 gameLoop1, T2 gameLoop2)
			where T1 : IGameLoop
			where T2 : IGameLoop
		{
			m_RestrictedGameLoopTypes.Clear();
			m_RestrictedGameLoopTypes.Add(typeof(T1));
			m_RestrictedGameLoopTypes.Add(typeof(T2));
		}

		public bool CanRunGameLoop<T>(T gameLoop)
			where T : IGameLoop
		{
			return m_RestrictedGameLoopTypes.Count == 0 || m_RestrictedGameLoopTypes.Contains(typeof(T));
		}
	}
}