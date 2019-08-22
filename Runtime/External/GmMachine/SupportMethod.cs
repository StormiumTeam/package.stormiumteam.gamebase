using System;

namespace GmMachine
{
	public class SupportMethod<TDelegate>
		where TDelegate : Delegate
	{
		public bool      IsSupported { get; }
		public TDelegate Delegate;

		public SupportMethod(Func<TDelegate> createFunc)
		{
			try
			{
				Delegate    = createFunc();
				IsSupported = true;
			}
			catch
			{
				IsSupported = false;
			}
		}
	}
}