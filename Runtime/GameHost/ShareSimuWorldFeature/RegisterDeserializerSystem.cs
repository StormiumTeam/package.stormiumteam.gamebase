using System;
using System.Collections.Generic;
using StormiumTeam.GameBase.GameHost.Simulation;
using Unity.Entities;
using UnityEngine;

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


		internal Dictionary<Key__, (ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer deserializer)> deserializerMap;

		protected override void OnCreate()
		{
			base.OnCreate();

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
				deserializerMap[new Key__(componentDeserializer.Size, type)] = (attach, componentDeserializer);
				Debug.Log($"{componentDeserializer.Size}, {type}, {componentDeserializer}");
			}
		}

		public (ICustomComponentArchetypeAttach attach, ICustomComponentDeserializer deserializer) Get(int size, string name)
		{
			deserializerMap.TryGetValue(new Key__(size, name), out var tuple);
			return tuple;
		}

		public void AttachArchetype(ref ReceiveSimulationWorldSystem.Archetype__ archetype, in Dictionary<string, ComponentTypeDetails> detailMap)
		{
			foreach (var (attach, _) in deserializerMap.Values)
			{
				if (attach.CanAttachToArchetype(archetype.ComponentTypes.Reinterpret<GhComponentType>(), detailMap)) 
					archetype.Attaches.Add(attach);
			}
		}
	}
}