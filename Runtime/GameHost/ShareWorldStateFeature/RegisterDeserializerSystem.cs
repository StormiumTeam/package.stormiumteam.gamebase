using System;
using System.Collections.Generic;
using GameHost.Native;
using Unity.Entities;
using UnityEngine;

namespace GameHost.ShareSimuWorldFeature
{
	public class RegisterDeserializerSystem : SystemBase
	{
		internal Dictionary<Key__, (ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer deserializer)> deserializerMap;

		public RegisterDeserializerSystem()
		{
			deserializerMap = new Dictionary<Key__, (ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer deserializer)>();
		}

		protected override void OnUpdate()
		{
		}

		public void Register(ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer componentDeserializer)
		{
			var types = attach.RegisterTypes();
			foreach (var type in types)
			{
				deserializerMap[new Key__(componentDeserializer.Size, CharBufferUtility.Create<CharBuffer256>(type))] = (attach, componentDeserializer);
				Debug.Log($"{componentDeserializer.Size}, {type}, {componentDeserializer}");
			}
		}

		public (ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer deserializer) Get(int size, CharBuffer256 name)
		{
			deserializerMap.TryGetValue(new Key__(size, name), out var tuple);
			if (tuple.deserializer.Size != size)
				throw new InvalidOperationException($"Size mismatch {size}<>{tuple.deserializer.Size}");
			
			return tuple;
		}

		public void AttachArchetype(ref ReceiveSimulationWorldSystem.Archetype__ archetype, in Dictionary<CharBuffer256, ComponentTypeDetails> detailMap)
		{
			foreach (var (attach, _) in deserializerMap.Values)
				if (attach.CanAttachToArchetype(archetype.ComponentTypes.Reinterpret<GhComponentType>(), detailMap))
					archetype.Attaches.Add(attach);
		}

		internal readonly struct Key__ : IEquatable<Key__>
		{
			public readonly int           Size;
			public readonly CharBuffer256 Name;

			public Key__(int size, CharBuffer256 name)
			{
				Size = size;
				Name = name;
			}

			public bool Equals(Key__ other)
			{
				return Size == other.Size && Name.Span.SequenceEqual(other.Name.Span);
			}

			public override bool Equals(object obj)
			{
				return obj is Key__ other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Size * 397) ^ Name.GetHashCode();
				}
			}
		}
	}
}