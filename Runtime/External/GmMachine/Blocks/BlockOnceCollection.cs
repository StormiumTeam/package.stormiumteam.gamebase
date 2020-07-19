using System.Collections.Generic;

namespace GmMachine.Blocks
{
	public class BlockOnceCollection : BlockCollection
	{
		public BlockOnceCollection(string name) : base(name)
		{
		}

		public BlockOnceCollection(string name, List<Block> collections) : base(name, collections)
		{
		}

		protected override bool OnRun()
		{
			var collections = GetList();
			if (collections.Count == 0)
				return true;

			if (Index >= collections.Count || CurrentRunningChild == null) return true;

			RunNext(CurrentRunningChild);
			return false;
		}
	}
}