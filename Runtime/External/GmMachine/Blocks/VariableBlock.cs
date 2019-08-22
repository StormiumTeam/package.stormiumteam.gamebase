namespace GmMachine.Blocks
{
	public interface IVariableHolder<T>
	{
		T Value { get; set; }
	}

	public abstract class VariableBaseBlock : Block
	{
		public bool DefaultDataOnFinish;
		public bool DefaultDataOnReset;

		public VariableBaseBlock(string name) : base(name)
		{
		}

		public abstract object GetVal();

		public abstract void SetVal(object obj);

		public abstract void SetDefault();

		protected override void OnReset()
		{
			base.OnReset();
			if (DefaultDataOnReset)
				SetDefault();
		}

		protected override void OnFinished()
		{
			base.OnFinished();
			if (DefaultDataOnFinish)
				SetDefault();
		}
	}

	public class VariableBlock<T> : VariableBaseBlock, IVariableHolder<T>
	{
		public T Value { get; set; }

		public VariableBlock(string name) : base(name)
		{
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		public Variable<T> GetWrapper()
		{
			return new Variable<T> {Holder = this};
		}

		public override object GetVal()
		{
			return Value;
		}

		public override void SetVal(object obj)
		{
			Value = (T) obj;
		}

		public override void SetDefault()
		{
			Value = default;
		}
	}

	public struct Variable<T>
	{
		public T                  ConstValue;
		public IVariableHolder<T> Holder;

		public T GetValue()
		{
			return Holder != null ? Holder.Value : ConstValue;
		}
	}
}