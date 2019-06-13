using System.Collections.Generic;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public class AudioClipUtility
	{
		public static AudioClip Combine(string name, params AudioClip[] clips)
		{
			if (clips == null || clips.Length == 0)
				return null;
			
			Debug.Log("Combine with " + clips.Length + " clips.");

			var length = 0;
			for (var i = 0; i < clips.Length; i++)
			{
				if (clips[i] == null)
				{
					Debug.Log("null clip?");
					continue;
				}

				length += clips[i].samples * clips[i].channels;
			}

			var data = new List<float>(length);
			for (var i = 0; i < clips.Length; i++)
			{
				if (clips[i] == null)
					continue;

				/*var buffer = new float[clips[i].samples * clips[i].channels];
				clips[i].GetData(buffer, 0);
				//System.Buffer.BlockCopy(buffer, 0, data, length, buffer.Length);
				buffer.CopyTo(data, length);
				length += buffer.Length;*/
				if (clips[i].loadState != AudioDataLoadState.Loaded)
				{
					var status = clips[i].LoadAudioData();
					Debug.Log($"Tried to load clip ({clips[i].name}), current status=" + clips[i].loadState);
				}
				
				var buffer = new float[clips[i].samples * clips[i].channels];
				if (!clips[i].GetData(buffer, 0))
				{
					Debug.Log("Couldn't get data from clip...");
				}

				data.AddRange(buffer);
			}

			Debug.Log($"pl: {data.Count} {length}");

			if (length == 0)
				return null;

			var result = AudioClip.Create(name, length / 2, 2, clips[0].frequency, false);
			result.SetData(data.ToArray(), 0);

			return result;
		}
	}
}