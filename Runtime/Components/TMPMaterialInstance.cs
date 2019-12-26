using TMPro;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[ExecuteInEditMode]
	public class TMPMaterialInstance : MonoBehaviour
	{
		public bool IsShared;

		[SerializeField] [HideInInspector] private Material m_InstancedMaterial;
		private                                    Material m_PreviousMaterial;

		public Material Material;
		public TMP_Text Text;

		private void OnEnable()
		{
			Debug.Assert(Material != null, "Material != null");
			Debug.Assert(Text != null, "Text != null");

			if (Material == null || Text == null)
				return;

			SetMaterial(Material);
		}

		private void Update()
		{
			SetDirty();
		}

		public void SetMaterial(Material mat)
		{
			if (m_InstancedMaterial != null) DestroyImmediate(m_InstancedMaterial);

			m_InstancedMaterial = Instantiate(mat);
			SetDirty();
		}

		private void SetDirty()
		{
			m_PreviousMaterial = Material;

			if (!IsShared)
			{
				if (m_InstancedMaterial != null) Text.fontMaterial = m_InstancedMaterial;
				else if (Material != null) Text.fontSharedMaterial = Material;
			}
		}
	}
}