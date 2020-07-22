using System;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase.Utility.Modules
{
	[Flags]
	public enum ModuleUpdateType
	{
		MainThread = 1,
		Job        = 2,
		All        = MainThread | Job
	}

	public abstract class BaseSystemModule
	{
		protected      ModuleUpdateType CurrentUpdateType;
		public virtual ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		public ComponentSystemBase System    { get; private set; }
		public bool                IsEnabled => System != null;

		public EntityManager EntityManager => System.EntityManager;

		public void Enable(ComponentSystemBase system)
		{
			System = system;
			OnEnable();
		}

		public void Update()
		{
			if ((UpdateType & ModuleUpdateType.MainThread) == 0)
				throw new InvalidOperationException();

			if (!IsEnabled)
				throw new InvalidOperationException();

			CurrentUpdateType = ModuleUpdateType.MainThread;

			var tmp = default(JobHandle);
			OnUpdate(ref tmp);
		}

		public JobHandle Update(JobHandle jobHandle)
		{
			if ((UpdateType & ModuleUpdateType.Job) == 0)
				throw new InvalidOperationException();

			if (!IsEnabled)
				throw new InvalidOperationException();

			CurrentUpdateType = ModuleUpdateType.Job;

			OnUpdate(ref jobHandle);
			return jobHandle;
		}

		public void Disable()
		{
			OnDisable();
			System = null;
		}

		protected abstract void OnEnable();
		protected abstract void OnUpdate(ref JobHandle jobHandle);
		protected abstract void OnDisable();
	}
}