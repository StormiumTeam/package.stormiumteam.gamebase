using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.shared;
using StormiumTeam.Shared;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public abstract class BaseProvider : BaseProvider<BaseProvider.NoData>
	{
		public override void SpawnLocalEntityWithArguments(NoData data, NativeList<Entity> outputEntities)
		{
			throw new NotImplementedException();
		}

		public struct NoData
		{
		}
	}

	public abstract class BaseProviderBatch<TCreateData> : BaseProvider<TCreateData>
		where TCreateData : struct
	{
		private NativeList<Entity> m_TemporaryEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_TemporaryEntity = new NativeList<Entity>(1, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_TemporaryEntity.Dispose();
		}

		public override void SpawnBatchEntitiesWithArguments(UnsafeAllocationLength<TCreateData> array, NativeList<Entity> outputEntities, NativeList<int> indices)
		{
			var naArray = new NativeArray<Entity>(array.Length, Allocator.TempJob);

			EntityManager.CreateEntity(EntityArchetypeWithAuthority, naArray);
			for (var i = 0; i != array.Length; i++)
			{
				var entity = naArray[i];
				var data   = array[i];

#if UNITY_EDITOR
				EntityManager.SetName(entity, $"{GetTypeName(GetType())} #{entity.Index}:{entity.Version}");
#endif
				SetEntityData(entity, data);

				indices.Add(i);
			}

			outputEntities.AddRange(naArray);
			naArray.Dispose();
		}

		public override void SpawnLocalEntityWithArguments(TCreateData data, NativeList<Entity> outputEntities)
		{
			var entity = EntityManager.CreateEntity(EntityArchetypeWithAuthority);

#if UNITY_EDITOR
			EntityManager.SetName(entity, $"{GetTypeName(GetType())} #{entity.Index}:{entity.Version}");
#endif
			SetEntityData(entity, data);

			outputEntities.Add(entity);
		}

		public Entity SpawnLocalEntityWithArguments(TCreateData data)
		{
			m_TemporaryEntity.Clear();
			SpawnLocalEntityWithArguments(data, m_TemporaryEntity);
			return m_TemporaryEntity[0];
		}

		public abstract void SetEntityData(Entity entity, TCreateData data);
	}

	[AlwaysUpdateSystem]
	public abstract class BaseProvider<TCreateData> : GameBaseSystem
		where TCreateData : struct
	{	
		protected NativeQueue<TCreateData> CreateEntityDelayed;

		private bool m_CanHaveDelayedEntities;
		private List<EntityCommandBuffer> m_DelayedEntityCommandBuffers;

		private ComponentType[]    m_EntityComponents;
		private GameManager        m_GameManager;
		private ModelIdent         m_ModelIdent;
		private EntityModelManager m_ModelManager;

		private JobHandle m_ProducerHandle;

		public ComponentType[] EntityComponents             => m_EntityComponents;
		public EntityArchetype EntityArchetype              { get; protected set; }
		public EntityArchetype EntityArchetypeWithAuthority { get; protected set; }

		protected static string GetTypeName(Type type)
		{
			return TypeUtility.SpecifiedTypeName(type);
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			if (m_CanHaveDelayedEntities = typeof(TCreateData) != typeof(BaseProvider.NoData))
			{
				CreateEntityDelayed = new NativeQueue<TCreateData>(Allocator.Persistent);
				m_DelayedEntityCommandBuffers = new List<EntityCommandBuffer>();
			}

			GetManager();
		}

		protected override void OnUpdate()
		{
			if (m_CanHaveDelayedEntities)
				FlushDelayedEntities();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (m_CanHaveDelayedEntities)
			{
				CreateEntityDelayed.Dispose();
				foreach (var buffer in m_DelayedEntityCommandBuffers)
					buffer.Dispose();
			}
		}

		public EntityCommandBuffer CreateEntityCommandBuffer()
		{
			var ecb = new EntityCommandBuffer(Allocator.TempJob);
			m_DelayedEntityCommandBuffers.Add(ecb);

			return ecb;
		}

		public NativeQueue<TCreateData> GetEntityDelayedStream()
		{
			if (!m_CanHaveDelayedEntities)
				throw new NotImplementedException();

			return CreateEntityDelayed;
		}

		public void AddJobHandleForProducer(JobHandle inputDeps)
		{
			m_ProducerHandle = JobHandle.CombineDependencies(m_ProducerHandle, inputDeps);
		}

		public unsafe void FlushDelayedEntities()
		{
			m_ProducerHandle.Complete();

			foreach (var buffer in m_DelayedEntityCommandBuffers)
			{
				if (!buffer.IsCreated)
					return;

				try
				{
					buffer.Playback(EntityManager);
				}
				catch (Exception ex)
				{
					Debug.LogException(new Exception("Error on ECB playback:", ex));
				}
				finally
				{
					buffer.Dispose();
				}
			}
			m_DelayedEntityCommandBuffers.Clear();

			var itemCount = CreateEntityDelayed.Count;
			if (itemCount == 0)
				return;

			var output  = new NativeList<Entity>(itemCount, Allocator.Temp);
			var indices = new NativeList<int>(itemCount, Allocator.Temp);
			
			var items = new NativeArray<TCreateData>(itemCount, Allocator.Temp);
			var i = 0;
			while (CreateEntityDelayed.TryDequeue(out var item))
			{
				items[i++] = item;
			}
			SpawnBatchEntitiesWithArguments(new UnsafeAllocationLength<TCreateData>((void*) items.GetUnsafePtr(), items.Length), output, indices);
			items.Dispose();
			
			output.Dispose();
			indices.Dispose();
		}

		public EntityModelManager GetManager()
		{
			if (m_ModelManager == null)
			{
				m_ModelManager = World.GetOrCreateSystem<EntityModelManager>();
				m_GameManager  = World.GetOrCreateSystem<GameManager>();

				GetComponents(out m_EntityComponents);
				if (EntityComponents == null)
				{
					m_ModelIdent = m_ModelManager.Register
					(
						$"EntityProvider.{GetTypeName(GetType())}", SpawnEntity, DestroyEntity
					);
				}
				else
				{
					// todo: I was lazy when making this, this should be remade as it's slow
					var l = new Dictionary<ComponentType, byte> {[ComponentType.ReadWrite<ModelIdent>()] = 0};

					// Remove duplicates and add ModelIdent component
					if (m_EntityComponents != null)
						foreach (var c in m_EntityComponents)
							l[c] = 0;
					m_EntityComponents = l.Keys.ToArray();
					l.Clear();

					l = new Dictionary<ComponentType, byte> {[ComponentType.ReadWrite<ModelIdent>()] = 0};

					var foreignList = new List<ComponentType>();
					foreach (var c in foreignList)
						l[c] = 0;

					EntityArchetype              = EntityManager.CreateArchetype(EntityComponents);
					EntityArchetypeWithAuthority = EntityManager.CreateArchetype(EntityComponents.Append(ComponentType.ReadWrite<EntityAuthority>()).ToArray());

					var patternName = $"EntityProvider.Full.{GetTypeName(GetType())}";
					m_ModelIdent = m_ModelManager.RegisterFull
					(
						patternName + ".Model", EntityComponents, ProviderSpawnEntity, ProviderDestroyEntity
					);
				}
			}

			return m_ModelManager;
		}

		public ModelIdent GetModelIdent()
		{
			return m_ModelIdent;
		}

		protected virtual Entity SpawnEntity(Entity origin)
		{
			return EntityManager.CreateEntity(EntityArchetype);
		}

		protected virtual void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}

		public Entity ProviderSpawnEntity(Entity origin)
		{
			return SpawnEntity(origin);
		}

		public void ProviderDestroyEntity(Entity worldEntity)
		{
			DestroyEntity(worldEntity);
		}

		public virtual void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = null;
		}

		public virtual void SpawnBatchEntitiesWithArguments(UnsafeAllocationLength<TCreateData> array, NativeList<Entity> outputEntities, NativeList<int> indices)
		{
			var count = array.Length;
			for (var i = 0; i != count; i++)
			{
				var item = array[i];
				SpawnLocalEntityWithArguments(item, outputEntities);

				for (var j = 0; j != outputEntities.Length; j++) indices.Add(i);
			}
		}

		public abstract void SpawnLocalEntityWithArguments(TCreateData data, NativeList<Entity> outputEntities);

		public virtual Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
		{
			var e = entityCommandBuffer.CreateEntity(EntityArchetype);
			entityCommandBuffer.SetComponent(e, GetModelIdent());

			return e;
		}

		public Entity SpawnLocal()
		{
			return m_GameManager.SpawnLocal(GetModelIdent());
		}
	}
}