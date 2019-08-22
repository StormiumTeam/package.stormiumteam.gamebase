using System;
using System.Collections.Generic;

namespace GmMachine.Blocks.Instructions
{
	public class InstructionCollection : BlockCollection, IResetCollectionOnBeginning
	{
		public bool ResetOnBeginning { get; set; }
		
		public InstructionCollection(string name) : base(name)
		{
		}

		public InstructionCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

		protected override bool OnRun()
		{
			if (ResetOnBeginning)
				Reset();
			
			foreach (var block in this)
			{
				CurrentRunningChild = block;
				BeforeChildIsRunning();
				{
					if (!block.Run(this))
					{
						throw new Exception($"The child '{CurrentRunningChild.Name}' is expecting to run in multiple frame. This is not accepted.");
					}
				}
				AfterChildIsRunning();
			}

			return true;
		}

		protected override void BeforeChildIsRunning()
		{
			base.BeforeChildIsRunning();
			CurrentRunningChild.Context.IsInstruction = true;
		}
	}
}