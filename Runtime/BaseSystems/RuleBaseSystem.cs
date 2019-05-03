using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Runtime.BaseSystems
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
			public abstract void   SetValue(object                v);
			public abstract void   WriteToBuffer(DataBufferWriter dataBuffer);
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

			public override void WriteToBuffer(DataBufferWriter dataBuffer)
			{
				var marker = new DataBufferMarker(WriteOffset);

				dataBuffer.WriteValue(Value, marker);
			}
		}

		public List<Property>      Properties = new List<Property>();
		public int                 Size;
		public ComponentSystemBase System;

		public unsafe Property<TValue> Add<TValue>(string name, ref TData data, ref TValue value)
			where TValue : struct
		{
			var dataPtr = new IntPtr(UnsafeUtility.AddressOf(ref data));
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

	[UpdateInGroup(typeof(RuleSystemGroup))]
	public abstract class RuleSystemGroupBase : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}

		public virtual void Process()
		{
			base.OnUpdate();
		}
	}

	public class GameEventRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public class PhysicsFilterRuleSystemGroup : RuleSystemGroupBase
	{
	}

	public abstract class RuleBaseSystem : GameBaseSystem
	{
		public List<RulePropertiesBase> PropertiesCollection;

		public virtual string Name        => "NoName";
		public virtual string Description => "NoDescription";

		protected override void OnCreate()
		{
			base.OnCreate();
			
			PropertiesCollection = new List<RulePropertiesBase>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

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