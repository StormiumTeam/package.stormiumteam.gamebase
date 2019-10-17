using Revolution;
using Revolution.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace StormiumTeam.GameBase
{
	public struct EyePosition : IReadWriteComponentSnapshot<EyePosition>
	{
		public float3 Value;

		public EyePosition(float3 value)
		{
			Value = value;
		}

		public void WriteTo(DataStreamWriter writer, ref EyePosition baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			for (var i = 0; i != 3; i++)
				writer.WritePackedIntDelta((int) Value[i] * 1000, (int) baseline.Value[i] * 1000, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref EyePosition baseline, DeserializeClientData jobData)
		{
			for (var i = 0; i != 3; i++)
				Value[i] = reader.ReadPackedIntDelta(ref ctx, (int) baseline.Value[i] * 1000, jobData.NetworkCompressionModel) * 0.001f;
		}
	}
}