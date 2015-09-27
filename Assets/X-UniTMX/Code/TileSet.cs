/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using TObject.Shared;
using System.IO;
using UnityEngine;
using System.Collections;

namespace X_UniTMX
{
	/// <summary>
	/// A Container for a Tile Set properties and its Tiles.
	/// </summary>
	public class TileSet
	{
		/// <summary>
		/// This TileSet's First ID
		/// </summary>
		public int FirstId;
		
		/// <summary>
		/// This TileSet's Name
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Width of the Tile in this set
		/// </summary>
		public int TileWidth;
		
		/// <summary>
		/// Height of the Tile in this set
		/// </summary>
		public int TileHeight;
		
		/// <summary>
		/// Spacing in pixels between Tiles
		/// </summary>
		public int Spacing = 0;
		
		/// <summary>
		/// Margin in pixels from Tiles to border of Texture
		/// </summary>
		public int Margin = 0;
		
		/// <summary>
		/// Offset in pixels in X Axis to draw the tiles from this tileset
		/// </summary>
		public int TileOffsetX = 0;
		
		/// <summary>
		/// Offset in pixels in Y Axis to draw the tiles from this tileset
		/// </summary>
		public int TileOffsetY = 0;

		/// <summary>
		/// This TileSet's image (sprite) path
		/// </summary>
		public string Image;

		/// <summary>
		/// Color to set as transparency
		/// </summary>
		public Color? ColorKey;

		/// <summary>
		/// This TileSet's loaded Texture
		/// </summary>
		public Texture2D Texture;

		/// <summary>
		/// Dictionary of tiles in this tileset. Key = Tile ID, Value = Tile reference
		/// </summary>
		public Dictionary<int, Tile> Tiles = new Dictionary<int, Tile>();

		/// <summary>
		/// Dictionary of tile objects in this tileset. Key = Tile ID, Value = TileObject reference
		/// </summary>
		public Dictionary<int, List<TileObject>> TilesObjects = new Dictionary<int, List<TileObject>>();

		/// <summary>
		/// Dictionary of tile properties in this tileset. Key = Tile ID, Value = PropertyCollection reference
		/// </summary>
		public Dictionary<int, PropertyCollection> TileProperties = new Dictionary<int, PropertyCollection>();

		/// <summary>
		/// Dictionary of animated tiles in this tileset. Key = Tile ID, Value = TileAnimation reference
		/// </summary>
		public Dictionary<int, TileAnimation> AnimatedTiles = new Dictionary<int, TileAnimation>();

		/// <summary>
		/// Delegate to call when this tileset finishes loading
		/// </summary>
		Action<TileSet> OnFinishedLoadingTileSet = null;

