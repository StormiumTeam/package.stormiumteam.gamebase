﻿using System;

 namespace GameHost.Native
 {
	 public interface ICharBuffer
	 {
		 int          Capacity { get; }
		 int          Length   { get; set; }
		 unsafe char* Begin    { get; }
		 Span<char>   Span     { get; }
	 }
 }