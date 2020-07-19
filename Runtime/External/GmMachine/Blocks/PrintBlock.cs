using System;
using UnityEngine;

namespace GmMachine.Blocks
{
	public class PrintBlock : Block
	{
		public Variable<string> Target;

		public PrintBlock(string name, Variable<string> variable) : base(name)
		{
			Target = variable;
		}

		protected override bool OnRun()
		{
			Debug.Log(Target.GetValue());
			return true;
		}
	}

	public class PrintBlock<T> : Block
		where T : IEquatable<T>
	{
		private string m_PreviousString;
		private T      m_PreviousTarget;

		public Variable<T> Target;

		public PrintBlock(string name, Variable<T> variable) : base(name)
		{
			Target = variable;
		}

		protected override bool OnRun()
		{
			var newVal = Target.GetValue();
			if (!m_PreviousTarget.Equals(newVal))
			{
				m_PreviousTarget = newVal;
				m_PreviousString = newVal.ToString();
			}

			Debug.Log(m_PreviousString);
			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_PreviousTarget = Target.GetValue();
			m_PreviousString = m_PreviousTarget.ToString();
		}
	}
}