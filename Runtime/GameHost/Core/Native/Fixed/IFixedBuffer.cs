﻿using System;
using System.Runtime.CompilerServices;

namespace GameHost.Native.Fixed
{
	public interface IFixedBuffer
	{
		int        Capacity    { get; }
		int        ElementSize { get; }
		int        Length      { get; set; }
		
		Span<byte> Raw         { get; }
	}

	public interface IFixedBuffer<T> : IFixedBuffer
		where T : IEquatable<T>
	{
		Span<T>          Span        { get; }
	}
}