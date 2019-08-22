using System;
using System.Collections.Generic;
using UnityEngine;

namespace GmMachine.Blocks
{
	public abstract class BlockCollection : Block
	{
		public Block CurrentRunningChild { get; protected set; }

		public int Index { get; set; }
		
		private List<Block>               m_Collections;
		private Dictionary<string, Block> m_NameToBlock;
		
		protected BlockCollection(string name) : base(name)
		{
			SetCollection(new List<Block>());
		}

		protected BlockCollection(string name, List<Block> collections) : base(name)
		{
			SetCollection(collections);
		}

		protected virtual void BeforeChildIsRunning()
		{
		}

		protected virtual void AfterChildIsRunning()
		{
		}

		public void SetCollection(List<Block> collections)
		{
			m_Collections = collections;
			RegenerateMetaData();
		}

		public void Add(Block block)
		{
			m_Collections.Add(block);
			RegenerateMetaData();
		}

		public bool RunNext(Block childTarget)
		{
			var currChild = CurrentRunningChild;
			if (currChild != childTarget)
				return false;

			CurrentRunningChild = currChild;
			if (CurrentRunningChild == null)
				return false;

			// go to next
			BeforeChildIsRunning();
			if (currChild.Run(this))
			{
				AfterChildIsRunning();
				NextChildBlock();
				currChild.SetFinished();
			}

			return true;
		}

		public bool IsTarget(Block childTarget)
		{
			return CurrentRunningChild == childTarget;
		}

		protected virtual void NextChildBlock(Block target = null)
		{
			var nextBlock = target;

			if (nextBlock == null && m_Collections.Count >= Index + 2)
			{
				Index++;
				nextBlock = m_Collections[Index];
			}

			CurrentRunningChild = nextBlock;
		}

		protected override void OnFinished()
		{
			OnReset();
		}

		protected override void OnReset()
		{
			Index = 0;

			if (m_Collections.Count > 0)
				CurrentRunningChild = m_Collections[0];

			foreach (var block in m_Collections)
			{
				try
				{
					block.Reset();
				}
				catch (Exception ex)
				{
					Debug.LogError($"[BC '{Name}'] Couldn't reset block '{block?.Name}'");
					throw;
				}
			}
		}

		public Block GetBlock(string blockName)
		{
			if (m_NameToBlock == null || m_NameToBlock.Count != m_Collections.Count)
			{
				RegenerateDictionary();
			}

			m_NameToBlock.TryGetValue(blockName, out var block);
			return block;
		}

		public override void SetMachine(Machine machine)
		{
			base.SetMachine(machine);
			foreach (var block in m_Collections)
				block.SetMachine(machine);
		}

		private void RegenerateMetaData()
		{
			RegenerateDictionary();
			if (Context.Machine != null)
			{
				foreach (var block in m_Collections)
				{
					if (block == null)
						throw new InvalidOperationException("Null block");
					
					Context.Machine.UpdateBlockGuid(block);
				}
			}
		}

		private void RegenerateDictionary()
		{
			if (m_NameToBlock == null)
				m_NameToBlock = new Dictionary<string, Block>();
			m_NameToBlock.Clear();
			foreach (var block in m_Collections)
			{
				m_NameToBlock[block.Name] = block;
			}
		}

		public List<Block>.Enumerator GetEnumerator()
		{
			return m_Collections.GetEnumerator();
		}

		protected List<Block> GetList()
		{
			return m_Collections;
		}
	}
}