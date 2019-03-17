namespace StormiumTeam.GameBase
{
	public interface IInterpolatable<T>
	{
		void Interpolate(in T next, float progress);
	}
}