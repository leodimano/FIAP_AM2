/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections.Generic;
using TObject.Shared;
using System.Globalization;
using UnityEngine;
using ClipperLib;
using X_UniTMX.Utils;

namespace X_UniTMX
{
	/// <summary>
	/// A TileObject is a simple Object placed in one Tile in the TileSet
	/// </summary>
	public class TileObject : Object
	{
		/// <summary>
		/// As ClipperLib only works with integers, we must multiply the points by a factor to keep the precision
		/// </summary>
		public static int ClipperScale = 100000;

		/// <summary>
		/// Creates a TileObject from node
		/// </summary>
		/// <param name="node">XML node to parse</param>
		public TileObject(NanoXMLNode node) : base(node) { }

		// TODO: Implement rotation! -> ok, you can rotate inside tiled...
		/// <summary>
		/// Generates Clipper's Path. As Clipper uses Integer for calculations, all point values are pre-multiplied by a factor of ClipperScale to minimize precision lost.
		/// </summary>
		/// <param name="tileX">Tile X position in the scene</param>
		/// <param name="tileY">Tile Y position in the scene</param>
		/// <param name="spriteEffects">The Tile's SpriteEffects in the TileLayer to generate its correct path if it is rotated.</param>
		/// <param name="tiledObjectEllipsePrecision">Precision factor for Ellipses. Higher = more vertices but a more precise ellipsoid</param>
		/// <returns>List of IntPoints to be used with ClipperLib functions</returns>
		public List<IntPoint> GetPath(int tileX, int tileY, SpriteEffects spriteEffects, int tiledObjectEllipsePrecision = 16) {
			List<IntPoint> path = new List<IntPoint>();
			// First we copy this object's point to a list, generating Box points' if needed and approximating an ellipse
			List<Vector2> points = new List<Vector2>();
			switch (ObjectType)
			{
				case ObjectType.Box:
					points.Add(new Vector2(Bounds.xMin, Bounds.yMin));
					points.Add(new Vector2(Bounds.xMax, Bounds.yMin));
					points.Add(new Vector2(Bounds.xMax, Bounds.yMax));
					points.Add(new Vector2(Bounds.xMin, Bounds.yMax));
					break;
				case ObjectType.Ellipse:
					#region Approximate Ellipse
					// Segments per quadrant
					int incFactor = Mathf.FloorToInt(tiledObjectEllipsePrecision / 4.0f);
					float minIncrement = 2 * Mathf.PI / (incFactor * tiledObjectEllipsePrecision / 2.0f);
					int currentInc = 0;
					// grow represents if we are going right on x-axis (true) or left (false)
					bool grow = true;

					//points = new List<Vector2>(tiledObjectEllipsePrecision);
					// Ellipsoide center
					Vector2 center = new Vector2(Bounds.x + Bounds.width / 2.0f, Bounds.y + Bounds.height / 2.0f);

					float r = 0;
					float angle = 0;
					for (int i = 0; i < tiledObjectEllipsePrecision; i++)
					{
						// Calculate radius at each point
						angle += currentInc * minIncrement;

						r = Bounds.width * Bounds.height / Mathf.Sqrt(Mathf.Pow(Bounds.height * Mathf.Cos(angle), 2) + Mathf.Pow(Bounds.width * Mathf.Sin(angle), 2)) / 2.0f;

						// Define the point localization using the calculated radius, angle and center
						points.Add(r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) + center);

						// if we are "growing", increment the angle, else, start decrementing it to close the polygon
						if (grow)
							currentInc++;
						else
							currentInc--;
						if (currentInc > incFactor - 1 || currentInc < 1)
							grow = !grow;
					}
					#endregion
					break;
				case ObjectType.Polygon:
				case ObjectType.Polyline:
					for (int i = 0; i < Points.Count; i++)
					{
						points.Add(new Vector2(Points[i].x + Bounds.x, Points[i].y + Bounds.y));
					}
					break;
			}

			// Then, rotate / flip if needed
			Vector2 flipAnchor = Vector2.one;
			Vector2 rotateAnchor = new Vector2(0.5f, 0.5f);
			if (Rotation != 0)
			{
				for (int i = 0; i < points.Count; i++)
				{
					points[i] = points[i].RotatePoint(Bounds.x, Bounds.y, Rotation * Mathf.Deg2Rad);
				}
			}

			if (spriteEffects != null)
			{
				if (spriteEffects.flippedAntiDiagonally || spriteEffects.flippedHorizontally || spriteEffects.flippedVertically)
				{
					if (spriteEffects.flippedHorizontally == true &&
						spriteEffects.flippedVertically == false &&
						spriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].FlipPointHorizontally(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == false &&
						spriteEffects.flippedVertically == true &&
						spriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].FlipPointVertically(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == true &&
						spriteEffects.flippedVertically == true &&
						spriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].FlipPointDiagonally(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == false &&
						spriteEffects.flippedVertically == false &&
						spriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].RotatePoint(rotateAnchor, 90 * Mathf.Deg2Rad);
							//points[i] = points[i].FlipPointDiagonally(flipAnchor);
							points[i] = points[i].FlipPointVertically(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == true &&
						spriteEffects.flippedVertically == false &&
						spriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].RotatePoint(rotateAnchor, 90 * Mathf.Deg2Rad);
							points[i] = points[i].FlipPointDiagonally(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == false &&
						spriteEffects.flippedVertically == true &&
						spriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].RotatePoint(rotateAnchor, -90 * Mathf.Deg2Rad);
							points[i] = points[i].FlipPointDiagonally(flipAnchor);
						}
					}

					if (spriteEffects.flippedHorizontally == true &&
						spriteEffects.flippedVertically == true &&
						spriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < points.Count; i++)
						{
							points[i] = points[i].RotatePoint(rotateAnchor, -90 * Mathf.Deg2Rad);
							points[i] = points[i].FlipPointVertically(flipAnchor);
						}
					}
				}
			}

			// Finally, position the points using tileX and tileY position, apply ClipperScale and add to the path
			for (int i = 0; i < points.Count; i++)
			{
				path.Add(
					new IntPoint(
						(points[i].x + tileX) * ClipperScale, 
						(points[i].y + tileY) * ClipperScale
					)
				);
			}

			return path;
		}
	}
}
