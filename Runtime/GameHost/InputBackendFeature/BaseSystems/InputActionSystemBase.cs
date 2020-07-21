using GameHost.InputBackendFeature.Interfaces;
using GameHost.InputBackendFeature.Layouts;
using Unity.Entities;

namespace GameHost.InputBackendFeature.BaseSystems
{
	public class InputActionSystemBase<TAction, TLayout> : SystemBase
		where TAction : struct, IInputAction
		where TLayout : InputLayoutBase
	{
		public string CustomLayoutPath { get; }
		public string CustomActionPath { get; }

		private RegisterInputLayoutSystem registerInputLayoutSystem;
		private RegisterInputActionSystem registerInputActionSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			registerInputLayoutSystem = World.GetExistingSystem<RegisterInputLayoutSystem>();
			registerInputActionSystem = World.GetExistingSystem<RegisterInputActionSystem>();
			registerInputLayoutSystem.Register<TLayout>(CustomLayoutPath ?? typeof(TLayout).FullName);
			registerInputActionSystem.Register<TAction>(CustomActionPath ?? typeof(TAction).FullName);
		}

		protected override void OnUpdate()
		{
		}
	}
}