using System;
using GameHost.InputBackendFeature.BaseSystems;
using GameHost.ShareSimuWorldFeature;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;

namespace GameHost.InputBackendFeature
{
	[UpdateAfter(typeof(UpdateInputActionSystemGroup))]
	public class SendBackendInputSystem : SystemBase
	{
		public DataBufferWriter Buffer;

		private CreateGameHostInputBackendSystem createSystem;
		private UpdateInputActionSystemGroup     updateInputActionSystemGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			createSystem = World.GetExistingSystem<CreateGameHostInputBackendSystem>();
			Buffer       = new DataBufferWriter(0, Allocator.Persistent);

			updateInputActionSystemGroup = World.GetExistingSystem<UpdateInputActionSystemGroup>();
		}

		protected override void OnUpdate()
		{
			Buffer.WriteInt((int) EMessageInputType.ReceiveInputs);

			var countMarker = Buffer.WriteInt(0);
			var count       = 0;
			foreach (var system in updateInputActionSystemGroup.Systems)
			{
				if (system is InputActionSystemBase inputActionSystem)
				{
					Buffer.WriteStaticString(inputActionSystem.ActionPath);
					inputActionSystem.CallSerialize(ref Buffer);
					count++;
				}
			}

			Buffer.WriteInt(count, countMarker);

			unsafe
			{
				createSystem.Driver.Broadcast(default, new Span<byte>((void*) Buffer.GetSafePtr(), Buffer.Length));
			}

			Buffer.Length = 0;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Buffer.Dispose();
		}
	}
}