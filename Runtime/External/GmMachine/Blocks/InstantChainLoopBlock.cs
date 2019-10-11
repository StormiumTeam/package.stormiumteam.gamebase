using System.Collections.Generic;
using UnityEngine;

namespace GmMachine.Blocks
{
	/// <summary>
	/// Similar to <see cref="BlockAutoLoopCollection"/> but blocks that are executed immediately will be chained
	/// </summary>
	public class InstantChainLoopBlock : BlockCollection, IResetCollectionOnBeginning
	{
		public bool ResetOnBeginning { get; set; }

		public InstantChainLoopBlock(string name) : base(name)
		{
		}

		public InstantChainLoopBlock(string name, List<Block> collections) : base(name, collections)
		{
		}

		protected override bool OnRun()
		{
			var collections = GetList();
			if (CurrentRunningChild == null || Index + 1 >= collections.Count)
			{
				// If we automatically reset the loop, there is no need to further instructions as they're basically the same
				if (ResetOnBeginning)
					Reset();
				else
				{
					Index               = 0;
					CurrentRunningChild = collections[0];
				}
			}

			var iter = 0;
			while (CurrentRunningChild != null && iter++ < 512)
			{
				BeforeChildIsRunning();
				{
					if (!CurrentRunningChild.Run(this))
						return false;
					CurrentRunningChild.SetFinished();
					NextChildBlock();
				}
				AfterChildIsRunning();
			}

			if (iter >= 500)
			{
				Debug.Log($"Crashed on block: {CurrentRunningChild.Name} (parent: {Name})");
			}

			return true;
		}
	}
}