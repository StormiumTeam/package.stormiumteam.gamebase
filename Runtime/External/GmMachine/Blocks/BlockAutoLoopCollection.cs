using System.Collections.Generic;

namespace GmMachine.Blocks
{
	public class BlockAutoLoopCollection : BlockCollection, IResetCollectionOnBeginning, IBreakCollection
	{
		private bool m_ShouldBreak;
		public  bool SkipWhenEnded;

		public BlockAutoLoopCollection(string name) : base(name)
		{
		}

		public BlockAutoLoopCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

		public void Break()
		{
			m_ShouldBreak = true;
		}

		public bool ResetOnBeginning { get; set; }

		protected override bool OnRun()
		{
			if (m_ShouldBreak)
			{
				m_ShouldBreak = false;
				Reset();

				return true;
			}

			var collections = GetList();
			if (collections.Count == 0)
				return true;

			if (Index >= collections.Count)
			{
				// If we automatically reset the loop, there is no need to further instructions as they're basically the same
				if (ResetOnBeginning)
				{
					Reset();
				}
				else
				{
					Index               = 0;
					CurrentRunningChild = collections[0];
				}

				if (SkipWhenEnded)
					return true;
			}

			RunNext(CurrentRunningChild);
			return false;
		}
	}
}