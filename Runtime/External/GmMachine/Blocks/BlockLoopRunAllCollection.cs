using System.Collections.Generic;

namespace GmMachine.Blocks
{
	/// <summary>
	///     It's a bit similar to an instruction block, but it can be waited.
	/// </summary>
	public class BlockLoopRunAllCollection : BlockCollection
	{
		public BlockLoopRunAllCollection(string name) : base(name)
		{
		}

		public BlockLoopRunAllCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

		protected override bool OnRun()
		{
			var state = true;
			foreach (var block in this)
			{
				CurrentRunningChild = block;
				BeforeChildIsRunning();
				{
					if (!block.Run(this))
						state = false;
				}
				AfterChildIsRunning();
			}

			return state;
		}
	}
}