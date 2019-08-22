using System.Collections.Generic;

namespace GmMachine.Blocks
{
	public class BlockAutoLoopCollection : BlockCollection
	{
		public bool DoResetOnLoopEnd;
		
		public BlockAutoLoopCollection(string name) : base(name)
		{
		}

		public BlockAutoLoopCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

		protected override bool OnRun()
		{
			var collections = GetList();
			if (collections.Count == 0)
				return true;

			if (Index + 1 >= collections.Count)
			{
				// If we automatically reset the loop, there is no need to further instructions as they're basically the same
				if (DoResetOnLoopEnd)
					Reset();
				else
				{
					Index = 0;
					NextChildBlock(collections[0]);
				}
			}

			RunNext(CurrentRunningChild);
			return false;
		}
	}
}