using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StormiumTeam.GameBase.Roles.Components
{
	public readonly struct Owner : IComponentData
	{
		public readonly Entity Target;

		public Owner(Entity target)
		{
			Target = target;
		}

#if UNITY_5_3_OR_NEWER
		public struct ValueDeserializer : IValueDeserializer<Owner>
		{
			public int Size => UnsafeUtility.SizeOf<GhGameEntitySafe>();

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghToUnityEntity, ref Owner component, ref DataBufferReader reader)
			{
				ghToUnityEntity.TryGetValue(reader.ReadValue<GhGameEntitySafe>(), out var unityEntity);
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