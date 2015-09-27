/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections.Generic;
using X_UniTMX.Utils;

namespace X_UniTMX
{
	/// <summary>
	/// A simple Tile Frame, with reference to a Tile ID and it's duration
	/// </summary>
	public class TileFrame
	{
		/// <summary>
		/// Tile ID
		/// </summary>
		public int TileID;
		/// <summary>
		/// Frame's duration in milliseconds
		/// </summary>
		public float Duration;

		/// <summary>
		/// Creates a Tile Frame
		/// </summary>
		/// <param name="tileID">Tile's ID reference</param>
		/// <param name="duration">Duration of this frame in milliseconds</param>
		public TileFrame(int tileID, float duration)
		{
			TileID = tileID;
			Duration = duration;
		}
	}
	/// <summary>
	/// Tile animation helper class. This is used together with X_UniTMX.Utils.AnimatedSprite to animate tiles
	/// </summary>
	public class TileAnimation
	{
		/// <summary>
		/// List of tile animation frames
		/// </summary>
		public List<TileFrame> TileFrames;

		/// <summary>
		/// Add a tile to this animation frames
		/// </summary>
		/// <param name="tileID">Tile's ID</param>
		/// <param name="delay">Duration of this tile frame</param>
		public void AddTileFrame(int tileID, float delay)
		{
			TileFrame tf = new TileFrame(tileID, delay);
			if (TileFrames == null)
				TileFrames = new List<TileFrame>();

			TileFrames.Add(tf);
		}
	}
}
