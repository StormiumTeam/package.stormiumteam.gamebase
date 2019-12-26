using System;
using System.Collections.Generic;
using GmMachine.Blocks;
using UnityEngine;

namespace GmMachine
{
	public class Machine
	{
		public Dictionary<Type, ExternalContextBase> Contexts;

		public           int                     Frame;
		private          BlockCollection         m_Collection;
		private readonly Dictionary<Guid, Block> m_GuidToBlock;

		public Machine()
		{
			m_GuidToBlock = new Dictionary<Guid, Block>();
			Contexts      = new Dictionary<Type, ExternalContextBase>();
		}

		public void SetCollection(BlockCollection collection)
		{
			m_Collection = collection;
			m_Collection.SetMachine(this);
			m_Collection.Reset();
		}

		public bool Update()
		{
			if (m_Collection == null)
				return true;

			m_Collection.Context.Machine = this;

			var state = m_Collection.Run(null);
			Frame++;
			return state;
		}

		public void Reset()
		{
			m_Collection.Reset();
			Frame = 0;
		}

		public void UpdateBlockGuid(Block block)
		{
			block.SetMachine(this);
			if (block.Guid == Guid.Empty)
				return;

			if (m_GuidToBlock.TryGetValue(block.Guid, out var otherBlock)
			    && otherBlock != block)
				Debug.LogError($"A block was found with the same GUID? [Requested '{block.Name}' T<{block}>][Other '{otherBlock.Name}' T<{otherBlock}>]");

			m_GuidToBlock[block.Guid] = block;
		}

		public TContext GetContext<TContext>()
			where TContext : ExternalContextBase
		{
			Contexts.TryGetValue(typeof(TContext), out var context);
			return (TContext) context;
		}

		public void AddContext<TContext>(TContext context)
			where TContext : ExternalContextBase
		{
			Contexts[typeof(TContext)] = context;
		}
	}

	public abstract class ExternalContextBase
	{
	}
}