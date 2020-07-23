﻿using System;

namespace GameHost.Native
{
	public unsafe struct CharBuffer64 : ICharBuffer
	{
		private const int  ConstCapacity = 64;
		private fixed char buffer[ConstCapacity];

		int ICharBuffer.Capacity => ConstCapacity;
		int ICharBuffer.Length   { get; set; }

		public Span<char> Span
		{
			get
			{
				fixed (char* ptr = buffer)
					return new Span<char>(ptr, this.GetLength());
			}
		}
	}
}