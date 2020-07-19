using GmMachine;
using Unity.Entities;

namespace StormiumTeam.GameBase.External
{
	public class QueryBuilderContext : ExternalContextBase
	{
		public PlaceholderSystem System;

		public QueryBuilderContext(World world)
		{
			System = world.GetOrCreateSystem<PlaceholderSystem>();
		}

		public EntityQueryBuilder From => System.GetEntityQueryBuilder();

		public class PlaceholderSystem : ComponentSystem
		{
			protected override void OnUpdate()
			{
			}

			public EntityQueryBuilder GetEntityQueryBuilder()
			{
				return Entities;
			}
		}
	}
}