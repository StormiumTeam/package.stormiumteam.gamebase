namespace GameHost
{
	// need to be synchronized with GameHost enum structure
	public enum EMessageType
	{
		Unknown        = 0,
		Rpc            = 10,
		InputData      = 50,
		SimulationData = 100
	}
}