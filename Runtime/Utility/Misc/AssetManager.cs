using System;
using BundleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Misc
{
	public struct AssetPath
	{
		public bool IsCreated => Bundle != null && Asset != null;
		public bool IsEmpty   => Bundle == string.Empty && Asset == string.Empty;
		
		public static AssetPath Empty => new AssetPath(string.Empty, string.Empty);
		
		public readonly string Bundle, Asset;

		public AssetPath(string bundle, string asset)
		{
			Bundle = bundle;
			Asset  = asset;
		}

		public override string ToString()
		{
			return $"('{Bundle}'/'{Asset}')";
		}

		public static implicit operator AssetPath((string bundle, string asset) tuple)   => new AssetPath(tuple.bundle, tuple.asset);
		public static implicit operator AssetPath(in ResPath                    resPath) => new AssetPath(resPath.Author + "." + resPath.ModPack, resPath.Resource);
	}
	
	public struct SceneInstance
	{
		Scene                   m_Scene;
		internal AsyncOperation m_Operation;
		/// <summary>
		/// The scene instance.
		/// </summary>
		public Scene Scene { get { return m_Scene; } internal set { m_Scene = value; } }

		public AsyncOperation ActivateAsync()
		{
			m_Operation.allowSceneActivation = true;
			return m_Operation;
		}
		
		public override int GetHashCode()
		{
			return Scene.GetHashCode();
		}
		
		public override bool Equals(object obj)
		{
			if (!(obj is SceneInstance))
				return false;
			return Scene.Equals(((SceneInstance)obj).Scene);
		}
	}

	public abstract class AssetManager
	{
		private static AssetManager Instance = new LocusAssetManager();

		public static UniTask<TAsset> LoadAssetAsync<TAsset>(AssetPath assetPath)
			where TAsset : Object
		{
			return Instance.DoLoadAssetAsync<TAsset>(assetPath);
		}

		public static UniTask<SceneInstance> LoadSceneAsync(AssetPath assetPath, LoadSceneMode loadSceneMode, bool activateOnLoad = true)
		{
			return Instance.DoLoadSceneAsync(assetPath, loadSceneMode, activateOnLoad);
		}

		public static void Release(Object obj)
		{
			Instance.DoRelease(obj);
		}

		protected abstract UniTask<SceneInstance> DoLoadSceneAsync(AssetPath assetPath, LoadSceneMode loadSceneMode,
		                                                           bool      activateOnLoad);
		protected abstract UniTask<TAsset>        DoLoadAssetAsync<TAsset>(AssetPath assetPath) where TAsset : Object;
		protected abstract void                   DoRelease(Object                   obj);
	}

	public class LocusAssetManager : AssetManager
	{
		protected override UniTask<SceneInstance> DoLoadSceneAsync(AssetPath assetPath, LoadSceneMode loadSceneMode, bool activateOnLoad)
		{
			return UniTask.Create(async () =>
			{
				var enumerator = BundleManager.LoadSceneAsync(assetPath.Bundle, assetPath.Asset, loadSceneMode);
				enumerator.allowSceneActivation = activateOnLoad;
				await enumerator;
				return new SceneInstance
				{
					m_Operation = enumerator,
					Scene       = SceneManager.GetSceneByPath(assetPath.Asset)
				};
			});
		}

		protected override UniTask<TAsset> DoLoadAssetAsync<TAsset>(AssetPath assetPath)
		{
			return UniTask.Create(async () =>
			{
				var enumerator = BundleManager.LoadAsync<TAsset>(assetPath.Bundle, assetPath.Asset);
				await enumerator;
				return enumerator.Asset;
			});
		}

		protected override void DoRelease(Object obj)
		{
			BundleManager.ReleaseObject(obj);
		}
	}
}