/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 *           
 *	RotatePoint function thanks to http://stackoverflow.com/questions/2259476/rotating-a-point-about-another-point-2d
 */
using UnityEngine;
using System.Collections;

namespace X_UniTMX.Utils
{
	/// <summary>
	/// Useful little mathematical functions
	/// </summary>
	public static class MathfExtensions
	{
		/// <summary>
		/// Calculate Mesh's Tangents
		/// </summary>
		/// <param name="mesh">mesh with tangents to be calculated</param>
		public static void CalculateMeshTangents(Mesh mesh)
		{
			//speed up math by copying the mesh arrays
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;

			//variable definitions
			int triangleCount = triangles.Length;
			int vertexCount = vertices.Length;

			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];

			Vector4[] tangents = new Vector4[vertexCount];

			for (long a = 0; a < triangleCount; a += 3)
			{
				long i1 = triangles[a + 0];
				long i2 = triangles[a + 1];
				long i3 = triangles[a + 2];

				if (i1 >= vertices.Length) break;
				Vector3 v1 = vertices[i1];
				if (i2 >= vertices.Length) break;
				Vector3 v2 = vertices[i2];
				if (i3 >= vertices.Length) break;
				Vector3 v3 = vertices[i3];

				if (i1 >= uv.Length) break;
				Vector2 w1 = uv[i1];
				if (i2 >= uv.Length) break;
				Vector2 w2 = uv[i2];
				if (i3 >= uv.Length) break;
				Vector2 w3 = uv[i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float r = 1.0f / (s1 * t2 - s2 * t1);

				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}


			for (long a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normals[a];
				Vector3 t = tan1[a];

				//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize(ref n, ref t);
				tangents[a].x = t.x;
				tangents[a].y = t.y;
				tangents[a].z = t.z;

				tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			}

			mesh.tangents = tangents;
		}

		#region Vector2 Flip Point
		/// <summary>
		/// Flips a point diagonally related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector2 FlipPointDiagonally(this Vector2 point, Vector2 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, true, true);
		}

		/// <summary>
		/// Flips a point horizontally related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector2 FlipPointHorizontally(this Vector2 point, Vector2 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, true, false);
		}

		/// <summary>
		/// Flips a point vertically related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector2 FlipPointVertically(this Vector2 point, Vector2 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, false, true);
		}

		/// <summary>
		/// Flips a point onto an anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <param name="flipX">true to flip horizontally</param>
		/// <param name="flipY">true to flip vertically</param>
		/// <returns>Flipped point</returns>
		public static Vector2 FlipPoint(this Vector2 point, Vector2 anchor, bool flipX, bool flipY)
		{
			return FlipPoint(point, anchor.x, anchor.y, flipX, flipY);
		}

		/// <summary>
		/// Flips a point onto an anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchorX">Anchor X position</param>
		/// <param name="anchorY">Anchor Y position</param>
		/// <param name="flipX">true to flip horizontally</param>
		/// <param name="flipY">true to flip vertically</param>
		/// <returns>Flipped point</returns>
		public static Vector2 FlipPoint(this Vector2 point, float anchorX, float anchorY, bool flipX, bool flipY)
		{
			if (flipX)
				point.x = anchorX - point.x;
			if (flipY)
				point.y = anchorY - point.y;
			return point;
		}
		#endregion

		#region Vector3 Flip Point
		/// <summary>
		/// Flips a point diagonally related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector3 FlipPointDiagonally(this Vector3 point, Vector3 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, anchor.z, true, true, false);
		}

		/// <summary>
		/// Flips a point horizontally related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector3 FlipPointHorizontally(this Vector3 point, Vector3 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, anchor.z, true, false, false);
		}

		/// <summary>
		/// Flips a point vertically related to the anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <returns>Flipped point</returns>
		public static Vector3 FlipPointVertically(this Vector3 point, Vector3 anchor)
		{
			return FlipPoint(point, anchor.x, anchor.y, anchor.z, false, true, false);
		}

		/// <summary>
		/// Flips a point onto an anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchor">anchor point position</param>
		/// <param name="flipX">true to flip horizontally</param>
		/// <param name="flipY">true to flip vertically</param>
		/// <param name="flipZ">true to flip in the z-axis</param>
		/// <returns>Flipped point</returns>
		public static Vector3 FlipPoint(this Vector3 point, Vector3 anchor, bool flipX, bool flipY, bool flipZ)
		{
			return FlipPoint(point, anchor.x, anchor.y, anchor.z, flipX, flipY, flipZ);
		}

		/// <summary>
		/// Flips a point onto an anchor point
		/// </summary>
		/// <param name="point">point to be flipped</param>
		/// <param name="anchorX">Anchor X position</param>
		/// <param name="anchorY">Anchor Y position</param>
		/// <param name="anchorZ">Anchor Z position</param>
		/// <param name="flipX">true to flip horizontally</param>
		/// <param name="flipY">true to flip vertically</param>
		/// <param name="flipZ">true to flip in the z-axis</param>
		/// <returns>Flipped point</returns>
		public static Vector3 FlipPoint(this Vector3 point, float anchorX, float anchorY, float anchorZ, bool flipX, bool flipY, bool flipZ)
		{
			if (flipX)
				point.x = anchorX - point.x + anchorX;
			if (flipY)
				point.y = anchorY - point.y + anchorY;
			if (flipZ)
				point.z = anchorZ - point.z + anchorZ;
			return point;
		}
		#endregion

		#region Rotate 2D Point
		/// <summary>
		/// Rotate a point along an anchor point
		/// </summary>
		/// <param name="point">Point to be rotated</param>
		/// <param name="anchor">Anchor point where rotation will occurr</param>
		/// <param name="angle">Angle in radians</param>
		/// <returns>Rotated point</returns>
		public static Vector2 RotatePoint(this Vector2 point, Vector2 anchor, float angle)
		{
			return RotatePoint(point, anchor.x, anchor.y, angle);
		}

		/// <summary>
		/// Rotate a point along an anchor point
		/// </summary>
		/// <param name="point">Point to be rotated</param>
		/// <param name="anchorX">Anchor X position</param>
		/// <param name="anchorY">Anchor Y position</param>
		/// <param name="angle">Angle in radians</param>
		/// <returns>Rotated point</returns>
		public static Vector2 RotatePoint(this Vector2 point, float anchorX, float anchorY, float angle)
		{
			float s = Mathf.Sin(angle);
			float c = Mathf.Cos(angle);

			// translate point back to origin:
			point.x -= anchorX;
			point.y -= anchorY;

			// rotate point
			//float xnew = point.x * c - point.y * s;
			//float ynew = -point.x * s + point.y * c;
			float xnew = point.y * s + point.x * c;
			float ynew = -point.x * s + point.y * c;

			// translate point back:
			point.x = xnew + anchorX;
			point.y = ynew + anchorY;

			return point;
		}
		#endregion

		#region Rotate 3D Point
		/// <summary>
		/// Rotate a point along an anchor point [thanks http://answers.unity3d.com/questions/532297/rotate-a-vector-around-a-certain-point.html]
		/// </summary>
		/// <param name="point">Point to be rotated</param>
		/// <param name="anchor">Anchor point where rotation will occurr</param>
		/// <param name="angle">Angles in radians</param>
		/// <returns>Rotated point</returns>
		public static Vector3 RotatePoint(this Vector3 point, Vector3 anchor, Vector3 angle)
		{
			Vector3 direction = point - anchor;

			direction = Quaternion.Euler(angle) * direction;

			point = direction + anchor;

			return point;
		}
		#endregion
	}
}