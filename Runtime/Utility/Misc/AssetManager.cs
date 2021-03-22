using System;
using BundleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase.Utility.Misc
{
	[Serializable]
	public struct AssetPathSerializable
	{
		// when editing in a KeyValue format, we want to show the asset first
		public string asset, bundle;
	}
	
	public struct AssetPath : IEquatable<AssetPath>
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
			return $"(bundle=\"{Bundle}\" asset=\"{Asset}\")";
		}

		public static implicit operator AssetPath((string bundle, string asset) tuple)   => new AssetPath(tuple.bundle, tuple.asset);
		public static implicit operator AssetPath(in ResPath                    resPath) => new AssetPath(resPath.Author + "." + resPath.ModPack, resPath.Resource);
		public static implicit operator AssetPath(in AssetPathSerializable             serializable)   => new AssetPath(serializable.bundle, serializable.asset);

		public bool Equals(AssetPath other)
		{
			return Bundle == other.Bundle && Asset == other.Asset;
		}

		public override bool Equals(object obj)
		{
			return obj is AssetPath other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Bundle != null ? Bundle.GetHashCode() : 0) * 397) ^ (Asset != null ? Asset.GetHashCode() : 0);
			}
		}

		public static bool operator ==(AssetPath left, AssetPath right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AssetPath left, AssetPath right)
		{
			return !left.Equals(right);
		}
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
		protected override async UniTask<SceneInstance> DoLoadSceneAsync(AssetPath assetPath, LoadSceneMode loadSceneMode, bool activateOnLoad)
		{
			if (!assetPath.IsCreated)
				throw new NullReferenceException(nameof(assetPath));
			
			await UniTask.Yield();
			
			var enumerator = BundleManager.LoadSceneAsync(assetPath.Bundle, assetPath.Asset, loadSceneMode);
			if (enumerator == null)
			{
				Debug.LogError($"[AssetManager] Couldn't call LoadSceneAsync on path {assetPath}");
				return default;
			}
			
			enumerator.allowSceneActivation = activateOnLoad;
			await enumerator;
			
			return new SceneInstance
			{
				m_Operation = enumerator,
				Scene       = SceneManager.GetSceneByPath(assetPath.Asset)
			};
		}

		protected override async UniTask<TAsset> DoLoadAssetAsync<TAsset>(AssetPath assetPath)
		{
			if (!assetPath.IsCreated)
				throw new NullReferenceException(nameof(assetPath));

			var enumerator = BundleManager.LoadAsync<TAsset>(assetPath.Bundle, assetPath.Asset);
			if (enumerator == null)
				return null;
			
			await enumerator;
			
			if (enumerator.Asset == null)
			{
				Debug.LogError($"[AssetManager] Null asset on {assetPath} ({enumerator.IsDone} {enumerator.Progress})");
				return default;
			}
			
			return enumerator.Asset;
		}

		protected override void DoRelease(Object obj)
		{
			BundleManager.ReleaseObject(obj);
		}
	}
}