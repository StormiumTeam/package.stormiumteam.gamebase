using GameHost.InputBackendFeature.Components;
using GameHost.InputBackendFeature.Interfaces;
using GameHost.InputBackendFeature.Layouts;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;

namespace GameHost.InputBackendFeature.BaseSystems
{
	public class UpdateInputActionSystemGroup : ComponentSystemGroup
	{
	}

	public abstract class InputActionSystemBase : SystemBase
	{
		public abstract string LayoutPath { get; }
		public abstract string ActionPath { get; }

		protected virtual string CustomLayoutPath { get; }
		protected virtual string CustomActionPath { get; }
		
		internal abstract void CallSerialize(ref DataBufferWriter buffer);
	}

	[UpdateInGroup(typeof(UpdateInputActionSystemGroup))]
	public abstract class InputActionSystemBase<TAction, TLayout> : InputActionSystemBase
		where TAction : struct, IInputAction
		where TLayout : InputLayoutBase
	{
		private RegisterInputLayoutSystem registerInputLayoutSystem;
		private RegisterInputActionSystem registerInputActionSystem;

		public override string LayoutPath => CustomLayoutPath ?? typeof(TLayout).FullName;
		public override string ActionPath => CustomActionPath ?? typeof(TAction).FullName;
		
		protected InputBackendSystem Backend    { get; private set; }
		protected EntityQuery        InputQuery { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();
			registerInputLayoutSystem = World.GetExistingSystem<RegisterInputLayoutSystem>();
			registerInputActionSystem = World.GetExistingSystem<RegisterInputActionSystem>();
			registerInputLayoutSystem.Register<TLayout>(LayoutPath);
			registerInputActionSystem.Register<TAction>(ActionPath);

			Backend    = World.GetExistingSystem<InputBackendSystem>();
			InputQuery = GetEntityQuery(typeof(TAction), typeof(ReplicatedInputAction));
		}

		protected InputActionLayouts GetLayouts(in Entity entity)
		{
			return Backend.GetLayoutsOf(entity);
		}

		internal override void CallSerialize(ref DataBufferWriter buffer)
		{
			OnSerialize(ref buffer);
		}

		protected virtual void OnSerialize(ref DataBufferWriter buffer)
		{
			buffer.WriteInt(InputQuery.CalculateEntityCount());
			foreach (var entity in InputQuery.ToEntityArray(Allocator.Temp))
			{
				var repl   = EntityManager.GetComponentData<ReplicatedInputAction>(entity);
				var action = EntityManager.GetComponentData<TAction>(entity);

				buffer.WriteInt(repl.Id);
				action.Serialize(ref buffer);
			}
		}
	}
}