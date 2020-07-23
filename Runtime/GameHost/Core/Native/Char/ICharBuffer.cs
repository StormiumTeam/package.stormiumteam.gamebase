﻿using System;

namespace GameHost.Native
{
	public interface ICharBuffer
	{
		int        Capacity { get; }
		int        Length   { get; set; }
		Span<char> Span     { get; }
	}
}