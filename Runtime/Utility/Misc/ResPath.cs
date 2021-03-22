using System;

namespace StormiumTeam.GameBase.Utility.Misc
{
	public partial struct ResPath
	{
		public readonly EType  Type;
		public readonly string Author;
		public readonly string ModPack;
		public readonly string Resource;

		private string computedFullString;

		public string FullString => computedFullString ??= Create(Author, ModPack, Resource, Type);

		public ResPath(EType type, string author, string modPack, string resource)
		{
			Type     = type;
			Author   = author;
			ModPack  = modPack;
			Resource = resource;

			computedFullString = null;
		}

		public ResPath(EType type, string author, string modPack, string[] resourceDeepness)
			: this(type, author, modPack, string.Join("/", resourceDeepness))
		{
		}

		public ResPath(string fullPath)
		{
			if (string.IsNullOrEmpty(fullPath))
			{
				this = default;
				return;
			}

			computedFullString = fullPath;

			var inspection = Inspect(fullPath);
			Type     = inspection.Type;
			Author   = inspection.Author;
			ModPack  = inspection.ModPack;
			Resource = inspection.ResourcePath;
		}
	}

	public partial struct ResPath
	{
		public enum EType
		{
			/// <summary>
			/// Prioritize MasterServer
			/// </summary>
			MasterServer,

			/// <summary>
			/// Prioritize Client
			/// </summary>
			ClientResource,
		}

		public struct Inspection
		{
			public EType  Type;
			public bool   IsGUID;
			public string Author;
			public string ModPack;
			public string ResourcePath;

			public bool IsCore;
		}

		public static Inspection Inspect(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				if (path == null)
					throw new NullReferenceException(nameof(path));

				return new Inspection()
				{
					IsCore       = false,
					IsGUID       = false,
					Author       = string.Empty,
					ModPack      = string.Empty,
					ResourcePath = string.Empty,
					Type         = EType.MasterServer
				};
			}

			Inspection inspection = default;

			var firstDotIdx    = path.IndexOf('.');
			var protocolEndIdx = path.IndexOf("://");

			// MasterServer
			if (path.StartsWith("ms://"))
			{
				var asSpan = path.AsSpan("ms://".Length);

				inspection.Type = EType.MasterServer;

				firstDotIdx = asSpan.IndexOf('.');

				inspection.Author       = asSpan.Slice(0, firstDotIdx).ToString();
				inspection.ModPack      = asSpan.Slice(firstDotIdx + 1, (asSpan.IndexOf('/') - firstDotIdx) - 1).ToString();
				inspection.ResourcePath = asSpan.Slice(asSpan.IndexOf('/') + 1).ToString();
			}
			// Client Resource
			else if (path.StartsWith("cr://"))
			{
				var asSpan = path.AsSpan("cr://".Length);

				inspection.Type = EType.ClientResource;

				firstDotIdx = asSpan.IndexOf('.');

				inspection.Author       = asSpan.Slice(0, firstDotIdx).ToString();
				inspection.ModPack      = asSpan.Slice(firstDotIdx + 1, (asSpan.IndexOf('/') - firstDotIdx) - 1).ToString();
				inspection.ResourcePath = asSpan.Slice(asSpan.IndexOf('/') + 1).ToString();
			}
			else
			{
				var asSpan = path.AsSpan();
				inspection.Type = EType.ClientResource;
				
				
				firstDotIdx = asSpan.IndexOf('.');
				if (firstDotIdx < 0)
					throw new InvalidOperationException($"ParseError on {path}");

				inspection.Author       = asSpan.Slice(0, firstDotIdx).ToString();
				inspection.ModPack      = asSpan.Slice(firstDotIdx + 1, (asSpan.IndexOf('/') - firstDotIdx) - 1).ToString();
				inspection.ResourcePath = asSpan.Slice(asSpan.IndexOf('/') + 1).ToString();
			}

			inspection.IsCore = inspection.Author == inspection.ModPack && inspection.Author == "#";

			return inspection;
		}
	}

	public partial struct ResPath
	{
		public static string Create(string author, string modPack, string resource, EType type)
		{
			return type switch
			{
				EType.MasterServer => $"ms://{author}.{modPack}/{resource}",
				EType.ClientResource => $"cr://{author}.{modPack}/{resource}"
			};
		}

		public static string Create(string author, string modPack, string[] resourceDeepness, EType type)
		{
			return Create(author, modPack, string.Join("/", resourceDeepness), type);
		}
	}
}