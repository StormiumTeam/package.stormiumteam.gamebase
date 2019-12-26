using Revolution;
using Unity.NetCode;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Utilities;

namespace StormiumTeam.GameBase.Data
{
	public unsafe struct ExecutingServerMap : IReadWriteComponentSnapshot<ExecutingServerMap>, ISnapshotDelta<ExecutingServerMap>
	{
		public NativeString512 Key;

		public void WriteTo(DataStreamWriter writer, ref ExecutingServerMap baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedStringDelta(Key, baseline.Key, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref ExecutingServerMap baseline, DeserializeClientData jobData)
		{
			Key = reader.ReadPackedStringDelta(ref ctx, baseline.Key, jobData.NetworkCompressionModel);
		}

		public bool DidChange(ExecutingServerMap baseline)
		{
			return !baseline.Key.Equals(Key);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSync : MixedComponentSnapshotSystemDelta<ExecutingServerMap>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
	public class ServerSynchronizeMap : ComponentSystem
	{
		private EntityQuery m_ExecutingMapQuery;
		private Entity      m_ServerMapEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ExecutingMapQuery     = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapEntity = EntityManager.CreateEntity(typeof(ExecutingServerMap), typeof(GhostEntity));
		}

		protected override void OnUpdate()
		{
			if (m_ExecutingMapQuery.IsEmptyIgnoreFilter)
				return;

			var entity  = m_ExecutingMapQuery.GetSingletonEntity();
			var currMap = EntityManager.GetComponentData<ExecutingMapData>(entity);

			EntityManager.SetComponentData(m_ServerMapEntity, new ExecutingServerMap {Key = currMap.Key});
		}
	}
}