using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StormiumTeam.GameBase.Roles.Components
{
	/// <summary>
	///     A relative path to a <see cref="Entity" /> with <see cref="IEntityDescription" />
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
		public struct ValueDeserializer : IValueDeserializer<Relative<TDescription>>
		{
			public int Size => UnsafeUtility.SizeOf<GhGameEntitySafe>();

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghToUnityEntity, ref Relative<TDescription> component, ref DataBufferReader reader)
			{
				ghToUnityEntity.TryGetValue(reader.ReadValue<GhGameEntitySafe>(), out var unityEntity);
				component = new Relative<TDescription>(unityEntity);
			}
		}
#endif

		public abstract class Register : RegisterGameHostComponentData<Relative<TDescription>>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => BurstKnowDeserializer();

			public abstract ICustomComponentDeserializer BurstKnowDeserializer();
		}
	}
}