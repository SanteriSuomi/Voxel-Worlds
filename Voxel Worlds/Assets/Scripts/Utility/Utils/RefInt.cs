namespace Voxel.Utility
{
    /// <summary>
    /// Represents a single integer value that can be used like a reference type.
    /// </summary>
    public class RefInt
    {
        public int Value { get; set; }

        public RefInt(int value) => Value = value;

        public RefInt()
        {
        }
    }
}