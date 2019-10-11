using System.Collections.Generic;
using UnityEngine;

namespace GmMachine.Blocks
{
	public class BlockAutoLoopCollection : BlockCollection, IResetCollectionOnBeginning, IBreakCollection
	{
		public bool ResetOnBeginning { get; set; }
		public bool SkipWhenEnded;


		private bool m_ShouldBreak;

		public BlockAutoLoopCollection(string name) : base(name)
		{
		}

		public BlockAutoLoopCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

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
					Reset();
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

		public void Break()
		{
			m_ShouldBreak = true;
		}
	}
}