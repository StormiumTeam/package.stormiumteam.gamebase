using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StormiumTeam.GameBase.BaseSystems;
using UnityEngine;

namespace Misc
{
	public class BaseRuleConfiguration : IDisposable
	{
		private FileInfo           m_FileInfo;
		private RulePropertiesBase m_Rule;

		public int TargetVersion;
		public int FileVersion;

		public BaseRuleConfiguration(RulePropertiesBase rule, RuleBaseSystem system, int version) : this(rule,
			version,
			$"{Application.streamingAssetsPath}/rules/{(string.IsNullOrEmpty(system.Name) ? system.GetType().Name : system.Name)}.json")
		{

		}

		public BaseRuleConfiguration(RulePropertiesBase rule, int targetVersion, string filePath)
		{
			m_Rule        = rule;
			TargetVersion = targetVersion;

			rule.OnPropertyChanged += OnPropertyChanged;

			m_FileInfo = new FileInfo(filePath);
			var exists = m_FileInfo.Exists;
			m_FileInfo.Directory.Create();

			if (!exists)
			{
				Debug.Log("Save");
				m_FileInfo.Create().Dispose();
				Save(); // copy current data to new file...
			}
			else
			{
				Debug.Log("Load");
				var str = File.ReadAllText(filePath);
				var deserialized = JsonConvert.DeserializeObject<Data>(str, new JsonSerializerSettings
				{
					ContractResolver = new WritablePropertiesOnlyResolver()
				});

				if (deserialized != null)
				{
					FileVersion = deserialized.Version;
					rule.SetDataObject(deserialized.Value);
				}
				else
				{
					Save();
				}
			}
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Save();
		}

		public void Save()
		{
			Debug.Log("Saved!");
			var data = m_Rule.GetDataObject();
			File.WriteAllText(m_FileInfo.FullName, JsonConvert.SerializeObject(new Data
			{
				Version = TargetVersion,
				Value   = data
			}, Formatting.Indented, new JsonSerializerSettings
			{
				ContractResolver = new WritablePropertiesOnlyResolver()
			}));
		}

		public void SaveAndDispose()
		{
			Save();
			Dispose();
		}

		public void Dispose()
		{
			m_Rule.OnPropertyChanged -= OnPropertyChanged;
		}

		private class Data
		{
			public int    Version;
			public object Value;
		}
		
		class WritablePropertiesOnlyResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
				return props.Where(p => p.Writable).ToList();
			}
		}
	}
}