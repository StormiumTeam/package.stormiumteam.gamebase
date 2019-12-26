using System;
using Unity.Entities;

namespace StormiumTeam.GameBase
{
	[Flags]
	public enum GameType
	{
		Uninitialized = 0,
		Client        = 1,
		Server        = 2,
		Global        = 3
	}

	public class GameManager : BaseComponentSystem
	{
		private Entity   client;
		public  GameType GameType { get; private set; }

		public EntityModelManager EntityModelManager { get; private set; }

		protected override void OnCreate()
		{
			EntityModelManager = World.GetExistingSystem<EntityModelManager>();
		}

		protected override void OnUpdate()
		{
		}

		[Obsolete]
		public Entity SpawnLocal(ModelIdent ident, bool assignAuthority = true)
		{
			var entity = EntityModelManager.SpawnEntity(ident.Value, default);
			if (assignAuthority && !EntityManager.HasComponent<EntityAuthority>(entity)) EntityManager.AddComponent(entity, typeof(EntityAuthority));

			return entity;
		}

		public void SetGameAs(GameType gameType)
		{
			if (gameType != GameType.Uninitialized)
				throw new Exception("The game type was already set to " + gameType);

			GameType = gameType;
		}
	}
}