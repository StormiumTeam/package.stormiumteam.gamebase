using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase._2D
{
	public class BackgroundAssetPresentation : RuntimeAssetPresentation
	{
		public float width;

		private void OnEnable()
		{
			transform.position = Vector3.zero;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.white;
			Draw();
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Draw();
		}

		private void Draw()
		{
			var pos = transform.position;
			pos.y = 0;
			pos.z = 10;

			var top = pos;
			top.y += 100;

			Gizmos.DrawLine(pos + Vector3.left * (width * 0.5f), top + Vector3.left * (width * 0.5f));
			Gizmos.DrawLine(pos + Vector3.right * (width * 0.5f), top + Vector3.right * (width * 0.5f));
		}

		public float GetPositionX(float value)
		{
			return math.lerp(Vector3.left * (width * 0.5f), Vector3.right * (width * 0.5f), value).x;
		}
	}
	
	public class BackgroundAssetBackend : RuntimeAssetBackend<BackgroundAssetPresentation> {}
}