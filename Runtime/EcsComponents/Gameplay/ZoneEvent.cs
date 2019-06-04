using Unity.Entities;
using Unity.Mathematics;

namespace StormiumTeam.GameBase.Components
{
	public struct ZoneEvent : IComponentData
	{
		public Entity Source;
		public float3 Position;
	}

	public struct ZoneRayRadius : IComponentData
	{
		public float Value;
	}

	public struct DamageZoneEvent : IComponentData, IEventData
	{
		public int Value;
	}

	public struct BumpZoneEvent : IComponentData, IEventData
	{
		public float3 Force;
		public float3 VelocityReset;
	}
}