using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.Rules;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.BaseSystems
{
	public abstract class RuleBaseSystem : AbsGameBaseSystem
	{
		public List<RulePropertiesBase> PropertiesCollection;

		public virtual string Name        => GetType().Name;
		public virtual string Description => "NoDescription";

		protected override void OnCreate()
		{
			base.OnCreate();

			PropertiesCollection = new List<RulePropertiesBase>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			PropertiesCollection.ForEach(r => r.Dispose());
		}

		protected RuleProperties<TData> AddRule<TData>()
			where TData : struct, IComponentData
		{
			var properties = new RuleProperties<TData> {System = this};

			PropertiesCollection.Add(properties);

			if (!HasSingleton<TData>())
			{
				Debug.Log($"Create {typeof(TData)}");
				EntityManager.CreateEntity(typeof(TData));
				RequireSingletonForUpdate<TData>();
			}

			return properties;
		}

		protected RuleProperties<TData> AddRule<TData>(out TData data)
			where TData : struct, IComponentData
		{
			var properties = AddRule<TData>();
			Debug.Log(HasSingleton<TData>());
			data = GetSingleton<TData>();
			return properties;
		}
	}

	public abstract class RuleBaseSystem<TData> : RuleBaseSystem
		where TData : struct, IComponentData
	{
		protected BaseRuleConfiguration Configuration;
		public    RuleProperties<TData> Rule;

		public virtual int Version => 1;

		protected override void OnCreate()
		{
			base.OnCreate();

			Rule = AddRule<TData>();
			AddRuleProperties();
			SetDefaultProperties();
			Configuration = new BaseRuleConfiguration(Rule, this, Version);
			if (Configuration.FileVersion != Configuration.TargetVersion) OnUpgrade(Configuration.FileVersion);
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Configuration.Dispose();
			Rule.Dispose();
		}

		protected abstract void AddRuleProperties();
		protected abstract void SetDefaultProperties();

		protected virtual void OnUpgrade(int previousVersion)
		{
		}
	}
}