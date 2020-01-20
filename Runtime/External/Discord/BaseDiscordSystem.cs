using System;
using Unity.Entities;
using Unity.NetCode;
using Discord;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace StormiumTeam.GameBase.External.Discord
{
	public class DiscordLocalUser : IComponentData
	{
		public User   User;
		public string Username      => User.Username;
		public string Discriminator => User.Discriminator;
		public long   Id            => User.Id;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	[AlwaysUpdateSystem]
	public abstract class BaseDiscordSystem : ComponentSystem
	{
		public static BaseDiscordSystem Instance { get; private set; }

		private Activity                m_Activity;
		private global::Discord.Discord m_Discord;

		/// <summary>
		/// Return the client ID for Discord...
		/// </summary>
		protected abstract long ClientId { get; }

		/// <summary>
		/// Log level for <see cref="HookResult"/>
		/// </summary>
		protected global::Discord.LogLevel LogLevel { get; }

		protected bool IsUserReady;

		protected override void OnCreate()
		{
			base.OnCreate();

			if (Instance != null)
				throw new Exception($"We were trying to set the instance to {GetType()} while there is already one: {Instance.GetType()}!");
			
			Instance = this;
			
			#if UNITY_EDITOR
			System.Environment.SetEnvironmentVariable("DISCORD_INSTANCE_ID", "0");
			#endif
			
			Debug.Log("Creating Discord Instance...");
			m_Discord = new global::Discord.Discord(ClientId, (uint) global::Discord.CreateFlags.Default);
			m_Discord.SetLogHook(LogLevel.Debug, HookResult);
			IsUserReady = false;

			Debug.Log("GetUserManager() start");
			m_Discord.GetUserManager().OnCurrentUserUpdate += CurrentUserUpdateResult;
			Debug.Log("GetUserManager() end");
		}

		protected override void OnUpdate()
		{
			m_Discord.RunCallbacks();
		}

		protected override void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
			
			m_Discord.Dispose();
		}

		/// <summary>
		/// Push a new activity value
		/// </summary>
		/// <returns>Return true if the value was different from previous activity</returns>
		protected unsafe bool Push(Activity activity)
		{
			if (UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref m_Activity), UnsafeUtility.AddressOf(ref activity), UnsafeUtility.SizeOf<Activity>()) == 0)
				return false;

			m_Activity = activity;
			m_Discord.GetActivityManager()
			         .UpdateActivity(activity, UpdateActivityResult);

			return true;
		}

		/// <summary>
		/// Get the current Discord client
		/// </summary>
		/// <returns></returns>
		protected global::Discord.Discord GetDiscord() => m_Discord;

		protected virtual void UpdateActivityResult(global::Discord.Result result)
		{
			Debug.Log("discord/UpdateActivityResult -> " + result);
		}

		protected virtual void HookResult(global::Discord.LogLevel level, string message)
		{
			Debug.Log($"discord/HookResult -> {level} -> {message}");
		}

		protected virtual void CurrentUserUpdateResult()
		{
			Debug.Log("UpdateResult() start");
			IsUserReady = true;

			EntityQuery query = GetEntityQuery(typeof(DiscordLocalUser));
			Entity      localUser;
			localUser = query.CalculateEntityCount() == 0
				? EntityManager.CreateEntity(typeof(DiscordLocalUser))
				: query.GetSingletonEntity();

			EntityManager.SetComponentData(localUser, new DiscordLocalUser {User = m_Discord.GetUserManager().GetCurrentUser()});
			Debug.Log("UpdateResult() end");
		}

		public virtual User GetLocalUser()
		{
			return m_Discord.GetUserManager().GetCurrentUser();
		}

		public virtual void GetUser(long id, UserManager.GetUserHandler onUser)
		{
			m_Discord.GetUserManager().GetUser(id, onUser);
		}
	}
}