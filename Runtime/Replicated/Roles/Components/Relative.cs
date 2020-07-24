using System;
using GameBase.Roles.Interfaces;
using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameBase.Roles.Components
{
	/// <summary>
	///     A relative path to a <see cref="Entity" /> with <see cref="Interfaces.IEntityDescription" />
	/// </summary>
	/// <typeparam name="TDescription"></typeparam>
	public readonly struct Relative<TDescription> : IComponentData
		where TDescription : IEntityDescription
	{
		/// <summary>
		///     Path to the entity
		/// </summary>
		public readonly Entity Target;

		public Relative(Entity target)
		{
			Target = target;
		}

#if UNITY_5_3_OR_NEWER
		public class ValueDeserializer : IValueDeserializer<Relative<TDescription>>
		{
			public int Size => UnsafeUtility.SizeOf<GhGameEntity>() + sizeof(uint);

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghToUnityEntity, ref Relative<TDescription> component, ref DataBufferReader reader)
			{
				ghToUnityEntity.TryGetValue(reader.ReadValue<GhGameEntity>(), out var unityEntity);
				component = new Relative<TDescription>(unityEntity);
			}
		}
#endif

		public abstract class Register : RegisterGameHostComponentData<Relative<TDescription>>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<Relative<TDescription>, ValueDeserializer>();
		}
	}
}