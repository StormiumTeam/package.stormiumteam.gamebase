using Unity.Entities;

namespace StormiumTeam.GameBase.Bootstrapping
{
	public abstract class BaseBootstrapSystem : ComponentSystem
	{
		private EntityQuery m_Query;
		public  Entity      Bootstrap { get; private set; }

		protected abstract void Register(Entity bootstrap);
		protected abstract void Match(Entity    bootstrapSingleton);

		protected override void OnCreate()
		{
			base.OnCreate();

			Bootstrap = EntityManager.CreateEntity(typeof(BootstrapComponent));
			Register(Bootstrap);
#if UNITY_EDITOR
			EntityManager.SetName(Bootstrap, $"Bootstrap '{EntityManager.GetComponentData<BootstrapComponent>(Bootstrap).Name}'");
#endif

			m_Query = GetEntityQuery(typeof(TargetBootstrap));
		}

		protected override void OnUpdate()
		{
			m_Query.SetSharedComponentFilter(new TargetBootstrap {Value = Bootstrap});
			if (m_Query.CalculateEntityCount() != 1)
				return;

			Match(m_Query.GetSingletonEntity());
		}
	}
}