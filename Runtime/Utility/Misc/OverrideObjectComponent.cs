using System;
using System.Collections.Generic;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.Misc
{

	public class OverrideObjectComponent : MonoBehaviour
	{
		[Serializable]
		public struct Obj
		{
			public string             Key;
			public UnityEngine.Object Object;
			public string             String;
			public int                Int;
			public float              Float;
			public Vector3            Vector;
		}

		[SerializeField]
		private List<Obj> presentationObjects;

		private Dictionary<string, Obj> m_EntityObjects;

		public Dictionary<string, Obj> EntityObjects
		{
			get
			{
				if (m_EntityObjects == null || m_EntityObjects.Count != presentationObjects.Count)
				{
					m_EntityObjects = new Dictionary<string, Obj>();
					foreach (var obj in presentationObjects)
					{
						m_EntityObjects[obj.Key] = obj;
					}
				}

				return m_EntityObjects;
			}
		}

		public bool TryGetPresentationObject<T>(string toSearch, out T obj, T defValue = default)
			where T : UnityEngine.Object
		{
			obj = defValue;
			if (!EntityObjects.TryGetValue(toSearch, out var baseObj))
				return false;

			obj = (T) baseObj.Object;
			return true;
		}

		public bool TryGetFloat(string toSearch, out float obj, float defValue = default)
		{
			obj = defValue;
			if (!EntityObjects.TryGetValue(toSearch, out var baseObj))
				return false;

			obj = baseObj.Float;
			return true;
		}
	}
}