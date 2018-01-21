using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class CollectionExtensions
    {
        public static void SetContents<T>(this List<T> dest, ICollection<T> source)
        {
            int sizeFinal = MathHelper.GetNearestBiggerPowerOfTwo(source.Count);
            if (dest.Capacity < sizeFinal || dest.Capacity * 8 > sizeFinal)
                dest.Capacity = sizeFinal;
            source.CopyTo(dest.GetInternalArray(), 0);
            dest.SetSize(source.Count);
        }
    }
}