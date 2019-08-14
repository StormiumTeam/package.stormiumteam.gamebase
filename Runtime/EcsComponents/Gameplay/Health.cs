using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace StormiumTeam.GameBase.Components
{
	public struct HealthConcreteValue : IComponentData
	{
		public int Value, Max;
	}

	public struct LivableHealth : IComponentData
	{
		public int Value, Max;

		// Manual variable.
		// > This value should be set by the gamemode.
		// > If it's true, no entities will be added to 
		public bool IsDead;
	}

	public struct HealthModifyingHistory : IBufferElementData
	{
		/// <summary>
		/// The entity who bring the damage
		/// </summary>
		public Entity Instigator;

		/// <summary>
		/// The modifying value (it can be negative for damage or positive for healing)
		/// </summary>
		public int Value;

		public UTick Tick;
	}

	public struct HealthContainer : IBufferElementData
	{
		public Entity Target;

		public HealthContainer(Entity healthTarget)
		{
			Target = healthTarget;
		}
	}

	public struct HealthAssetDescription : IComponentData
	{
	}

	public struct HealthDescription : IComponentData
	{
	}

	public enum ModifyHealthType
	{
		SetFixed,
		Add,
		SetMax,
		SetNone
	}

	public struct ModifyHealthEvent : IComponentData
	{
		public ModifyHealthType Type;
		public int              Origin;

		public int Consumed;

		public Entity Target;

		public ModifyHealthEvent(ModifyHealthType type, int origin, Entity target)
		{
			Type = type;

			Origin   = origin;
			Consumed = origin;

			Target = target;
		}
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class HealthProcessGroup : ComponentSystemGroup
	{
		public class BeforeGathering : ComponentSystemGroup
		{
			protected override void OnUpdate()
			{
			}

			internal void Process()
			{
				base.OnUpdate();
			}
		}

		[BurstCompile]
		private struct ClearBuffer : IJobChunk
		{
			public ArchetypeChunkBufferType<HealthContainer> HealthContainerType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var bufferAccessor = chunk.GetBufferAccessor(HealthContainerType);
				var length         = chunk.Count;

				for (var i = 0; i != length; i++)
				{
					bufferAccessor[i].Clear();
				}
			}
		}

		[BurstCompile]
		private struct ClearLivableHealthData : IJobForEach<HealthConcreteValue, Owner>
		{
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

			public void Execute([ReadOnly] ref HealthConcreteValue concrete, [ReadOnly] ref Owner owner)
			{
				if (LivableHealthFromEntity.Exists(owner.Target))
				{
					var prev = LivableHealthFromEntity[owner.Target];
					prev.Value = 0;
					prev.Max   = 0;

					LivableHealthFromEntity[owner.Target] = prev;
				}
			}
		}

		[BurstCompile]
		private struct GatherEvents : IJobChunk
		{
			[NativeDisableParallelForRestriction] // the order of execution don't matter
			public NativeList<ModifyHealthEvent> ModifyEventList;

			[ReadOnly]
			public ArchetypeChunkComponentType<ModifyHealthEvent> ModifyHealthEventType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var length = chunk.Count;

				var eventArray = chunk.GetNativeArray(ModifyHealthEventType);
				for (var i = 0; i != length; i++)
				{
					ModifyEventList.Add(eventArray[i]);
				}
			}
		}

		[BurstCompile]
		private struct AssignLivableHealthData : IJobForEach<HealthConcreteValue, Owner>
		{
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

			public void Execute([ReadOnly] ref HealthConcreteValue concrete, [ReadOnly] ref Owner owner)
			{
				// this may be possible if we are switching owners or if the owner is destroyed or if we are on clients and didn't received the owner yet
				if (!LivableHealthFromEntity.Exists(owner.Target))
					return;
				
				var health             = LivableHealthFromEntity[owner.Target];
				var livableVectorized  = new int2(health.Value, health.Max);
				var concreteVectorized = new int2(concrete.Value, concrete.Max);
				var result             = livableVectorized + concreteVectorized;

				LivableHealthFromEntity[owner.Target] = new LivableHealth
				{
					Value  = result.x,
					Max    = result.y,
					IsDead = health.IsDead
				};
			}
		}

		private NativeList<ModifyHealthEvent> m_ModifyEventList;
		private EntityQuery                   m_GroupEvent;
		private EntityQuery                   m_LivableWithoutHistory;
		private EntityQuery                   m_GroupLivableBuffer;

		private ComponentSystemGroup m_ServerSimulationSystemGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ModifyEventList = new NativeList<ModifyHealthEvent>(64, Allocator.Persistent);
			m_GroupEvent = GetEntityQuery(new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadOnly<ModifyHealthEvent>(),
				}
			});
			m_LivableWithoutHistory = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(LivableHealth)},
				None = new ComponentType[] {typeof(HealthModifyingHistory)}
			});
			m_GroupLivableBuffer = GetEntityQuery(typeof(HealthContainer));

			m_ServerSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_ModifyEventList.Dispose();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (m_LivableWithoutHistory.CalculateEntityCount() > 0)
			{
				Entities.With(m_LivableWithoutHistory).ForEach((Entity entity) =>
				{
					var history = EntityManager.AddBuffer<HealthModifyingHistory>(entity);

					history.Reserve(history.Capacity + 1);
					history.Clear();
				});
			}

			World.GetExistingSystem<BeforeGathering>().Process();

			m_ModifyEventList.Clear();

			JobHandle job = default;

			job = new ClearBuffer
			{
				HealthContainerType = GetArchetypeChunkBufferType<HealthContainer>()
			}.Schedule(m_GroupLivableBuffer, job);

			//job.Complete();

			job = new ClearLivableHealthData
			{
				LivableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>()
			}.Schedule(this, job);
			if (m_GroupEvent.CalculateEntityCount() > 0)
			{
				job = new GatherEvents
				{
					ModifyEventList = m_ModifyEventList,

					ModifyHealthEventType = GetArchetypeChunkComponentType<ModifyHealthEvent>(true),
				}.Schedule(m_GroupEvent, job);
			}

			foreach (var componentSystemBase in m_systemsToUpdate)
			{
				var system = (HealthProcessSystem) componentSystemBase;
				system.__process(ref job, m_ModifyEventList);
			}

			job = new AssignLivableHealthData
			{
				LivableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>()
			}.Schedule(this, job);

			job.Complete();

			Entities.WithAll<HealthDescription>().ForEach((Entity e, ref Owner owner) =>
			{
				if (owner.Target == default || !EntityManager.Exists(owner.Target))
				{
					Debug.LogWarning($"No health container found for {e} (target: {owner.Target})");
					return;
				}

				if (!EntityManager.HasComponent(owner.Target, typeof(HealthContainer)))
				{
					EntityManager.AddComponent(owner.Target, typeof(HealthContainer));
					Debug.LogWarning("Added 'HealthContainer' to " + owner.Target);
				}

				var buffer = EntityManager.GetBuffer<HealthContainer>(owner.Target);
				buffer.Add(new HealthContainer(e));
			});

			Entities.ForEach((Entity entity, ref LivableHealth livableHealth, DynamicBuffer<HealthModifyingHistory> history) =>
			{
				if (livableHealth.IsDead)
					history.Clear();

				while (history.Length > 32)
					history.RemoveAt(0);
			});

			if (m_GroupEvent.CalculateEntityCount() > 0)
				EntityManager.DestroyEntity(m_GroupEvent);
		}
	}

	public abstract class HealthProcessSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
		}

		protected NativeList<ModifyHealthEvent> ModifyHealthEventList;

		internal void __process(ref JobHandle jobHandle, NativeList<ModifyHealthEvent> modifyHealthEventList)
		{
			ModifyHealthEventList = modifyHealthEventList;

			jobHandle = Process(jobHandle);
		}

		protected abstract JobHandle Process(JobHandle jobHandle);
	}
}