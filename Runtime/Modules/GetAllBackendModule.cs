using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase.Modules
{
	public interface ICheckValidity
	{
		void OnSetup(ComponentSystemBase system);
		bool IsValid(Entity              target);
	}

	public static class GetAllBackendModule
	{
		public struct AlwaysValid : ICheckValidity
		{
			public void OnSetup(ComponentSystemBase system)
			{
			}

			public bool IsValid(Entity target)
			{
				return true;
			}
		}	
	}

	public class GetAllBackendModule<T> : GetAllBackendModule<T, GetAllBackendModule.AlwaysValid>
		where T : MonoBehaviour
	{
	}

	public class GetAllBackendModule<T, TCheckValid> : BaseSystemModule
		where T : MonoBehaviour
		where TCheckValid : struct, ICheckValidity
	{
		public NativeList<ModelParent> AttachedBackendDestination;
		public NativeList<Entity>      AttachedBackendEntities;

		public NativeList<Entity> BackendWithoutModel;

		private EntityQuery        m_BackendQuery;
		public  NativeList<Entity> MissingTargets;

		public          NativeArray<Entity> TargetEntities;
		public override ModuleUpdateType    UpdateType => ModuleUpdateType.All;

		private TCheckValid m_ValidData;

		protected override void OnEnable()
		{
			m_BackendQuery             = System.EntityManager.CreateEntityQuery(typeof(T), typeof(ModelParent));
			MissingTargets             = new NativeList<Entity>(Allocator.Persistent);
			BackendWithoutModel        = new NativeList<Entity>(Allocator.Persistent);
			AttachedBackendDestination = new NativeList<ModelParent>(Allocator.Persistent);
			AttachedBackendEntities    = new NativeList<Entity>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (!TargetEntities.IsCreated)
				return;

			BackendWithoutModel.Clear();
			AttachedBackendEntities.Clear();
			AttachedBackendDestination.Clear();

			MissingTargets.Clear();
			MissingTargets.AddRange(TargetEntities);

			m_ValidData.OnSetup(System);

			if (CurrentUpdateType == ModuleUpdateType.Job)
			{
				jobHandle = new UpdateValidity
				{
					CheckAgainst = MissingTargets,
					Validity     = m_ValidData
				}.Schedule(jobHandle);
				jobHandle = new FindBackend
				{
					MissingTargets             = MissingTargets,
					BackendWithoutModel        = BackendWithoutModel,
					AttachedBackendEntities    = AttachedBackendEntities,
					AttachedBackendDestination = AttachedBackendDestination,

					Validity = m_ValidData
				}.ScheduleSingle(m_BackendQuery, jobHandle);
			}
			else if (CurrentUpdateType == ModuleUpdateType.MainThread)
			{
				new UpdateValidity
				{
					CheckAgainst = MissingTargets,
					Validity     = m_ValidData
				}.Run();
				new FindBackend
				{
					MissingTargets             = MissingTargets,
					BackendWithoutModel        = BackendWithoutModel,
					AttachedBackendEntities    = AttachedBackendEntities,
					AttachedBackendDestination = AttachedBackendDestination,

					Validity = m_ValidData
				}.Run(m_BackendQuery);
			}
		}

		protected override void OnDisable()
		{
			AttachedBackendDestination.Dispose();
			AttachedBackendEntities.Dispose();
		}

		[BurstCompile]
		private struct UpdateValidity : IJob
		{
			public TCheckValid Validity;
			public NativeList<Entity> CheckAgainst;
			
			public void Execute()
			{
				for (var i = 0; i != CheckAgainst.Length; i++)
				{
					if (Validity.IsValid(CheckAgainst[i]))
						continue;
					
					CheckAgainst.RemoveAtSwapBack(i--);
				}
			}
		}
		
		[BurstCompile]
		private struct FindBackend : IJobForEachWithEntity<ModelParent>
		{
			public TCheckValid Validity;

			public NativeList<Entity> MissingTargets;

			public NativeList<Entity>      BackendWithoutModel;
			public NativeList<Entity>      AttachedBackendEntities;
			public NativeList<ModelParent> AttachedBackendDestination;

			public void Execute(Entity entity, int index, ref ModelParent parent)
			{
				var count = MissingTargets.Length;
				for (var i = 0; i != count; i++)
					if (MissingTargets[i] == parent.Parent)
					{
						AttachedBackendEntities.Add(entity);
						AttachedBackendDestination.Add(parent);

						MissingTargets.RemoveAtSwapBack(i);
						return;
					}

				BackendWithoutModel.Add(entity);
			}
		}
	}
}