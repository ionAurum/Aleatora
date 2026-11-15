using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// Static: the shapes the Melodies page's views draw their meshes from, in a Graphic's local
    /// space: rectangles, frames, thick polylines, discs, rings and triangles. Untextured.
    /// </summary>
    public static class ViewMesh
    {
        /// <summary>A filled rectangle from (x0, y0) to (x1, y1).</summary>
        public static void Rect(VertexHelper vh, float x0, float y0, float x1, float y1, Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), color, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), color, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), color, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        /// <summary>A rectangle's frame, width wide, drawn inside it.</summary>
        public static void Frame(VertexHelper vh, float x0, float y0, float x1, float y1, float width, Color32 color)
        {
            Rect(vh, x0, y0, x1, y0 + width, color);
            Rect(vh, x0, y1 - width, x1, y1, color);
            Rect(vh, x0, y0 + width, x0 + width, y1 - width, color);
            Rect(vh, x1 - width, y0 + width, x1, y1 - width, color);
        }

        /// <summary>
        /// A line width wide through the points, each joint along the mean of its two segments'
        /// normals: right for smooth curves, which is all the views draw.
        /// </summary>
        public static void Polyline(VertexHelper vh, IReadOnlyList<Vector2> points, float width, Color32 color)
        {
            int count = points.Count;
            if (count < 2)
            {
                return;
            }

            float half = width / 2f;
            int start = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 before = points[Mathf.Max(i - 1, 0)];
                Vector2 after = points[Mathf.Min(i + 1, count - 1)];
                Vector2 along = (after - before).normalized;
                var normal = new Vector2(-along.y, along.x);
                vh.AddVert(points[i] + normal * half, color, Vector2.zero);
                vh.AddVert(points[i] - normal * half, color, Vector2.zero);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = start + 2 * i;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }

        /// <summary>A filled triangle.</summary>
        public static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        /// <summary>A filled disc of this many sides.</summary>
        public static void Disc(VertexHelper vh, Vector2 centre, float radius, Color32 color, int sides = 40)
        {
            int start = vh.currentVertCount;
            vh.AddVert(centre, color, Vector2.zero);
            for (int i = 0; i < sides; i++)
            {
                float angle = 2f * Mathf.PI * i / sides;
                vh.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }

            for (int i = 0; i < sides; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % sides);
            }
        }

        /// <summary>The band between two circles round one centre.</summary>
        public static void Ring(VertexHelper vh, Vector2 centre, float inner, float outer, Color32 color, int sides = 40)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < sides; i++)
            {
                float angle = 2f * Mathf.PI * i / sides;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(centre + direction * outer, color, Vector2.zero);
                vh.AddVert(centre + direction * inner, color, Vector2.zero);
            }

            for (int i = 0; i < sides; i++)
            {
                int a = start + 2 * i;
                int b = start + 2 * ((i + 1) % sides);
                vh.AddTriangle(a, b, b + 1);
                vh.AddTriangle(a, b + 1, a + 1);
            }
        }
    }
}
