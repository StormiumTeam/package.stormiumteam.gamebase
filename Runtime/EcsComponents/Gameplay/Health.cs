using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
	}

	public struct HealthContainer : IBufferElementData
	{
		public Entity Target;

		public HealthContainer(Entity healthTarget)
		{
			Target = healthTarget;
		}

		public bool TargetValid()
		{
			return World.Active.GetExistingManager<EntityManager>().Exists(Target);
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
		public readonly ModifyHealthType Type;
		public readonly int  Origin;

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

	public class HealthProcessGroup : ComponentSystemGroup
	{
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
		private struct ClearLivableHealthData : IJobProcessComponentData<HealthConcreteValue, OwnerState<LivableDescription>>
		{
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

			public void Execute([ReadOnly] ref HealthConcreteValue concrete, [ReadOnly] ref OwnerState<LivableDescription> livableOwner)
			{
				LivableHealthFromEntity[livableOwner.Target] = default;
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
		private struct AssignLivableHealthData : IJobProcessComponentData<HealthConcreteValue, OwnerState<LivableDescription>>
		{
			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

			public void Execute([ReadOnly] ref HealthConcreteValue concrete, [ReadOnly] ref OwnerState<LivableDescription> livableOwner)
			{
				var health             = LivableHealthFromEntity[livableOwner.Target];
				var livableVectorized  = new int2(health.Value, health.Max);
				var concreteVectorized = new int2(concrete.Value, concrete.Max);
				var result             = livableVectorized + concreteVectorized;

				LivableHealthFromEntity[livableOwner.Target] = new LivableHealth
				{
					Value = result.x,
					Max   = result.y
				};
			}
		}

		private NativeList<ModifyHealthEvent> m_ModifyEventList;
		private ComponentGroup                m_GroupEvent;
		private ComponentGroup                m_GroupLivableBuffer;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_ModifyEventList = new NativeList<ModifyHealthEvent>(64, Allocator.Persistent);
			m_GroupEvent = GetComponentGroup(new EntityArchetypeQuery
			{
				All = new[]
				{
					ComponentType.ReadOnly<ModifyHealthEvent>(),
				}
			});
			m_GroupLivableBuffer = GetComponentGroup(typeof(HealthContainer));
		}

		private static HealthProcessGroup s_LastInstance;

		protected override void OnUpdate()
		{
			base.OnUpdate();

			s_LastInstance = this;

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
			job = new GatherEvents
			{
				ModifyEventList = m_ModifyEventList,

				ModifyHealthEventType = GetArchetypeChunkComponentType<ModifyHealthEvent>(true),
			}.Schedule(m_GroupEvent, job);

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

			Entities.WithAll<HealthDescription>().ForEach((Entity e, ref OwnerState<LivableDescription> livableOwner) =>
			{
				var buffer = s_LastInstance.EntityManager.GetBuffer<HealthContainer>(livableOwner.Target);
				if (buffer.Capacity > buffer.Length)
				{
					buffer.Add(new HealthContainer(e));
				}
				else
				{
					Debug.LogError("Out of bounds for livable owner " + livableOwner.Target);
				}
			});
			
			EntityManager.DestroyEntity(m_GroupEvent);
		}
	}

	[DisableAutoCreation]
	public abstract class HealthProcessSystem : ComponentSystem
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