﻿using System;
using DefaultNamespace.BaseSystems.Interfaces;
using GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Entities;

namespace DefaultNamespace.BaseSystems
{
	public abstract class AbsGameBaseSystem : SystemBase, IGameBaseSystem
	{
		private ComponentSystemGroup m_ClientPresentationGroup;
		private EntityQuery          m_LocalPlayerGroup;

		private EntityQuery m_PlayerGroup;

		private ModuleRegister m_ModuleRegister;

		protected override void OnCreate()
		{
			m_LocalPlayerGroup = GetEntityQuery
			(
				typeof(PlayerDescription), typeof(PlayerIsLocal)
			);

			m_PlayerGroup = GetEntityQuery
			(
				typeof(PlayerDescription)
			);

			m_ModuleRegister = new ModuleRegister(this);
		}

		public void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			m_ModuleRegister.GetModule(out module);
		}

		EntityQuery IGameBaseSystem.GetPlayerGroup()      => m_PlayerGroup;
		EntityQuery IGameBaseSystem.GetLocalPlayerGroup() => m_LocalPlayerGroup;
	}
}