using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using UnityEngine;

namespace StormiumTeam.GameBase.Utility.Rules
{
	public class BaseRuleConfiguration : IDisposable
	{
		public           int                FileVersion;
		private readonly FileInfo           m_FileInfo;
		private readonly RulePropertiesBase m_Rule;

		public int TargetVersion;

		public BaseRuleConfiguration(RulePropertiesBase rule, RuleBaseSystem system, int version) : this(rule,
			version,
			$"{Application.persistentDataPath}/rules/{(string.IsNullOrEmpty(system.Name) ? system.GetType().Name : system.Name)}.json")
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

		public void Dispose()
		{
			m_Rule.OnPropertyChanged -= OnPropertyChanged;
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
				ContractResolver = new WritablePropertiesOnlyResolver(),
				Converters       = new List<JsonConverter> {new NativeStringConverter()}
			}));
		}

		public void SaveAndDispose()
		{
			Save();
			Dispose();
		}

		private class Data
		{
			public object Value;
			public int    Version;
		}

		private class NativeStringConverter : JsonConverter<FixedString64>
		{
			public override void WriteJson(JsonWriter writer, FixedString64 value, JsonSerializer serializer)
			{
				writer.WriteValue(value.ToString());
			}

			public override FixedString64 ReadJson(JsonReader reader, Type objectType, FixedString64 existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				return new FixedString64(reader.ReadAsString());
			}
		}

		private class WritablePropertiesOnlyResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				var props = base.CreateProperties(type, memberSerialization);
				return props.Where(p => p.Writable).ToList();
			}
		}
	}
}