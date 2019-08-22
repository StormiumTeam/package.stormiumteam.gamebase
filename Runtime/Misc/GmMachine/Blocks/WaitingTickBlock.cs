using GmMachine;
using StormiumTeam.GameBase;

namespace Misc.GmMachine.Blocks
{
	public interface ITickGetter
	{
		UTick GetTick();
	}

	public class WaitingTickBlock : Block
	{
		public ITickGetter TickGetter;
		public UTick       Target;

		public WaitingTickBlock(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			return TickGetter.GetTick() > Target;
		}

		public void SetTicksFromMs(long ms)
		{
			Target = UTick.AddMsNextFrame(TickGetter.GetTick(), ms);
		}
	}
}