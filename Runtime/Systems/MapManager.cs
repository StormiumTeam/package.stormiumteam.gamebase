using System;
using System.Collections.Generic;
using System.IO;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace StormiumTeam.GameBase.Systems
{	
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[AlwaysUpdateSystem]
	public class MapManager : GameBaseSystem
	{
		private EntityQuery                               m_ExecutingQuery;
		private EntityQuery                               m_ForceDataQuery;
		private EntityQuery                               m_LoadOperationQuery;
		private List<AsyncOperationHandle<SceneInstance>> m_LoadOperations;

		private Dictionary<string, JMapFormat>            m_MapCatalog;
		private EntityQuery                               m_OperationQuery;
		private EntityQuery                               m_RequestLoadQuery;
		private EntityQuery                               m_RequestUnloadQuery;
		private List<AsyncOperationHandle<SceneInstance>> m_UnloadOperations;

		private EntityQuery m_MapComponentQuery;

		public bool IsMapLoaded      => m_ExecutingQuery.CalculateEntityCount() > 0 && !IsMapBeingLoaded;
		public bool AnyMapQueued     => m_RequestLoadQuery.CalculateEntityCount() > 0 || m_ForceDataQuery.CalculateEntityCount() > 0;
		public bool IsMapBeingLoaded => m_LoadOperationQuery.CalculateEntityCount() > 0;
		public bool AnyOperation     => m_OperationQuery.CalculateEntityCount() > 0;

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

			m_MapCatalog       = new Dictionary<string, JMapFormat>(16);
			m_LoadOperations   = new List<AsyncOperationHandle<SceneInstance>>();
			m_UnloadOperations = new List<AsyncOperationHandle<SceneInstance>>();

			m_ExecutingQuery     = GetEntityQuery(typeof(ExecutingMapData));
			m_RequestUnloadQuery = GetEntityQuery(typeof(RequestMapUnload));
			m_RequestLoadQuery   = GetEntityQuery(typeof(RequestMapLoad));
			m_LoadOperationQuery = GetEntityQuery(typeof(AsyncMapOperation), typeof(OperationMapLoadTag));
			m_OperationQuery     = GetEntityQuery(typeof(AsyncMapOperation));
			m_ForceDataQuery     = GetEntityQuery(typeof(ForceMapData));
			m_MapComponentQuery = GetEntityQuery(typeof(MapComponent));

			var mapDirectory = Application.streamingAssetsPath + "/maps/";
			if (!Directory.Exists(mapDirectory)) Directory.CreateDirectory(mapDirectory);
			foreach (var file in Directory.GetFiles(mapDirectory, "*.json"))
			{
				var txt    = File.ReadAllText(file);
				var id     = new FileInfo(file).Name.Replace(".json", string.Empty);
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
			var scenes = EntityManager.GetBuffer<MapScene>(entity).ToNativeArray(Allocator.Temp);
			for (int i = 0, length = scenes.Length; i != length; i++)
			{
				try
				{
					SceneManager.UnloadSceneAsync(scenes[i].Value /*, options?*/);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error when unloading '{scenes[i].Value.name}' scene");
					Debug.LogException(ex);
				}
			}

			Entities.WithAll<MapComponent>().WithNone<LinkedEntityGroup>().ForEach((Entity e, DynamicBuffer<Child> children) =>
			{
				var childrenArray = children.ToNativeArray(Allocator.Temp);
				for (var i = 0; i != childrenArray.Length; i++) EntityManager.DestroyEntity(childrenArray[i].Value);
			});
			Entities.WithAll<MapComponent>().WithAll<LinkedEntityGroup>().ForEach((Entity e, DynamicBuffer<LinkedEntityGroup> group) =>
			{
				EntityManager.DestroyEntity(e);
			});
			
			EntityManager.DestroyEntity(m_ExecutingQuery);
			EntityManager.DestroyEntity(m_MapComponentQuery);
		}

		private void LoadMap()
		{
			EntityManager.DestroyEntity(m_ExecutingQuery);

			Entity entity;
			using (var entities = m_RequestLoadQuery.ToEntityArray(Allocator.TempJob))
			{
				entity = entities[entities.Length - 1]; // get the latest request
			}

			var request = EntityManager.GetComponentData<RequestMapLoad>(entity);
			var strKey  = request.Key.ToString();
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

			m_LazyFrame = 1;
		}

		private int m_LazyFrame;
		private void SynchronizeMapLoad()
		{
			if (m_LoadOperations.Count <= 0)
			{
				m_LazyFrame = 1;
				return;
			}

			var loaded = 0;
			foreach (var op in m_LoadOperations)
			{
				if (!op.IsDone)
					continue;
				loaded++;
			}
			
			if (loaded < m_LoadOperations.Count)
				return;

			if (m_LazyFrame-- > 0)
				return;

			if (!HasSingleton<MapMetadata>())
				return;
			
			var entity      = m_ExecutingQuery.GetSingletonEntity();
			var sceneBuffer = EntityManager.GetBuffer<MapScene>(entity);
			Debug.Assert(sceneBuffer.Length == 0, "sceneBuffer.Length == 0");

			for (var i = 0; i != m_LoadOperations.Count; i++)
			{
				Debug.Log($"Adding scene: {m_LoadOperations[i].Result.Scene.name}");
				sceneBuffer.Add(new MapScene {Value = m_LoadOperations[i].Result.Scene});
			}
			
			EntityManager.DestroyEntity(m_LoadOperationQuery);
			
			m_LoadOperations.Clear();
		}

		public void AddMapToCatalog(string id, JMapFormat mapFormat)
		{
			m_MapCatalog[id] = mapFormat;
		}

		[Serializable]
		public struct JMapFormat
		{
			public string   name;
			public string   description;
			public string   addrPrefix;
			public string[] clientScenes;
			public string[] serverScenes;
		}
	}
}