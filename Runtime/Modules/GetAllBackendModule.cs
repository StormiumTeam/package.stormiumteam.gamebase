using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase.Modules
{
	public class GetAllBackendModule<T> : BaseSystemModule
		where T : MonoBehaviour
	{
		public NativeList<ModelParent> AttachedBackendDestination;
		public NativeList<Entity>      AttachedBackendEntities;

		public NativeList<Entity> BackendWithoutModel;

		private EntityQuery        m_BackendQuery;
		public  NativeList<Entity> MissingTargets;

		public          NativeArray<Entity> TargetEntities;
		public override ModuleUpdateType    UpdateType => ModuleUpdateType.Job;

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

			jobHandle = new FindBackend
			{
				MissingTargets             = MissingTargets,
				BackendWithoutModel        = BackendWithoutModel,
				AttachedBackendEntities    = AttachedBackendEntities,
				AttachedBackendDestination = AttachedBackendDestination
			}.ScheduleSingle(m_BackendQuery, jobHandle);
		}

		protected override void OnDisable()
		{
			AttachedBackendDestination.Dispose();
			AttachedBackendEntities.Dispose();
		}

		[BurstCompile]
		private struct FindBackend : IJobForEachWithEntity<ModelParent>
		{
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