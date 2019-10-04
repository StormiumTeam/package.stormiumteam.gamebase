using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace StormiumTeam.GameBase.BaseSystems
{
	public abstract class RulePropertiesBase : IDisposable
	{
		public virtual void Dispose()
		{
		}
	}

	public class RuleProperties<TData> : RulePropertiesBase
		where TData : struct, IComponentData
	{
		public abstract class Property
		{
			protected internal RuleProperties<TData> Base;

			public abstract Type Type { get; }

			public string Name;
			public int    WriteOffset;

			public abstract object GetValue();
			public abstract void   SetValue(object v);
		}

		public class Property<T> : Property
			where T : struct
		{
			public override Type Type => typeof(T);

			public unsafe T Value
			{
				get
				{
					var data = Base.System.GetSingleton<TData>();
					var ptr  = new IntPtr(UnsafeUtility.AddressOf(ref data));

					UnsafeUtility.CopyPtrToStructure<T>((void*) (ptr + WriteOffset), out var value);

					return value;
				}
				set
				{
					var data = Base.System.GetSingleton<TData>();
					var ptr  = new IntPtr(UnsafeUtility.AddressOf(ref data));

					UnsafeUtility.CopyStructureToPtr(ref value, (void*) (ptr + WriteOffset));

					Base.System.SetSingleton(data);
				}
			}

			public override object GetValue()
			{
				return Value;
			}

			public override void SetValue(object v)
			{
				Value = (T) v;
			}
		}

		public List<Property>      Properties = new List<Property>();
		public int                 Size;
		public ComponentSystemBase System;

		public unsafe Property<TValue> Add<TValue>(string name, ref TData data, ref TValue value)
			where TValue : struct
		{
			var dataPtr  = new IntPtr(UnsafeUtility.AddressOf(ref data));
			var valuePtr = new IntPtr(UnsafeUtility.AddressOf(ref value));

			var writeOffset = IntPtr.Subtract(valuePtr, dataPtr.ToInt32()).ToInt32();
			if (writeOffset < 0)
				throw new IndexOutOfRangeException($"[{System}] AddProperty: WriteOffset is not positive (wo: {writeOffset}, dataPtr: {dataPtr}, valuePtr: {valuePtr})");

			var property = new Property<TValue> {Name = name, WriteOffset = writeOffset, Base = this};
			Properties.Add(property);

			Size += UnsafeUtility.SizeOf<TValue>();

			if (Size > UnsafeUtility.SizeOf<TData>())
				throw new IndexOutOfRangeException($"[{System}] AddProperty: We reached TData size (currSize: {Size}, TData size: {UnsafeUtility.SizeOf<TData>()}, TValue size: {UnsafeUtility.SizeOf<TValue>()})");

			return property;
		}

		public override void Dispose()
		{
			Properties.Clear();
		}
	}

	public class RuleSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}
	}

	[DisableAutoCreation]
	public class RuleSystemBarrier : EntityCommandBufferSystem
	{
	}

	[UpdateInGroup(typeof(RuleSystemGroup))]
	public abstract class RuleSystemGroupBase : ComponentSystemGroup
	{
		private RuleSystemBarrier m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<RuleSystemBarrier>();
		}

		protected override void OnUpdate()
		{
		}

		public virtual void Process()
		{
			foreach (var componentSystem in Systems)
			{
				if (!(componentSystem is RuleBaseSystem ruleSystem))
					return;

				ruleSystem.Barrier = m_Barrier;
			}

			base.OnUpdate();

			m_Barrier.Update();
		}
	}

	public class GameEventRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public class PhysicsFilterRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public abstract class RuleBaseSystem : JobGameBaseSystem
	{
		public List<RulePropertiesBase> PropertiesCollection;

		public virtual string Name        => "NoName";
		public virtual string Description => "NoDescription";

		protected internal RuleSystemBarrier Barrier { get; internal set; }

		protected void AddJobHandleForProducer(JobHandle inputDeps)
		{
			Barrier.AddJobHandleForProducer(inputDeps);
		}

		protected EntityCommandBuffer GetCommandBuffer()
		{
			return Barrier.CreateCommandBuffer();
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			PropertiesCollection = new List<RulePropertiesBase>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			Parallel.For(0, 4, (i) => { });
			PropertiesCollection.ForEach(r => r.Dispose());
		}

		protected RuleProperties<TData> AddRule<TData>(out TData data)
			where TData : struct, IComponentData
		{
			var properties = new RuleProperties<TData> {System = this};

			PropertiesCollection.Add(properties);

			if (!HasSingleton<TData>())
			{
				EntityManager.CreateEntity(typeof(TData));
				RequireSingletonForUpdate<TData>();
			}

			data = GetSingleton<TData>();

			return properties;
		}
	}
}