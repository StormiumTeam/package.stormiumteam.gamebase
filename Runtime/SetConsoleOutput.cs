using System;
using System.IO;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
	public class SetConsoleOutput : SystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			UnitySystemConsoleRedirector.Redirect();
			Enabled = false;
		}

		protected override void OnUpdate()
		{
		}
	}

	/// <summary>
	///     Redirects writes to System.Console to Unity3D's Debug.Log.
	/// </summary>
	/// <author>
	///     Jackson Dunstan, http://jacksondunstan.com/articles/2986
	/// </author>
	public static class UnitySystemConsoleRedirector
	{
		public static void Redirect()
		{
			Console.SetOut(new UnityTextWriter());
		}

		private class UnityTextWriter : TextWriter
		{
			private readonly StringBuilder buffer = new StringBuilder();

			public override Encoding Encoding => Encoding.Default;

			public override void Flush()
			{
				buffer.Insert(0, "CONSOLE: ");
				Debug.Log(buffer.ToString());
				buffer.Length = 0;
			}

			public override void Write(string value)
			{
				buffer.Append(value);
				if (value != null)
				{
					var len = value.Length;
					if (len > 0)
					{
						var lastChar = value[len - 1];
						if (lastChar == '\n') Flush();
					}
				}
			}

			public override void Write(char value)
			{
				buffer.Append(value);
				if (value == '\n') Flush();
			}

			public override void Write(char[] value, int index, int count)
			{
				Write(new string(value, index, count));
			}
		}
	}
}