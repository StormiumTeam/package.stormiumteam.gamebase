using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.Rendering.BaseSystems
{
	public abstract class BaseRenderSystem<TDefinition> : AbsGameBaseSystem
		where TDefinition : Component
	{
		protected bool HasAnyDefinition { get; private set; }
		protected int  DefinitionCount  { get; private set; }

		private EntityQuery m_DefinitionQuery;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_DefinitionQuery = GetEntityQuery(typeof(TDefinition));
		}

		protected abstract void PrepareValues();
		protected abstract void Render(TDefinition definition);
		protected abstract void ClearValues();

		protected override void OnUpdate()
		{
			HasAnyDefinition = !m_DefinitionQuery.IsEmptyIgnoreFilter;
			if (HasAnyDefinition)
			{
				DefinitionCount = m_DefinitionQuery.CalculateEntityCount();
			}
			else DefinitionCount = 0;

			// Prepare the values that is needed for the UI elements...
			PrepareValues();
			// Process the UI elements...
			if (HasAnyDefinition)
			{
				// todo: remove gc alloc :(
				var definitionArray = m_DefinitionQuery.ToComponentArray<TDefinition>();
				for (var i = 0; i != definitionArray.Length; i++)
					Render(definitionArray[i]);
			}

			// Clear values that were prepared.
			ClearValues();
		}
	}
}