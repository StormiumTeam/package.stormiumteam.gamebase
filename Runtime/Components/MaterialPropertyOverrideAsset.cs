using System.Collections.Generic;
using UnityEngine;

namespace StormiumTeam.GameBase.Misc
{
	[CreateAssetMenu(fileName = "MaterialPropertyOverrideAsset", menuName = "Assets/MaterialPropertyOverrideAsset")]
	public class MaterialPropertyOverrideAsset : ScriptableObject
	{
		// This is where the overrides are serialized
		public List<MaterialPropertyOverride.ShaderPropertyValue> propertyOverrides = new List<MaterialPropertyOverride.ShaderPropertyValue>();
		public Shader                                             shader;
	}
}