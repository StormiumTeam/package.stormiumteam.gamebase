using System;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using StormiumTeam.GameBase.BaseSystems;

namespace Misc
{
	public class BaseRuleConfiguration : IDisposable
	{
		private FileInfo           m_FileInfo;
		private RulePropertiesBase m_Rule;

		public int TargetVersion;
		public int FileVersion;

		public BaseRuleConfiguration(RulePropertiesBase rule, int targetVersion, string filePath)
		{
			m_Rule        = rule;
			TargetVersion = targetVersion;

			rule.OnPropertyChanged += OnPropertyChanged;

			m_FileInfo = new FileInfo(filePath);
			var exists   = m_FileInfo.Exists;
			m_FileInfo.Directory.Create();

			if (!exists)
			{
				m_FileInfo.Create().Dispose();
				Save(); // copy current data to new file...
			}
			else
			{
				var str          = File.ReadAllText(filePath);
				var deserialized = JsonConvert.DeserializeObject<Data>(str);
				
				FileVersion = deserialized.Version;
			}
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Save();
		}

		public void Save()
		{
			var data = m_Rule.GetDataObject();
			File.WriteAllText(m_FileInfo.FullName, JsonConvert.SerializeObject(new Data
			{
				Version = TargetVersion,
				Value   = data
			}, Formatting.Indented));
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
	}
}