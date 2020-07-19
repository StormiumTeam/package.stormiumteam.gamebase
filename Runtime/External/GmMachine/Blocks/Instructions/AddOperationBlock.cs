using System;
using System.Linq.Expressions;

namespace GmMachine.Blocks.Instructions
{
	public class AddOperationBlock<T> : Block
	{
		private static readonly SupportMethod<Func<T, T, T>> AddDelegate = new SupportMethod<Func<T, T, T>>(() =>
		{
			ParameterExpression paramA = Expression.Parameter(typeof(T), "a"),
			                    paramB = Expression.Parameter(typeof(T), "b");
			var body = Expression.Add(paramA, paramB);
			return Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
		});

		public T Modifier;

		public VariableBlock<T> Variable;

		public AddOperationBlock(string name) : base(name)
		{
		}

		public AddOperationBlock(string name, T modifier) : base(name)
		{
			Modifier = modifier;
		}

		protected override bool OnRun()
		{
			Variable.Value = AddDelegate.Delegate(Variable.Value, Modifier);
			return true;
		}
	}
}