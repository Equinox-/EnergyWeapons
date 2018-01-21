using System;
using System.Collections.Generic;
using Equinox.Utils.Logging;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace Equinox.Utils.Render
{
    public class Icosphere
    {
        private readonly Vector3[] _vertexBuffer;
        private readonly int[][] _indexBuffer;

        public Icosphere(int lods)
        {
            float X = 0.525731112119133606f;
            float Z = 0.850650808352039932f;
            Vector3[] data =
            {
                new Vector3(-X, 0, Z), new Vector3(X, 0, Z), new Vector3(-X, 0, -Z), new Vector3(X, 0, -Z),
                new Vector3(0, Z, X), new Vector3(0, Z, -X), new Vector3(0, -Z, X), new Vector3(0, -Z, -X),
                new Vector3(Z, X, 0), new Vector3(-Z, X, 0), new Vector3(Z, -X, 0), new Vector3(-Z, -X, 0)
            };
            List<Vector3> points = new List<Vector3>(12 * (1 << (lods - 1)));
            points.AddRange(data);
            int[][] index = new int[lods][];
            index[0] = new int[]
            {
                0, 4, 1, 0, 9, 4, 9, 5, 4, 4, 5, 8, 4, 8, 1,
                8, 10, 1, 8, 3, 10, 5, 3, 8, 5, 2, 3, 2, 7, 3, 7, 10, 3, 7,
                6, 10, 7, 11, 6, 11, 0, 6, 0, 1, 6, 6, 1, 10, 9, 0, 11, 9,
                11, 2, 9, 2, 5, 7, 2, 11
            };
            for (int i = 1; i < lods; i++)
                index[i] = Subdivide(points, index[i - 1]);

            _indexBuffer = index;
            _vertexBuffer = points.ToArray();
        }

        private static Vector2 WorldToScreen(Vector3D v)
        {
            var tmp = MyAPIGateway.Session.Camera.WorldToScreen(ref v);
            var sf = new Vector2((float) ((tmp.X + 1) / 2), (float) ((tmp.Y + 1) / 2));
            return Vector2.Clamp(sf, new Vector2(0.25f), new Vector2(0.75f)) * MyAPIGateway.Session.Camera.ViewportSize;
        }

        public void DrawAuto(MatrixD matrix, float radius, Vector4 color,
            MyStringId? faceMaterial = null, MyStringId? lineMaterial = null, float lineThickness = -1f)
        {
            for (var id = 0; id < _indexBuffer[0].Length - 2; id += 3)
                DrawTriangle(0, id, radius, ref matrix, ref color, faceMaterial, lineMaterial, lineThickness);
        }

        private void DrawTriangle(int lod, int id, float radius, ref MatrixD matrix, ref Vector4 color,
            MyStringId? faceMaterial, MyStringId? lineMaterial, float lineThickness)
        {
            var i0 = _indexBuffer[lod][id];
            var i1 = _indexBuffer[lod][id + 1];
            var i2 = _indexBuffer[lod][id + 2];

            var v0 = Vector3D.Transform(radius * _vertexBuffer[i0], matrix);
            var v1 = Vector3D.Transform(radius * _vertexBuffer[i1], matrix);
            var v2 = Vector3D.Transform(radius * _vertexBuffer[i2], matrix);


            var n0 = Vector3D.TransformNormal(_vertexBuffer[i0], matrix);
            var n1 = Vector3D.TransformNormal(_vertexBuffer[i1], matrix);
            var n2 = Vector3D.TransformNormal(_vertexBuffer[i2], matrix);

            var s0 = WorldToScreen(v0);
            var s1 = WorldToScreen(v1);
            var s2 = WorldToScreen(v2);

            var sSize = Vector2.Max(s0, Vector2.Max(s1, s2)) - Vector2.Min(s0, Vector2.Min(s1, s2));
            var size = (v0 - v1).LengthSquared() /
                       (MyAPIGateway.Session.Camera.Position - (v0 + v1 + v2) / 3).Length();
            if (size < 1 || lod == _indexBuffer.Length - 1)
            {
                // draw
                if (faceMaterial.HasValue)
                    MyTransparentGeometry.AddTriangleBillboard(v0, v1, v2, n0, n1, n2, Vector2.Zero, Vector2.Zero,
                        Vector2.Zero, faceMaterial.Value, 0,
                        (v0 + v1 + v2) / 3, color);
                if (lineMaterial.HasValue && lineThickness > 0)
                {
                    MySimpleObjectDraw.DrawLine(v0, v1, lineMaterial, ref color, lineThickness);
                    MySimpleObjectDraw.DrawLine(v1, v2, lineMaterial, ref color, lineThickness);
                    MySimpleObjectDraw.DrawLine(v2, v0, lineMaterial, ref color, lineThickness);
                }
            }
            else
            {
                // subdivide
                var ni = id * 4;
                DrawTriangle(lod + 1, ni, radius, ref matrix, ref color, faceMaterial, lineMaterial, lineThickness);
                DrawTriangle(lod + 1, ni + 3, radius, ref matrix, ref color, faceMaterial, lineMaterial, lineThickness);
                DrawTriangle(lod + 1, ni + 6, radius, ref matrix, ref color, faceMaterial, lineMaterial, lineThickness);
                DrawTriangle(lod + 1, ni + 9, radius, ref matrix, ref color, faceMaterial, lineMaterial, lineThickness);
            }
        }

        public void Draw(MatrixD matrix, float radius, int lod, Vector4 color, MyStringId? faceMaterial = null,
            MyStringId? lineMaterial = null, float lineThickness = -1f)
        {
            var ix = _indexBuffer[lod];
            for (var i = 0; i < ix.Length - 2; i += 3)
            {
                var i0 = ix[i];
                var i1 = ix[i + 1];
                var i2 = ix[i + 2];

                var v0 = Vector3D.Transform(radius * _vertexBuffer[i0], matrix);
                var v1 = Vector3D.Transform(radius * _vertexBuffer[i1], matrix);
                var v2 = Vector3D.Transform(radius * _vertexBuffer[i2], matrix);

                var n = (_vertexBuffer[i0] + _vertexBuffer[i1] + _vertexBuffer[i2]) / 3;
                if (faceMaterial.HasValue)
                    MyTransparentGeometry.AddTriangleBillboard(v0, v1, v2, _vertexBuffer[i0], _vertexBuffer[i1],
                        _vertexBuffer[i2], Vector2.Zero, Vector2.Zero, Vector2.Zero, faceMaterial.Value, 0,
                        (v0 + v1 + v2) / 3, color);
                if (lineMaterial.HasValue && lineThickness > 0)
                {
                    MySimpleObjectDraw.DrawLine(v0, v1, lineMaterial, ref color, lineThickness);
                    MySimpleObjectDraw.DrawLine(v1, v2, lineMaterial, ref color, lineThickness);
                    MySimpleObjectDraw.DrawLine(v2, v0, lineMaterial, ref color, lineThickness);
                }
            }
        }

        private static int SubdividedAddress(IList<Vector3> pts, IDictionary<string, int> assoc, int a, int b)
        {
            string key = a < b ? (a + "_" + b) : (b + "_" + a);
            int res;
            if (assoc.TryGetValue(key, out res))
                return res;
            var np = pts[a] + pts[b];
            np.Normalize();
            pts.Add(np);
            assoc.Add(key, pts.Count - 1);
            return pts.Count - 1;
        }

        private static int[] Subdivide(IList<Vector3> vbuffer, IReadOnlyList<int> prevLod)
        {
            // LOD 1 needs 30 more than LOD 0.
            // LOD 2 needs 120 more than LOD 1.
            // LOD 3 needs 480 more than LOD 2. etc...
            Dictionary<string, int> assoc = new Dictionary<string, int>();
            int[] res = new int[prevLod.Count * 4];
            int rI = 0;
            var before = vbuffer.Count;
            for (int i = 0; i < prevLod.Count; i += 3)
            {
                int v1 = prevLod[i];
                int v2 = prevLod[i + 1];
                int v3 = prevLod[i + 2];
                int v12 = SubdividedAddress(vbuffer, assoc, v1, v2);
                int v23 = SubdividedAddress(vbuffer, assoc, v2, v3);
                int v31 = SubdividedAddress(vbuffer, assoc, v3, v1);

                res[rI++] = v1;
                res[rI++] = v12;
                res[rI++] = v31;

                res[rI++] = v2;
                res[rI++] = v23;
                res[rI++] = v12;

                res[rI++] = v3;
                res[rI++] = v31;
                res[rI++] = v23;

                res[rI++] = v12;
                res[rI++] = v23;
                res[rI++] = v31;
            }

            var after = vbuffer.Count;
            EnergyWeapons.EnergyWeaponsCore.LoggerStatic?.Info($"LOD changed vcount from {before} to {after}");

            return res;
        }


        /// <summary>
        /// Calculates the number of vertices in the given LOD
        /// </summary>
        public static long VertsForLod(int lod)
        {
            var shift = lod * 2;
            var k = (1L << shift) - 1;
            return 12 + 30 * (k & 0x5555555555555555L);
        }

        public class Instance
        {
            private readonly Icosphere _backing;

            private Vector3D[] _vertexBuffer;
            private Vector3D[] _normalBuffer;
            private Vector4[] _triColorBuffer;

            public Instance(Icosphere backing)
            {
                _backing = backing;
            }

            private int _lod;

            public void CalculateTransform(MatrixD matrix, int lod)
            {
                _lod = lod;
                var count = checked((int) VertsForLod(lod));
                Array.Resize(ref _vertexBuffer, count);
                Array.Resize(ref _normalBuffer, count);

                var normalMatrix = MatrixD.Transpose(MatrixD.Invert(matrix.GetOrientation()));

                for (var i = 0; i < count; i++)
                    Vector3D.Transform(ref _backing._vertexBuffer[i], ref matrix, out _vertexBuffer[i]);

                for (var i = 0; i < count; i++)
                    Vector3D.TransformNormal(ref _backing._vertexBuffer[i], ref normalMatrix, out _normalBuffer[i]);
            }

            public void CalculateColor()
            {
                var ib = _backing._indexBuffer[_lod];
                Array.Resize(ref _triColorBuffer, ib.Length / 3);
                for (int i = 0, j = 0; i < ib.Length; i += 3, j++)
                {
                    var i0 = ib[i];
                    var i1 = ib[i + 1];
                    var i2 = ib[i + 2];

                    var v0 = _vertexBuffer[i0];
                    var v1 = _vertexBuffer[i1];
                    var v2 = _vertexBuffer[i2];

                    _triColorBuffer[j] = Vector4.One; // your color
                }
            }

            public void Draw(MyStringId? faceMaterial = null, MyStringId? lineMaterial = null,
                float lineThickness = -1f)
            {
                var ib = _backing._indexBuffer[_lod];
                for (int i = 0, j = 0; i < ib.Length; i += 3, j++)
                {
                    var i0 = ib[i];
                    var i1 = ib[i + 1];
                    var i2 = ib[i + 2];

                    var v0 = _vertexBuffer[i0];
                    var v1 = _vertexBuffer[i1];
                    var v2 = _vertexBuffer[i2];

                    var n0 = _normalBuffer[i0];
                    var n1 = _normalBuffer[i1];
                    var n2 = _normalBuffer[i2];

                    var color = _triColorBuffer[j];

                    if (faceMaterial.HasValue)
                        MyTransparentGeometry.AddTriangleBillboard(v0, v1, v2, n0, n1, n2, Vector2.Zero, Vector2.Zero,
                            Vector2.Zero, faceMaterial.Value, 0,
                            (v0 + v1 + v2) / 3, color);
                    if (lineMaterial.HasValue && lineThickness > 0)
                    {
                        MySimpleObjectDraw.DrawLine(v0, v1, lineMaterial, ref color, lineThickness);
                        MySimpleObjectDraw.DrawLine(v1, v2, lineMaterial, ref color, lineThickness);
                        MySimpleObjectDraw.DrawLine(v2, v0, lineMaterial, ref color, lineThickness);
                    }
                }
            }
        }
    }
}