		/// <summary>
		/// Load this TileSet's information from node
		/// </summary>
		/// <param name="node">NanoXMLNode to parse</param>
		/// <param name="map">Reference to the Map this TileSet is in</param>
		/// <param name="firstGID">First ID is a per-Map property, so External TileSets won't have this info in the node</param>
		protected TileSet(NanoXMLNode node, Map map, int firstGID = 1)
		{
			if (node.GetAttribute("firstgid") == null || !int.TryParse(node.GetAttribute("firstgid").Value, out FirstId))
				FirstId = firstGID;
			
			//this.FirstId = int.Parse(node.GetAttribute("firstgid").Value, CultureInfo.InvariantCulture);
			this.Name = node.GetAttribute("name").Value;
			this.TileWidth = int.Parse(node.GetAttribute("tilewidth").Value, CultureInfo.InvariantCulture);
			this.TileHeight = int.Parse(node.GetAttribute("tileheight").Value, CultureInfo.InvariantCulture);

			if (node.GetAttribute("spacing") != null)
			{
				this.Spacing = int.Parse(node.GetAttribute("spacing").Value, CultureInfo.InvariantCulture);
			}

			if (node.GetAttribute("margin") != null)
			{
				this.Margin = int.Parse(node.GetAttribute("margin").Value, CultureInfo.InvariantCulture);
			}

			NanoXMLNode tileOffset = node["tileoffset"];
			if (tileOffset != null)
			{
				this.TileOffsetX = int.Parse(tileOffset.GetAttribute("x").Value, CultureInfo.InvariantCulture);
				this.TileOffsetY = -int.Parse(tileOffset.GetAttribute("y").Value, CultureInfo.InvariantCulture);
			}

			NanoXMLNode imageNode = node["image"];
			this.Image = imageNode.GetAttribute("source").Value;


			// if the image is in any director up from us, just take the filename
			//if (this.Image.StartsWith(".."))
			//	this.Image = Path.GetFileName(this.Image);

			if (imageNode.GetAttribute("trans") != null)
			{
				string color = imageNode.GetAttribute("trans").Value;
				string r = color.Substring(0, 2);
				string g = color.Substring(2, 2);
				string b = color.Substring(4, 2);
				this.ColorKey = new Color((byte)Convert.ToInt32(r, 16), (byte)Convert.ToInt32(g, 16), (byte)Convert.ToInt32(b, 16));
			}
			foreach (NanoXMLNode subNode in node.SubNodes)
			{
				if (subNode.Name.Equals("tile"))
				{
					int id = this.FirstId + int.Parse(subNode.GetAttribute("id").Value, CultureInfo.InvariantCulture);
					
					// Load Tile Properties, if any
					NanoXMLNode propertiesNode = subNode["properties"];
					if (propertiesNode != null)
					{
						PropertyCollection properties = new PropertyCollection(propertiesNode);//Property.ReadProperties(propertiesNode);
						this.TileProperties.Add(id, properties);
					}

					// Load Tile Animation, if any
					NanoXMLNode animationNode = subNode["animation"];
					if (animationNode != null)
					{

						TileAnimation _tileAnimation = new TileAnimation();
						foreach (NanoXMLNode frame in animationNode.SubNodes)
						{
							if (!frame.Name.Equals("frame"))
								continue;
							int tileid = int.Parse(frame.GetAttribute("tileid").Value, CultureInfo.InvariantCulture) + FirstId;
							int duration = int.Parse(frame.GetAttribute("duration").Value, CultureInfo.InvariantCulture);
							_tileAnimation.AddTileFrame(tileid, duration);
						}
						this.AnimatedTiles.Add(id, _tileAnimation);
					}

					// Load Tile Objects, if any
					NanoXMLNode objectsNode = subNode["objectgroup"];
					if (objectsNode != null)
					{
						List<TileObject> tileObjects = new List<TileObject>();
						foreach (NanoXMLNode tileObjNode in objectsNode.SubNodes)
						{
							TileObject tObj = new TileObject(tileObjNode);
							tObj.ScaleObject(map.TileWidth, map.TileHeight, map.Orientation);
							tileObjects.Add(tObj);
						}
						// There's a bug in Tiled 0.10.1- where the objectgroup node won't go away even if you delete all objects from a tile's collision group.
						if(tileObjects.Count > 0)
							TilesObjects.Add(id, tileObjects);
					}
				}
			}
		}

		/// <summary>
		/// Load this TileSet's information from node and builds its tiles
		/// </summary>
		/// <param name="node">NanoXMLNode to parse</param>
		/// <param name="mapPath">Map's directory</param>
		/// <param name="map">Reference to the Map this TileSet is in</param>
		/// <param name="isUsingStreamingPath">true if is using StreamingAssets path or HTTP URL (WWW)</param>
		/// <param name="onFinishedLoadingTileSet">Delegate to call when this TileSet finishes loading</param>
		/// <param name="firstID">First ID is a per-Map property, so External TileSets won't have this info in the node</param>
		public TileSet(NanoXMLNode node, string mapPath, Map map, bool isUsingStreamingPath = false, Action<TileSet> onFinishedLoadingTileSet = null, int firstID = 1) 
			: this(node, map, firstID)
		{
			// Build tiles from this tileset
			string texturePath = mapPath;
			// Parse the path
			if (Image.StartsWith("../"))
			{
				string path = Image;
				string rootPath = mapPath;
				string appPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));

