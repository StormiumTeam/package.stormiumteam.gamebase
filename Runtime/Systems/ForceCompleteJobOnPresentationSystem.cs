using Unity.NetCode;
using Unity.Entities;

namespace StormiumTeam.GameBase.Internal
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ForceCompleteJobOnPresentationEndSystem : ComponentSystem
	{
		private BeginPresentationEntityCommandBufferSystem m_BeginBarrier;
		private EndPresentationEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_BeginBarrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
			m_EndBarrier = World.GetOrCreateSystem<EndPresentationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			var buffer = m_BeginBarrier.CreateCommandBuffer();
			buffer.ShouldPlayback = true;

			buffer = m_EndBarrier.CreateCommandBuffer();
			buffer.ShouldPlayback = true;
		}
	}
}