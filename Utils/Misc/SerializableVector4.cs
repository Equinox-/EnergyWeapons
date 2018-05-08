using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public struct SerializableVector4
    {
        [XmlAttribute]
        public float X;

        [XmlAttribute]
        public float Y;

        [XmlAttribute]
        public float Z;

        [XmlAttribute]
        public float W;

        public static implicit operator Vector4(SerializableVector4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static implicit operator SerializableVector4(Vector4 v)
        {
            return new SerializableVector4() {X = v.X, Y = v.Y, Z = v.Z, W = v.W};
        }
    }
}