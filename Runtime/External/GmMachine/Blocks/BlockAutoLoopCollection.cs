using System.Collections.Generic;

namespace GmMachine.Blocks
{
	public class BlockAutoLoopCollection : BlockCollection, IResetCollectionOnBeginning
	{
		public bool ResetOnBeginning { get; set; }
		public bool SkipWhenEnded;

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

			if (Index >= collections.Count)
			{
				// If we automatically reset the loop, there is no need to further instructions as they're basically the same
				if (ResetOnBeginning)
					Reset();
				else
				{
					Index = 0;
					NextChildBlock(collections[0]);
				}

				if (SkipWhenEnded)
					return true;
			}

			RunNext(CurrentRunningChild);
			return false;
		}
	}
}