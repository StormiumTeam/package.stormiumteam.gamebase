using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameBase.Roles.Components
{
	public readonly struct Owner : IComponentData
	{
		public readonly Entity Target;

		public Owner(Entity target)
		{
			Target = target;
		}

#if UNITY_5_3_OR_NEWER
		public class ValueDeserializer : IValueDeserializer<Owner>
		{
			public int Size => UnsafeUtility.SizeOf<GhGameEntity>();

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghToUnityEntity, ref Owner component, ref DataBufferReader reader)
			{
				ghToUnityEntity.TryGetValue(reader.ReadValue<GhGameEntity>(), out var unityEntity);
				component = new Owner(unityEntity);
			}
		}
#endif

		public class Register : RegisterGameHostComponentData<Owner>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<Owner, ValueDeserializer>();
		}
	}
}