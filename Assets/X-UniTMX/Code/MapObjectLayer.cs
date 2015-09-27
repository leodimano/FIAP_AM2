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
using UnityEngine;

namespace X_UniTMX
{
	/// <summary>
	/// A layer comprised of objects.
	/// </summary>
	public class MapObjectLayer : Layer
	{
		private Dictionary<string, MapObject> namedObjects = new Dictionary<string, MapObject>();

		/// <summary>
		/// Gets or sets this layer's color.
		/// </summary>
		public Color Color { get; set; }

		/// <summary>
		/// Gets the objects on the current layer.
		/// </summary>
		public List<MapObject> Objects { get; private set; }

		/// <summary>
		/// Creates a Map Object Layer from node
		/// </summary>
		/// <param name="node">XML node to parse</param>
		/// <param name="tiledMap">MapObjectLayer parent Map</param>
		/// <param name="layerDepth">This Layer's zDepth</param>
		/// <param name="materials">List of Materials containing the TileSet textures</param>
		public MapObjectLayer(NanoXMLNode node, Map tiledMap, int layerDepth, List<Material> materials)
            : base(node)
        {
            if (node.GetAttribute("color") != null)
            {
                // get the color string, removing the leading #
                string color = node.GetAttribute("color").Value.Substring(1);

                // get the RGB individually
                string r = color.Substring(0, 2);
                string g = color.Substring(2, 2);
                string b = color.Substring(4, 2);

                // convert to the color
                Color = new Color(
                    (byte)int.Parse(r, NumberStyles.AllowHexSpecifier),
                    (byte)int.Parse(g, NumberStyles.AllowHexSpecifier),
                    (byte)int.Parse(b, NumberStyles.AllowHexSpecifier));
            }

			LayerGameObject.transform.parent = tiledMap.MapObject.transform;
			LayerGameObject.transform.localPosition = new Vector3(0, 0, this.LayerDepth);
			LayerGameObject.isStatic = true;
			LayerGameObject.SetActive(Visible);

			Objects = new List<MapObject>();

			foreach (NanoXMLNode objectNode in node.SubNodes)
			{
				if (!objectNode.Name.Equals("object"))
					continue;

				MapObject mapObjectContent = new MapObject(objectNode, this);

				mapObjectContent.ScaleObject(tiledMap.TileWidth, tiledMap.TileHeight, tiledMap.Orientation);
				mapObjectContent.Name = this.Name + "_" + mapObjectContent.Name;
				// Object names need to be unique for our lookup system, but Tiled
				// doesn't require unique names.
				string objectName = mapObjectContent.Name;
				int duplicateCount = 2;

				// if a object already has the same name...
				if (Objects.Find(o => o.Name.Equals(objectName)) != null)
				{
					// figure out a object name that does work
					do
					{
						objectName = string.Format("{0}{1}", mapObjectContent.Name, duplicateCount);
						duplicateCount++;
					} while (Objects.Find(o => o.Name.Equals(objectName)) != null);

					// log a warning for the user to see
					//Debug.LogWarning("Renaming object \"" + mapObjectContent.Name + "\" to \"" + objectName + "\" in layer \"" + Name + "\" to make a unique name.");

					// save that name
					mapObjectContent.Name = objectName;
				}
				mapObjectContent.CreateTileObject(tiledMap, Name, layerDepth, materials);

				AddObject(mapObjectContent);
			}
        }

		internal MapObjectLayer(string name, int width, int height, int layerDepth, bool visible, float opacity, PropertyCollection properties, List<MapObject> initialObjects)
			: base(name, width, height, layerDepth, visible, opacity, properties)
		{
			Objects = new List<MapObject>();
			initialObjects.ForEach(AddObject);
		}

		/// <summary>
		/// Adds a MapObject to the layer.
		/// </summary>
		/// <param name="mapObject">The MapObject to add.</param>
		public void AddObject(MapObject mapObject)
		{
			// avoid adding the object to the layer twice
			if (Objects.Contains(mapObject))
				return;

			namedObjects.Add(mapObject.Name, mapObject);
			Objects.Add(mapObject);
		}

		/// <summary>
		/// Gets a MapObject by name.
		/// </summary>
		/// <param name="objectName">The name of the object to retrieve.</param>
		/// <returns>The MapObject with the given name.</returns>
		public MapObject GetObject(string objectName)
		{
			return namedObjects[objectName];
		}

		/// <summary>
		/// Removes an object from the layer.
		/// </summary>
		/// <param name="mapObject">The object to remove.</param>
		/// <returns>True if the object was found and removed, false otherwise.</returns>
		public bool RemoveObject(MapObject mapObject)
		{
			return RemoveObject(mapObject.Name);
		}

		/// <summary>
		/// Removes an object from the layer.
		/// </summary>
		/// <param name="objectName">The name of the object to remove.</param>
		/// <returns>True if the object was found and removed, false otherwise.</returns>
		public bool RemoveObject(string objectName)
		{
			MapObject obj;
			if (namedObjects.TryGetValue(objectName, out obj))
			{
				Objects.Remove(obj);
				namedObjects.Remove(objectName);
				return true;
			}
			return false;
		}
	}
}
