﻿// IMatchFinder.cs

using System;

namespace SevenZip.Compression.LZ
{
	internal interface IInWindowStream
	{
		void SetStream(System.IO.Stream? inStream);
		void Init();
		void ReleaseStream();
        byte GetIndexByte(int index);
        uint GetMatchLen(int index, uint distance, uint limit);
        uint GetNumAvailableBytes();
	}

	internal interface IMatchFinder : IInWindowStream
	{
		void Create(uint historySize, uint keepAddBufferBefore,
                uint matchMaxLen, uint keepAddBufferAfter);
        uint GetMatches(uint[] distances);
		void Skip(uint num);
	}
}
