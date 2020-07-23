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

	// Component data format
	public abstract class RegisterGameHostComponentData<T> : RegisterGameHostComponentSystemBase
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

	// Dynamic buffer format
	public abstract class RegisterGameHostComponentBuffer<T> : RegisterGameHostComponentSystemBase
		where T : struct, IBufferElementData
	{
		public override string ComponentPath => CustomComponentPath ?? typeof(T).FullName;

		protected virtual ICustomComponentDeserializer CustomDeserializer { get; }

		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetExistingSystem<RegisterDeserializerSystem>()
			     .Register(new DefaultArchetypeAttach<T>(ComponentPath), CustomDeserializer ?? new DefaultBufferDeserializer<T>());
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