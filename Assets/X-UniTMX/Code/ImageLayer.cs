/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections;
using TObject.Shared;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace X_UniTMX
{
	/// <summary>
	/// A Map Layer containing an Image (Sprite)
	/// </summary>
	public class ImageLayer : Layer
	{
		/// <summary>
		/// Image source string (path)
		/// </summary>
		public string Image;
		/// <summary>
		/// Loaded Texture2D from the path
		/// </summary>
		public Texture2D Texture;
		/// <summary>
		/// Transparent Color
		/// </summary>
		public Color? ColorKey;
		/// <summary>
		/// Parent transform of the Layer
		/// </summary>
		public Transform Parent;

		/// <summary>
		/// ImageLayer position
		/// </summary>
		public Vector2 Position = Vector2.zero;


		int SortingOrder = 0;
		int TileWidth = 0;

		/// <summary>
		/// Creates an Image Layer from node
		/// </summary>
		/// <param name="node">XML node to parse</param>
		/// <param name="map">ImageLayer parent Map</param>
		/// <param name="mapPath">Map's directory path</param>
		/// <param name="baseMaterial">Material to use on this SpriteRenderer</param>
		public ImageLayer(NanoXMLNode node, Map map, string mapPath, Material baseMaterial)
			: base(node)
		{
			NanoXMLNode imageNode = node["image"];
			this.Image = imageNode.GetAttribute("source").Value;

			if (node.GetAttribute("x") != null)
			{
				Position.x = float.Parse(node.GetAttribute("x").Value, NumberStyles.Float) / (float)map.TileWidth;
			}
			if (node.GetAttribute("y") != null)
			{
				Position.y = -float.Parse(node.GetAttribute("y").Value, NumberStyles.Float) / (float)map.TileHeight;
			}
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

			SortingOrder = map.DefaultSortingOrder - LayerDepth;
			Parent = map.MapObject.transform;
			TileWidth = map.TileWidth;

			string texturePath = mapPath;
			if (!map.UsingStreamingAssetsPath)
			{
				// Parse the path
				if (Image.StartsWith("../"))
				{
					string path = Image;
					string rootPath = Directory.GetParent(mapPath).FullName;
					string appPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));

					while (path.StartsWith("../"))
					{
						rootPath = Directory.GetParent(rootPath).FullName;
						path = path.Remove(0, 3);
					}
					rootPath = rootPath.Replace(appPath, "");
					
					path = Path.GetDirectoryName(path) + Path.AltDirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);

					if (path.StartsWith("/"))
						path = path.Remove(0, 1);
					rootPath = rootPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
					if (rootPath.Length > 0)
						rootPath += Path.AltDirectorySeparatorChar;
					texturePath = rootPath + path;
					
				}
				else
				{
					if (Path.GetDirectoryName(this.Image).Length > 0)
						texturePath += Path.GetDirectoryName(this.Image) + Path.AltDirectorySeparatorChar;
					texturePath += Path.GetFileNameWithoutExtension(this.Image);
				}

				this.Texture = Resources.Load<Texture2D>(texturePath);
				BuildGameObject();
			}
			else
			{
				if (!texturePath.Contains("://"))
					texturePath = "file://" + texturePath + Path.GetFileName(this.Image);
				// Run Coroutine for WWW using TaskManager.
				new X_UniTMX.Utils.Task(LoadImageTexture(texturePath), true);
			}
		}

		IEnumerator LoadImageTexture(string path)
		{
			WWW www = new WWW(path);
			yield return www;
			Texture = www.texture;
			BuildGameObject();
		}

		void BuildGameObject()
		{
			LayerGameObject.transform.parent = Parent;
			LayerGameObject.transform.localPosition = new Vector3(Position.x, Position.y, this.LayerDepth);

			LayerGameObject.isStatic = true;
			LayerGameObject.SetActive(Visible);

			SpriteRenderer tileRenderer = LayerGameObject.AddComponent<SpriteRenderer>();
			tileRenderer.sprite = Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), Vector2.up, TileWidth);
			tileRenderer.sprite.name = Texture.name;
			tileRenderer.sortingOrder = SortingOrder;
			// Use Layer's name as Sorting Layer
			tileRenderer.sortingLayerName = this.Name;
			//tileRenderer.material = new Material(baseMaterial);
			//tileRenderer.material.mainTexture = Texture;
		}
	}
}
