using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Equinox.Utils.Render
{
    public static class TransparentRenderExt
    {
        public static void DrawTransparentCylinder(ref MatrixD worldMatrix, float radius1, float radius2, float length,
            Vector4 lineColor, Vector4 faceColor, int wireDivideRatio, float thickness,
            MyStringId? lineMaterial = null, MyStringId? faceMaterial = null)
        {
            Vector3D centerTop = Vector3D.Transform(new Vector3D(0, length / 2f, 0), ref worldMatrix);
            Vector3D centerBottom = Vector3D.Transform(new Vector3D(0, -length / 2f, 0), ref worldMatrix);

            Vector3D upDir = Vector3D.TransformNormal(new Vector3D(0, 1, 0), ref worldMatrix);
            upDir.Normalize();

            Vector3D currTop = Vector3D.Zero;
            Vector3D currBottom = Vector3D.Zero;
            Vector3D prevTop = Vector3D.Zero;
            Vector3D prevBottom = Vector3D.Zero;
            float num = 360f / wireDivideRatio;
            for (int i = 0; i <= wireDivideRatio; i++)
            {
                float degrees = i * num;
                currTop.X = (float) (radius1 * Math.Cos(MathHelper.ToRadians(degrees)));
                currTop.Y = length / 2f;
                currTop.Z = (float) (radius1 * Math.Sin(MathHelper.ToRadians(degrees)));
                currBottom.X = (float) (radius2 * Math.Cos(MathHelper.ToRadians(degrees)));
                currBottom.Y = -length / 2f;
                currBottom.Z = (float) (radius2 * Math.Sin(MathHelper.ToRadians(degrees)));
                currTop = Vector3D.Transform(currTop, worldMatrix);
                currBottom = Vector3D.Transform(currBottom, worldMatrix);

                if (lineMaterial.HasValue)
                    MySimpleObjectDraw.DrawLine(currBottom, currTop, lineMaterial, ref lineColor, thickness);

                if (i > 0)
                {
                    if (lineMaterial.HasValue)
                    {
                        MySimpleObjectDraw.DrawLine(prevBottom, currBottom, lineMaterial, ref lineColor, thickness);
                        MySimpleObjectDraw.DrawLine(prevTop, currTop, lineMaterial, ref lineColor, thickness);
                    }

                    if (faceMaterial.HasValue)
                    {
                        var quad = new MyQuadD()
                        {
                            Point0 = prevTop,
                            Point1 = currTop,
                            Point2 = currBottom,
                            Point3 = prevBottom
                        };
                        MyTransparentGeometry.AddQuad(faceMaterial.Value, ref quad, faceColor, ref currTop);

                        MyTransparentGeometry.AddTriangleBillboard(centerTop, currTop, prevTop, upDir, upDir, upDir,
                            Vector2.Zero, Vector2.Zero, Vector2.Zero, faceMaterial.Value, 0, currTop, faceColor);
                        MyTransparentGeometry.AddTriangleBillboard(centerBottom, currBottom, prevBottom, -upDir, -upDir, -upDir,
                            Vector2.Zero, Vector2.Zero, Vector2.Zero, faceMaterial.Value, 0, currBottom, faceColor);
                    }
                }

                prevBottom = currBottom;
                prevTop = currTop;
            }
        }
    }
}