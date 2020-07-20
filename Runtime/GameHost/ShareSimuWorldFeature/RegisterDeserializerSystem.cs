using System;
using System.Collections.Generic;
using Unity.Entities;

namespace DefaultNamespace
{
	public class RegisterDeserializerSystem : SystemBase
	{
		internal readonly struct Key__ : IEquatable<Key__>
		{
			public readonly int    Size;
			public readonly string Name;

			public Key__(int size, string name)
			{
				Size = size;
				Name = name;
			}

			public bool Equals(Key__ other)
			{
				return Size == other.Size && Name == other.Name;
			}

			public override bool Equals(object obj)
			{
				return obj is Key__ other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Size * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				}
			}
		}

		internal Dictionary<Key__, ICustomComponentDeserializer> deserializerMap;

		protected override void OnCreate()
		{
			base.OnCreate();

			deserializerMap = new Dictionary<Key__, ICustomComponentDeserializer>();
		}

		protected override void OnUpdate()
		{

		}

		public void Register(string type, ICustomComponentDeserializer componentDeserializer)
		{
			deserializerMap[new Key__(componentDeserializer.Size, type)] = componentDeserializer;
		}

		public ICustomComponentDeserializer GetDeserializer(int size, string name)
		{
			if (deserializerMap.TryGetValue(new Key__(size, name), out var deserializer))
				return deserializer;
			return null;
		}
	}
}