using System;
using System.Collections.Generic;
using UnityEngine;

namespace StormiumTeam.GameBase
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
		
		private Dictionary<string, object> m_EntityObjects; 
		public Dictionary<string, object> EntityObjects
		{
			get
			{
				if (m_EntityObjects == null || m_EntityObjects.Count != presentationObjects.Count)
				{
					m_EntityObjects = new Dictionary<string, object>();
					foreach (var obj in presentationObjects)
					{
						m_EntityObjects[obj.Key] = obj.Object;
					}
				}

				return m_EntityObjects;
			}
		}

		public bool TryGetPresentationObject<T>(string toSearch, out T obj, T defValue = default)
		{
			obj = defValue;
			if (!EntityObjects.TryGetValue(toSearch, out var baseObj))
				return false;

			obj = (T) baseObj;
			return true;
		}
	}
}