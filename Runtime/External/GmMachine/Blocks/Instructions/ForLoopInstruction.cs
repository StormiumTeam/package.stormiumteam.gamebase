using System.Collections.Generic;

namespace GmMachine.Blocks.Instructions
{
	public class ForLoopInstruction : InstructionCollection
	{
		public int Loop, Count;
		
		public ForLoopInstruction(string name, int count, List<Block> collections) : base(name, collections)
		{
			Count = count;
		}

		protected override void OnReset()
		{
			base.OnReset();

			Loop = 0;
		}

		protected override bool OnRun()
		{
			if (Loop >= Count)
				return true;

			while (Loop < Count)
			{
				base.OnRun();
				Loop++;
			}

			return true;
		}
	}
}