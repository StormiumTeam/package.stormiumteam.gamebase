public struct DataBufferMarker
{
	public bool Valid;
	public int  Index;

	public DataBufferMarker(int index)
	{
		Index = index;
		Valid = true;
	}

	public DataBufferMarker GetOffset(int offset)
	{
		return new DataBufferMarker(Index + offset);
	}
}