				while (path.StartsWith("../"))
				{
					if (rootPath.EndsWith("/")) {
						rootPath = rootPath.Remove(rootPath.Length - 1, 1);
					}
					rootPath = Directory.GetParent(rootPath).FullName;
					path = path.Remove(0, 3);
				}
				rootPath = rootPath.Replace(appPath + Path.DirectorySeparatorChar, "");
				path = Path.GetDirectoryName(path) + Path.AltDirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
				if (path.StartsWith("/"))
					path = path.Remove(0, 1);
				rootPath = rootPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if (rootPath.Length > 0)
					rootPath += Path.AltDirectorySeparatorChar;
				texturePath = string.Concat(rootPath, path);
			}
			else if (!Application.isWebPlayer)
			{
				if (!texturePath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
					texturePath += Path.AltDirectorySeparatorChar;
				if (texturePath.Equals("/")) texturePath = "";

				if (Path.GetDirectoryName(this.Image).Length > 0)
					texturePath += Path.GetDirectoryName(this.Image) + Path.AltDirectorySeparatorChar;
				if (texturePath.Equals("/")) texturePath = "";

				texturePath = string.Concat(texturePath, Path.GetFileNameWithoutExtension(this.Image));
			}
			else
			{
				texturePath = Path.Combine(mapPath, Path.GetFileName(this.Image));
			}

			OnFinishedLoadingTileSet = onFinishedLoadingTileSet;
			//Debug.Log(texturePath);

			if (!isUsingStreamingPath)
			{
				//texturePath = string.Concat(texturePath, Path.GetExtension(this.Image));
				
				this.Texture = Resources.Load<Texture2D>(texturePath);
				BuildTiles(map.TileWidth);
			}
			else
			{
				texturePath = string.Concat(texturePath, Path.GetExtension(this.Image));
				
				if (!texturePath.Contains("://"))
					texturePath = string.Concat("file://", texturePath);
				
				//Debug.Log(texturePath);
				// Run Coroutine for WWW using TaskManager.
				new X_UniTMX.Utils.Task(LoadTileSetTexture(texturePath, map.TileWidth), true);
			}
		}

		IEnumerator LoadTileSetTexture(string path, int mapTileWidth)
		{
			//Debug.Log(path);
			WWW www = new WWW(path);
			yield return www;
			Texture = www.texture;
			BuildTiles(mapTileWidth);
		}

		void BuildTiles(int mapTileWidth)
		{
			// figure out how many frames fit on the X axis
			int frameCountX = -(2 * Margin - Spacing - this.Texture.width) / (TileWidth + Spacing);

			// figure out how many frames fit on the Y axis
			int frameCountY = -(2 * Margin - Spacing - this.Texture.height) / (TileHeight + Spacing);

			// make our tiles. tiles are numbered by row, left to right.
			for (int y = 0; y < frameCountY; y++)
			{
				for (int x = 0; x < frameCountX; x++)
				{
					//Tile tile = new Tile();

					// calculate the source rectangle
					int rx = Margin + x * (TileWidth + Spacing);
					int ry = Texture.height + Margin - (Margin + (y + 1) * (TileHeight + Spacing));
					Rect Source = new Rect(rx, ry, TileWidth, TileHeight);
					//Debug.Log(Source);
					// get any properties from the tile set
					int index = FirstId + (y * frameCountX + x);
					PropertyCollection Properties = new PropertyCollection();
					if (TileProperties.ContainsKey(index))
					{
						Properties = TileProperties[index];
					}

					// save the tile
					Tiles.Add(index, new Tile(this, Source, index, Properties, Vector2.zero, mapTileWidth));
				}
			}
			if (OnFinishedLoadingTileSet != null)
				OnFinishedLoadingTileSet(this);
		}
	}

}
