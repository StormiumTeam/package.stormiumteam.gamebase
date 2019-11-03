using System;
using Unity.Profiling;
using UnityEngine;

namespace GmMachine
{
	public class Block
	{
		private string m_Name;
		
		public Context Context;
		
		public Block Executor => Context.Parent;

		public Guid Guid;

		public string Name
		{
			get => m_Name;
			set
			{
				if (m_Name != value)
				{
					Debug.Log($"Update New Marker {m_Name}");
					m_Marker = new ProfilerMarker(value);
				}
				m_Name = value;
			}
		}

		private ProfilerMarker m_Marker;

		public Block()
		{
			Name = "Empty Block";
		}
		
		public Block(string name)
		{
			Name = name;
		}

		public bool Run(Block executor)
		{
			using (m_Marker.Auto())
			{
				if (executor != null)
				{
					Context.Machine = executor.Context.Machine;
					Context.Parent  = executor;
				}
				// The executor may be null if this method is directly called from the machine
				else
				{
					// We don't set Machine to null as it's important.
					Context.Parent = null;
				}

				var r = OnRun();
				OnAfterRun();
				return r;
			}
		}

		public void Reset()
		{
			OnReset();
		}

		public void SetFinished()
		{
			OnFinished();
		}
		
		protected virtual bool OnRun()
		{
			return true;
		}

		protected virtual void OnAfterRun()
		{
			
		}
		
		protected virtual void OnReset()
		{
		}

		protected virtual void OnFinished()
		{
			
		}

		public virtual void SetMachine(Machine machine)
		{
			Context.Machine = machine;
		}
	}

	public struct Context
	{
		public Block   Parent;
		public Machine Machine;
		public bool    IsInstruction;

		public TContext GetExternal<TContext>()
			where TContext : ExternalContextBase
		{
			return Machine.GetContext<TContext>();
		}
	}
}