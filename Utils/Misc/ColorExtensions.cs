using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class ColorExtensions
    {
        private static readonly Color[] _colors =
        {
            Color.MediumSeaGreen, Color.Lavender, Color.DarkViolet, Color.Salmon, Color.SlateBlue, Color.AntiqueWhite,
            Color.DarkGoldenrod, Color.DarkKhaki, Color.MediumBlue, Color.Magenta, Color.PapayaWhip, Color.Orange,
            Color.SandyBrown,
            Color.Pink, Color.LightCyan, Color.LightGray, Color.LemonChiffon, Color.LightSkyBlue, Color.Snow,
            Color.Gold, Color.OldLace, Color.PeachPuff, Color.Brown, Color.Linen, Color.Tomato, Color.MidnightBlue,
            Color.LightSalmon, Color.LimeGreen,
            Color.PowderBlue, Color.DarkSlateBlue, Color.LightYellow, Color.MediumVioletRed, Color.DarkOliveGreen,
            Color.Goldenrod, Color.Indigo, Color.DarkCyan, Color.LavenderBlush, Color.Cornsilk, Color.Ivory,
            Color.Coral, Color.DarkSeaGreen,
            Color.MediumSpringGreen, Color.Azure, Color.Transparent, Color.Orchid, Color.Chartreuse, Color.FloralWhite,
            Color.Gainsboro, Color.RoyalBlue, Color.CadetBlue, Color.DarkSalmon, Color.DarkMagenta, Color.Beige,
            Color.Bisque, Color.Plum,
            Color.OrangeRed, Color.Olive, Color.Firebrick, Color.SkyBlue, Color.IndianRed, Color.Fuchsia,
            Color.CornflowerBlue, Color.DarkOrange, Color.BurlyWood, Color.Moccasin, Color.PaleTurquoise,
            Color.DeepPink, Color.Yellow, Color.SaddleBrown,
            Color.Tan, Color.MediumSlateBlue, Color.Teal, Color.YellowGreen, Color.Peru, Color.MintCream, Color.Blue,
            Color.DarkRed, Color.ForestGreen, Color.RosyBrown, Color.SteelBlue, Color.White, Color.DarkOrchid,
            Color.Gray, Color.Violet,
            Color.Maroon, Color.WhiteSmoke, Color.BlueViolet, Color.DarkSlateGray, Color.MistyRose, Color.SeaGreen,
            Color.DodgerBlue, Color.OliveDrab, Color.BlanchedAlmond, Color.DarkBlue, Color.HotPink, Color.DarkTurquoise,
            Color.PaleGreen,
            Color.Khaki, Color.Lime, Color.Honeydew, Color.Aqua, Color.Aquamarine, Color.DimGray, Color.Navy,
            Color.PaleGoldenrod, Color.Cyan, Color.Purple, Color.LightSeaGreen, Color.GreenYellow, Color.AliceBlue,
            Color.LightBlue, Color.Red,
            Color.LightPink, Color.Crimson, Color.SpringGreen, Color.Black, Color.Thistle, Color.PaleVioletRed,
            Color.MediumTurquoise, Color.MediumPurple, Color.Sienna, Color.Chocolate, Color.DeepSkyBlue,
            Color.SlateGray, Color.LawnGreen,
            Color.SeaShell, Color.MediumOrchid, Color.Wheat, Color.DarkGreen, Color.LightSlateGray,
            Color.MediumAquamarine, Color.LightSteelBlue, Color.DarkGray, Color.Turquoise, Color.NavajoWhite,
            Color.LightCoral, Color.LightGoldenrodYellow,
            Color.GhostWhite, Color.LightGreen, Color.Green, Color.Silver
        };

        public static Color SeededColor(int x)
        {
            var s = x % _colors.Length;
            if (s < 0)
                return Color.Black;
            return _colors[s];
        }
    }
}