using System;
using System.Linq;
using Unity.NetCode;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Authoring
{
	public class DestroyOnClientServerWorld : MonoBehaviour
	{
		[Flags]
		public enum DestroyTargetType
		{
			None            = 0x0,
			Client          = 0x1,
			Server          = 0x2,
			ClientAndServer = 0x3
		}

		[SerializeField]
		public DestroyTargetType DestroyOnTarget = DestroyTargetType.ClientAndServer;

		private void Awake()
		{
			// TODO: This should be remade
			/*
#if !UNITY_SERVER
			bool destroyOnClient = (ClientServerBootstrap.clientWorld != null && ClientServerBootstrap.clientWorld.Length >= 1);
#else
        bool destroyOnClient = true;
#endif
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
			bool destroyOnServer = ClientServerBootstrap.serverWorld != null;
#else
        bool destroyOnServer = true;
#endif
			if (!destroyOnClient || !destroyOnServer)
			{
				UnityEngine.Debug.LogWarning("DestroyTarget failed because there was no Client and Server Worlds", this);
				return;
			}

			destroyOnClient &= (DestroyOnTarget & DestroyTargetType.Client) != 0 && ClientServerBootstrap.clientWorld.Contains(World.Active);
			destroyOnServer &= (DestroyOnTarget & DestroyTargetType.Server) != 0 && ClientServerBootstrap.serverWorld == World.Active;
			
			if (destroyOnClient || destroyOnServer)
				Destroy(gameObject);*/
		}
	}
}