using System;
using System.Collections.ObjectModel;
using GameHost.InputBackendFeature.BaseSystems;
using GameHost.InputBackendFeature.Interfaces;
using GameHost.InputBackendFeature.Layouts;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.DefaultActions
{
	public struct PressAction : IInputAction
	{
		public class Layout : InputLayoutBase
		{
			public Layout(string id, params CInput[] inputs) : base(id)
			{
				Inputs = new ReadOnlyCollection<CInput>(inputs);
			}

			public override void Serialize(ref DataBufferWriter buffer)
			{
				buffer.WriteInt(Inputs.Count);
				foreach (var input in Inputs)
					buffer.WriteStaticString(input.Target);
			}

			public override void Deserialize(ref DataBufferReader buffer)
			{
				var count = buffer.ReadValue<int>();
				var array = new CInput[count];
				for (var i = 0; i != count; i++)
					array[i] = new CInput(buffer.ReadString());

				Inputs = new ReadOnlyCollection<CInput>(array);
				foreach (var input in Inputs)
					Console.WriteLine(input.Target);
			}
		}

		public uint DownCount, UpCount;

		public bool HasBeenPressed => DownCount > 0;

		public class System : InputActionSystemBase<PressAction, Layout>
		{

		}

		public void Serialize(ref DataBufferWriter buffer)
		{
			buffer.WriteValue(DownCount);
			buffer.WriteValue(UpCount);
		}

		public void Deserialize(ref DataBufferReader buffer)
		{
			DownCount = buffer.ReadValue<uint>();
			UpCount   = buffer.ReadValue<uint>();
		}
	}
}