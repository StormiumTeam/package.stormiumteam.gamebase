using System;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace StormiumTeam.GameBase._2D
{
	public struct GameVisualBackground : IComponentData
	{
	}

	public class AttachedBackground : IComponentData
	{
		public Transform  Root;
		public GameObject Value;
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[UpdateAfter(typeof(OrderGroup.Presentation.CopyToGameObject))]
	public class ComputeBackgroundSystem : ComponentSystem
	{
		private ClientCreateCameraSystem m_ClientCameraSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ClientCameraSystem = World.GetOrCreateSystem<ClientCreateCameraSystem>();
		}

		protected override void OnUpdate()
		{
			if (m_ClientCameraSystem.Camera == null)
				return;

			var cam   = m_ClientCameraSystem.Camera;
			var pos   = cam.transform.position.x;
			var width = cam.rect.width;

			Entities.WithAll<BackgroundHolderTag>().ForEach((Entity entity) =>
			{
				if (EntityManager.GetComponentObject<Transform>(entity) == null)
				{
					EntityManager.DestroyEntity(entity);
					return;
				}
				
				var startPos = pos;
				var targetPos = pos;
				if (EntityManager.HasComponent<ParallaxEffect>(entity))
				{
					startPos  = pos * (1 - EntityManager.GetComponentObject<ParallaxEffect>(entity).multiplier);
					targetPos = pos * EntityManager.GetComponentObject<ParallaxEffect>(entity).multiplier;
				}

				var offset = targetPos - pos;

				if (EntityManager.HasComponent<CloneBackgroundBehaviour>(entity))
				{
					var clone  = EntityManager.GetComponentObject<CloneBackgroundBehaviour>(entity);
					var buffer = EntityManager.GetBuffer<ComputedBackground>(entity);
					using (var computedArray = clone.Compute(pos, width, Allocator.TempJob))
					{
						if (buffer.Length != computedArray.Length)
						{
							foreach (var b in buffer)
							{
								if (EntityManager.Exists(b.child))
									EntityManager.DestroyEntity(b.child);
							}

							var oldLen = buffer.Length;
							var size   = UnsafeUtility.SizeOf<ComputedBackground>();
							buffer.ResizeUninitialized(computedArray.Length);
							if (oldLen < buffer.Length)
							{
								unsafe
								{
									var ptr = (byte*) buffer.GetUnsafePtr();
									UnsafeUtility.MemSet(ptr + (oldLen * size), 0, (buffer.Length - oldLen) * size);
								}
							}
						}

						var bufferArray = buffer.ToNativeArray(Allocator.Temp);
						for (var i = 0; i < bufferArray.Length; i++)
						{
							var b = bufferArray[i];
							b.data = computedArray[i];

							if (b.child == default || !EntityManager.Exists(b.child))
							{
								b.child = EntityManager.CreateEntity(typeof(Translation), typeof(GameVisualBackground), typeof(AttachedBackground));

								EntityManager.SetComponentData(b.child, new AttachedBackground
								{
									Root  = clone.transform,
									Value = clone.backgrounds[b.data.backgroundIndex].gameObject
								});
							}

							EntityManager.SetComponentData(b.child, new Translation {Value = b.position + offset});
							bufferArray[i] = b;
						}
						EntityManager.GetBuffer<ComputedBackground>(entity).CopyFrom(bufferArray);
					}
				}
			});
		}
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[UpdateAfter(typeof(ComputeBackgroundSystem))]
	public class BackgroundPoolingSystem : PoolingSystem<BackgroundAssetBackend, BackgroundAssetPresentation>
	{
		protected override string AddressableAsset            => string.Empty;
		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(Translation), typeof(GameVisualBackground));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			if (EntityManager.TryGetComponent(target, out AttachedBackground attached) 
			    && attached.Value != null)
			{
				LastBackend.transform.SetParent(attached.Root, false);
				LastBackend.SetPresentationSingle(Object.Instantiate(attached.Value));
			}
			else
			{
				LastBackend.GetComponent<SortingGroup>()
				           .sortingLayerName = "Backgrounds";
			}
		}
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[UpdateAfter(typeof(BackgroundPoolingSystem))]
	public class RenderBackgroundSystem : BaseRenderSystem<BackgroundAssetPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(BackgroundAssetPresentation definition)
		{
			var backend = definition.Backend;
			backend.transform.localPosition = new Vector3
			{
				x = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value.x
			};
		}

		protected override void ClearValues()
		{
			
		}
	}
}