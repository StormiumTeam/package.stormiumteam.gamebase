using System;
using System.Collections.Generic;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	interface IOnQueryEnableCollisionFor : IAppEvent
	{
		bool EnableCollisionFor(Entity entity);

		void EnableCollision();
		void DisableCollision();
	}

	public struct QueryCollisionFor : IDisposable
	{
		public Entity Entity;
		public PhysicQueryManager QueryManager;
		
		public QueryCollisionFor(Entity entity, PhysicQueryManager queryManager)
		{
			Entity = entity;
			QueryManager = queryManager;
			
			QueryManager.EnableCollisionFor(entity);
		}
		
		public void Dispose()
		{
			QueryManager.ReenableCollisions();
		}
	}

	public class PhysicQueryManager : ComponentSystem
	{
		private List<IOnQueryEnableCollisionFor> m_ReenableCollisions;
		private int m_RequestLength;

		protected override void OnCreate()
		{
			m_ReenableCollisions = new List<IOnQueryEnableCollisionFor>();
		}

		protected override void OnUpdate()
		{
			if (m_RequestLength > 0)
			{
				Debug.LogError($"{m_RequestLength} system(s) forgot to call ReenableCollisions() !!!");
			}
		}

		public void EnableCollisionFor(Entity entity)
		{
			m_RequestLength++;
			
			ReenableCollisions();
			
			m_ReenableCollisions.Clear();
			
			foreach (var obj in AppEvent<IOnQueryEnableCollisionFor>.GetObjEvents())
			{
				m_ReenableCollisions.Add(obj);
				
				if (!obj.EnableCollisionFor(entity))
				{
					obj.DisableCollision();
					continue;
				}

				obj.EnableCollision();
			}
		}

		public void ReenableCollisions()
		{
			if (m_ReenableCollisions.Count <= 0)
			{
				m_RequestLength--;
				return;
			}

			foreach (var obj in m_ReenableCollisions)
			{
				obj.EnableCollision();
			}
			
			m_RequestLength--;
		}

		public bool SphereCast(Entity entity, Ray ray, float radius, out RaycastHit hit, float maxDistance = float.PositiveInfinity)
		{
			using (new QueryCollisionFor(entity, this))
			{
				return Physics.SphereCast(ray, radius, out hit, maxDistance, GameBaseConstants.CollisionMask);
			}
		}
	}
}