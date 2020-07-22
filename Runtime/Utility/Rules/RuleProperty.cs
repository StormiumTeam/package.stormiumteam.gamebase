using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StormiumTeam.GameBase.Utility.Rules
{
	public abstract class RulePropertiesBase : IDisposable
	{
		public int                 Size;
		public ComponentSystemBase System;

		public virtual void Dispose()
		{
			OnPropertyChanged = null;
		}

		public event PropertyChangedEventHandler OnPropertyChanged;

		public void InvokePropertyChanged(PropertyChangedEventArgs args)
		{
			OnPropertyChanged?.Invoke(this, args);
		}

		public abstract object GetDataObject();
		public abstract void   SetDataObject(object data);
	}

	public class RuleProperties<TData> : RulePropertiesBase
		where TData : struct, IComponentData
	{
		public List<Property> Properties = new List<Property>();

		public override object GetDataObject()
		{
			return System.GetSingleton<TData>();
		}

		public override void SetDataObject(object data)
		{
			if (data is TData cast)
			{
				System.SetSingleton(cast);
			}
			else
			{
				if (data is JObject jsonObject)
				{
					System.SetSingleton(jsonObject.ToObject<TData>());
					return;
				}

				throw new Exception($"Invalid cast! {data.GetType()} -> {typeof(TData)}");
			}
		}

		public Property<TValue> Add<TValue>(Expression<Func<TData, TValue>> expression)
			where TValue : struct
		{
			return Add(null, expression);
		}

		public Property<TValue> Add<TValue>(string name, Expression<Func<TData, TValue>> expression)
			where TValue : struct
		{
			var memberInfo = ((MemberExpression) expression.Body).Member;
			if (memberInfo is FieldInfo fieldInfo)
			{
				if (string.IsNullOrEmpty(name))
					name = fieldInfo.Name;

				var property = new Property<TValue> {Name = name, WriteOffset = UnsafeUtility.GetFieldOffset(fieldInfo), Base = this};
				Properties.Add(property);

				Size += UnsafeUtility.SizeOf<TValue>();

				return property;
			}

			throw new NotImplementedException("Only members that are field are supported for now.");
		}

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

		public abstract class Property
		{
			protected internal RuleProperties<TData> Base;

			public string Name;
			public int    WriteOffset;

			public abstract Type Type { get; }

			public abstract object GetValue();
			public abstract void   SetValue(object v);
		}

		public class Property<T> : Property
			where T : struct
		{
			public delegate bool OnVerifyDelegate(ref T value);

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
					if (OnVerify != null && !OnVerify.Invoke(ref value))
						return;

					var data = Base.System.GetSingleton<TData>();
					var ptr  = new IntPtr(UnsafeUtility.AddressOf(ref data));

					UnsafeUtility.CopyStructureToPtr(ref value, (void*) (ptr + WriteOffset));

					Base.System.SetSingleton(data);
					Base.InvokePropertyChanged(new PropertyChangedEventArgs(Name));
				}
			}

			public event OnVerifyDelegate OnVerify;

			public override object GetValue()
			{
				return Value;
			}

			public override void SetValue(object v)
			{
				Value = (T) v;
			}
		}
	}
}