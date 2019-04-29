using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public class ClientServerBootstrap : ICustomBootstrap
	{
		public List<Type> Initialize(List<Type> systems)
		{
			foreach (var system in systems)
			{
				Debug.Log("FOUND SYSTEM: " + system);
			}
			return systems;
		}
	}
}