using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equinox.Utils.Components
{
    public interface IDebugComponent
    {
        void Debug(StringBuilder sb);
    }

    public static class DebugComponentExtensions
    {
        public static bool DebugWithType(this IDebugComponent c, StringBuilder sb)
        {
            var s = c.GetType().Name;
            sb.Append(s).Append(": ");
            var i = sb.Length;
            c.Debug(sb);
            if (sb.Length == i)
            {
                var del = s.Length + ": ".Length;
                sb.Remove(sb.Length - del, del);
                return false;
            }
            return true;
        }
    }
}