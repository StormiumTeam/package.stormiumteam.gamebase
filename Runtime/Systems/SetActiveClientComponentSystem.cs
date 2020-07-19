using Unity.Entities;
using Unity.NetCode;

namespace Systems
{
	public struct IsClientWorldActive : IComponentData
	{
	}

	[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
	[AlwaysUpdateSystem]
	public class SetActiveClientComponentSystem : ComponentSystem
	{
		private ClientPresentationSystemGroup m_ClientPresentationSystemGroup;

		protected override void OnCreate()
		{
			m_ClientPresentationSystemGroup = World.GetOrCreateSystem<ClientPresentationSystemGroup>();
		}

		protected override void OnUpdate()
		{
			var isEnabled = m_ClientPresentationSystemGroup.Enabled;
			switch (HasSingleton<IsClientWorldActive>())
			{
				case true:
				{
					if (isEnabled)
						return;
					EntityManager.DestroyEntity(GetSingletonEntity<IsClientWorldActive>());
					break;
				}
				case false:
				{
					if (!isEnabled)
						return;
					var ent = EntityManager.CreateEntity(typeof(IsClientWorldActive));
#if UNITY_EDITOR
					EntityManager.SetName(ent, "Active Client World");
#endif
					break;
				}
			}
		}
	}
}