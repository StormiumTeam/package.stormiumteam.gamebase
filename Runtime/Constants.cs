namespace StormiumTeam.GameBase
{
	public static class Constants
	{
		public const int CollisionMask = (1 << SolidShape.HitLayer) | (1 << CustomShape.HitLayer);
		public const int NoCollision   = 30;
	}
}