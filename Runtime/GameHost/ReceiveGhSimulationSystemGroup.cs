using Unity.Entities;

namespace GameHost
{
	public class BeforeFirstFrameGhSimulationSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}

		public void ForceUpdate()
		{
			base.OnUpdate();
		}
	}
	
	public class ReceiveFirstFrameGhSimulationSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}

		public void ForceUpdate()
		{
			base.OnUpdate();
		}
	}

	public class ReceiveGhSimulationSystemGroup : ComponentSystemGroup
	{
		public int Index { get; set; }

		protected override void OnUpdate()
		{
		}

		public void ForceUpdate()
		{
			base.OnUpdate();
		}
	}

	public class ReceiveLastFrameGhSimulationSystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
		}

		public void ForceUpdate()
		{
			base.OnUpdate();
		}
	}
}