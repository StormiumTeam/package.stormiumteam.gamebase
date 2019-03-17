namespace StormiumTeam.GameBase
{
	public interface IPredictable<T>
	{
		bool VerifyPrediction(in T real);
	}
}