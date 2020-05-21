using UnityEngine;
using System.Runtime.Serialization;

namespace Voxel.Saving
{
    public class QuaternionSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Quaternion quaternion = (Quaternion)obj;
            info.AddValue("X", quaternion.x);
            info.AddValue("Y", quaternion.y);
            info.AddValue("Z", quaternion.z);
            info.AddValue("W", quaternion.w);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return new Quaternion
            {
                x = (float)info.GetValue("X", typeof(float)),
                y = (float)info.GetValue("Y", typeof(float)),
                z = (float)info.GetValue("Z", typeof(float)),
                w = (float)info.GetValue("W", typeof(float))
            };
        }
    }
}