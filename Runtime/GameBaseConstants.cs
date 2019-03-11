namespace StormiumTeam.GameBase
{
	public static class GameBaseConstants
	{
		public const int CollisionMask = (1 << SolidShape.HitLayer) | (1 << CustomShape.HitLayer);
		public const int NoCollision   = 30;
	}
}