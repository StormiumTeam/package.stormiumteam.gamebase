using GameHost.ShareSimuWorldFeature;
using Unity.Entities;

namespace GameHost.Simulation.Features.ShareWorldState.BaseSystems
{
	public abstract class RegisterGameHostComponentSystemBase : SystemBase
	{
		public abstract   string ComponentPath       { get; }
		protected virtual string CustomComponentPath { get; }

		internal abstract void DontUseThisBase();
	}

	public abstract class RegisterGameHostComponentSystemBase<T> : RegisterGameHostComponentSystemBase
		where T : struct, IComponentData
	{
		public override string ComponentPath => CustomComponentPath ?? typeof(T).FullName;

		protected virtual ICustomComponentDeserializer CustomDeserializer { get; }

		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetExistingSystem<RegisterDeserializerSystem>()
			     .Register(new DefaultArchetypeAttach<T>(ComponentPath), CustomDeserializer ?? new DefaultSingleDeserializer<T>());
			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}

		internal override void DontUseThisBase()
		{
		}
	}
}