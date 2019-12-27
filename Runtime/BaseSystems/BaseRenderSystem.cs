using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace StormiumTeam.GameBase.Systems
{
	[AlwaysSynchronizeSystem]
	public abstract class BaseRenderSystem<TDefinition> : JobGameBaseSystem
		where TDefinition : Component
	{
		public abstract void PrepareValues();
		public abstract void Render(TDefinition definition);
		public abstract void ClearValues();

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			// Prepare the values that is needed for the UI elements...
			PrepareValues();
			// Process the UI elements...
			Entities
				.ForEach((TDefinition definition) => { Render(definition); })
				.WithoutBurst()
				.Run();
			// Clear values that were prepared.
			ClearValues();

			return default;
		}
	}
}