using UnityEngine;
using System.Runtime.Serialization;

namespace Voxel.Saving
{
    public class Vector3Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector3 vector3 = (Vector3)obj;
            info.AddValue("X", vector3.x);
            info.AddValue("Y", vector3.y);
            info.AddValue("Z", vector3.z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return new Vector3
            {
                x = (float)info.GetValue("X", typeof(float)),
                y = (float)info.GetValue("Y", typeof(float)),
                z = (float)info.GetValue("Z", typeof(float))
            };
        }
    }
}