using System;
using System.Collections.ObjectModel;
using GameHost.InputBackendFeature.BaseSystems;
using GameHost.InputBackendFeature.Interfaces;
using GameHost.InputBackendFeature.Layouts;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.InputSystem.Controls;

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
			protected override void OnUpdate()
			{
				foreach (var entity in InputQuery.ToEntityArray(Allocator.Temp))
				{
					var currentLayout = EntityManager.GetComponentData<InputCurrentLayout>(GetSingletonEntity<InputCurrentLayout>());

					var layouts = GetLayouts(entity);
					if (!layouts.TryGetOrDefault(currentLayout.Id, out var layout))
						return;

					PressAction action = default;
					foreach (var input in layout.Inputs)
						if (Backend.GetInputControl(input.Target) is ButtonControl buttonControl)
						{
							action.DownCount += buttonControl.wasPressedThisFrame ? 1u : 0;
							action.UpCount   += buttonControl.wasReleasedThisFrame ? 1u : 0;
						}

					EntityManager.SetComponentData(entity, action);
				}
			}
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