using System;
using System.Collections.Generic;
using System.IO;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace StormiumTeam.GameBase.Systems
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[AlwaysUpdateSystem]
	public class MapManager : ComponentSystem
	{
		[Serializable]
		public struct JMapFormat
		{
			public string   name;
			public string   description;
			public string   addrPrefix;
			public string[] clientScenes;
			public string[] serverScenes;
		}

		public bool IsMapLoaded      => m_ExecutingQuery.CalculateEntityCount() > 0 && !IsMapBeingLoaded;
		public bool AnyMapQueued     => m_RequestLoadQuery.CalculateEntityCount() > 0 || m_ForceDataQuery.CalculateEntityCount() > 0;
		public bool IsMapBeingLoaded => m_LoadOperationQuery.CalculateEntityCount() > 0;
		public bool AnyOperation => m_OperationQuery.CalculateEntityCount() > 0;

		private EntityQuery m_ExecutingQuery;
		private EntityQuery m_RequestUnloadQuery;
		private EntityQuery m_RequestLoadQuery;
		private EntityQuery m_LoadOperationQuery;
		private EntityQuery m_OperationQuery;
		private EntityQuery m_ForceDataQuery;

		private Dictionary<string, JMapFormat> m_MapCatalog;
		private List<AsyncOperationHandle<SceneInstance>> m_LoadOperations;
		private List<AsyncOperationHandle<SceneInstance>> m_UnloadOperations;

		public IReadOnlyList<AsyncOperationHandle<SceneInstance>> GetLoadOperations()
		{
			return m_LoadOperations;
		}

		public JMapFormat GetMapFormat(string id)
		{
			return m_MapCatalog[id];
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MapCatalog = new Dictionary<string, JMapFormat>(16);
			m_LoadOperations = new List<AsyncOperationHandle<SceneInstance>>();
			m_UnloadOperations = new List<AsyncOperationHandle<SceneInstance>>();

			m_ExecutingQuery     = GetEntityQuery(typeof(ExecutingMapData));
			m_RequestUnloadQuery = GetEntityQuery(typeof(RequestMapUnload));
			m_RequestLoadQuery   = GetEntityQuery(typeof(RequestMapLoad));
			m_LoadOperationQuery = GetEntityQuery(typeof(AsyncMapOperation), typeof(OperationMapLoadTag));
			m_OperationQuery = GetEntityQuery(typeof(AsyncMapOperation));
			m_ForceDataQuery     = GetEntityQuery(typeof(ForceMapData));

			foreach (var file in Directory.GetFiles(Application.streamingAssetsPath + "/maps/", "*.json"))
			{
				var txt = File.ReadAllText(file);
				var id = new FileInfo(file).Name.Replace(".json", string.Empty);
				var format = JsonUtility.FromJson<JMapFormat>(txt);
				
				Debug.Log($"Added Map({id}) [name: {format.name}]");
				
				AddMapToCatalog(id, format);
			}
		}

		protected override void OnUpdate()
		{
			if (!AnyOperation && m_RequestUnloadQuery.CalculateEntityCount() > 0)
			{
				UnloadMap(false);
				EntityManager.DestroyEntity(m_RequestUnloadQuery);
			}

			if (!AnyOperation && m_RequestLoadQuery.CalculateEntityCount() > 0)
			{
				UnloadMap(true);
				LoadMap();
				EntityManager.DestroyEntity(m_RequestLoadQuery);
			}

			SynchronizeMapLoad();
			
			EntityManager.DestroyEntity(m_ForceDataQuery);
		}

		private void UnloadMap(bool ignoreWarning)
		{
			if (!IsMapLoaded)
			{
				if (!ignoreWarning)
					Debug.LogWarning("Ignoring 'UnloadMap' as no map is currently loaded.");
				return;
			}

			var entity = m_ExecutingQuery.GetSingletonEntity();

			// Unload scenes...
			var scenes = EntityManager.GetBuffer<MapScene>(entity);
			for (int i = 0, length = scenes.Length; i != length; i++)
			{
				SceneManager.UnloadSceneAsync(scenes[i].Value /*, options?*/);
			}

			EntityManager.DestroyEntity(m_ExecutingQuery);
		}

		private void LoadMap()
		{
			EntityManager.DestroyEntity(m_ExecutingQuery);
			
			Entity entity;
			using (var entities = m_RequestLoadQuery.ToEntityArray(Allocator.TempJob))
				entity = entities[entities.Length - 1]; // get the latest request

			var request = EntityManager.GetComponentData<RequestMapLoad>(entity);
			var strKey = request.Key.ToString();
			if (!m_MapCatalog.ContainsKey(strKey))
			{
				Debug.LogError($"No map found with {strKey}");
				return;
			}

			var mapEntity = EntityManager.CreateEntity(typeof(ExecutingMapData), typeof(MapScene));
			var mapFormat = m_MapCatalog[strKey];
			
			var scenes = World.GetExistingSystem<ServerSimulationSystemGroup>() != null
				? mapFormat.serverScenes  // server
				: mapFormat.clientScenes; // client

			for (int s = 0, length = scenes.Length; s != length; s++)
			{
				Debug.Log($"Loading game map: {mapFormat.addrPrefix + scenes[s]}");
				m_LoadOperations.Add(Addressables.LoadSceneAsync(mapFormat.addrPrefix + scenes[s], LoadSceneMode.Additive));
			}

			EntityManager.CreateEntity(typeof(AsyncMapOperation), typeof(OperationMapLoadTag));
			EntityManager.SetComponentData(mapEntity, new ExecutingMapData {Key = new NativeString512(strKey)});
		}

		private void SynchronizeMapLoad()
		{
			if (m_LoadOperations.Count <= 0)
				return;
			
			var loaded = 0;
			foreach (var op in m_LoadOperations)
			{
				if (!op.IsDone)
					continue;
				loaded++;
			}

			if (loaded < m_LoadOperations.Count)
				return;
			
			var entity = m_ExecutingQuery.GetSingletonEntity();
			var sceneBuffer = EntityManager.GetBuffer<MapScene>(entity);
			Debug.Assert(sceneBuffer.Length == 0, "sceneBuffer.Length == 0");
			
			for (var i = 0; i != m_LoadOperations.Count; i++)
			{
				sceneBuffer.Add(new MapScene {Value = m_LoadOperations[i].Result.Scene});
			}
			m_LoadOperations.Clear();
			
			EntityManager.DestroyEntity(m_LoadOperationQuery);
		}

		public void AddMapToCatalog(string id, JMapFormat mapFormat)
		{
			m_MapCatalog[id] = mapFormat;
		}
	}
}