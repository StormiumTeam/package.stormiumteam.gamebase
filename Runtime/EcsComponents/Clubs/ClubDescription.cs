using System;
using DefaultNamespace;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Relative<ClubDescription>))]

namespace StormiumTeam.GameBase.Components
{
	public struct ClubDescription : IEntityDescription
	{
	}

	public struct ClubInformation : IComponentFromSnapshot<ClubSnapshotData>
	{
		public NativeString64 Name;

		public Color PrimaryColor;
		public Color SecondaryColor;
		
		public void Set(ClubSnapshotData snapshot, NativeHashMap<int, GhostEntity> ghostMap)
		{
			Name = snapshot.Name;
			PrimaryColor = snapshot.PrimaryColor;
			SecondaryColor = snapshot.SecondaryColor;

			PrimaryColor.a = 1;
			SecondaryColor.a = 1;
		}
	}

	public class ClubProvider : BaseProviderBatch<ClubProvider.Create>
	{
		[Serializable]
		public struct Create
		{
			public NativeString64 name;
			public Color          primaryColor;
			public Color          secondaryColor;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ClubDescription),
				typeof(ClubInformation)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new ClubInformation
			{
				Name           = data.name,
				PrimaryColor   = data.primaryColor,
				SecondaryColor = data.secondaryColor
			});
		}
	}
}