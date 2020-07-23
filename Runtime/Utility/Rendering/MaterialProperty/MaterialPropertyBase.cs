using UnityEngine;

namespace StormiumTeam.GameBase.Utility.Rendering.MaterialProperty
{
	public abstract class MaterialPropertyBase : MonoBehaviour
	{
		public abstract void RenderOn(MaterialPropertyBlock mpb);
	}

	public abstract class MaterialPropertyBase<T> : MaterialPropertyBase
		where T : unmanaged
	{
		public abstract string PropertyId { get; }

		[SerializeField]
		private string overridePropertyId;

		[field: SerializeField]
		public virtual T Value { get; set; }

		public override void RenderOn(MaterialPropertyBlock mpb)
		{
			var property = string.IsNullOrEmpty(overridePropertyId) ? PropertyId : overridePropertyId;

			switch (Value)
			{
				case float f32:
					mpb.SetFloat(property, f32);
					break;
				case int int32:
					mpb.SetInt(property, int32);
					break;
				case Color color:
					mpb.SetColor(property, color);
					break;
			}
		}
	}
}