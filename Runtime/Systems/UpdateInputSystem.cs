using Unity.Entities;
using Unity.NetCode;
using UnityEngine.InputSystem;

namespace Systems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class UpdateInputSystem : ComponentSystem
	{
		protected override void OnCreate()
		{
			InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
		}

		protected override void OnUpdate()
		{
			InputSystem.Update();
		}
	}
}