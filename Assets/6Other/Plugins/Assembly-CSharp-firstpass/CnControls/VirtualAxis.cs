namespace CnControls
{
	public class VirtualAxis
	{
		public string Name { get; set; }

		public float Value { get; set; }

		public VirtualAxis(string name)
		{
			Name = name;
		}
	}
}
