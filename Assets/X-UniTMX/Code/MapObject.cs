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

namespace X_UniTMX
{
	/// <summary>
	/// An arbitrary Object placed on an ObjectLayer.
	/// </summary>
	public class MapObject : Object
	{
		/// <summary>
		/// Gets the name of the object.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the type of the object.
		/// </summary>
		public string Type { get; private set; }

		/// <summary>
		/// Gets a list of the object's properties.
		/// </summary>
		public PropertyCollection Properties { get; private set; }

		/// <summary>
		/// Gets the object OriginalID
		/// </summary>
		public int GID { get; private set; }

		/// <summary>
		/// Gets or sets the whether the object is visible.
		/// </summary>
		public bool Visible { get; set; }

		/// <summary>
		/// The Object Layer this MapObject belongs to
		/// </summary>
		public MapObjectLayer ParentObjectLayer { get; set; }

		/// <summary>
		/// Creates a new MapObject.
		/// </summary>
		/// <param name="name">The name of the object.</param>
		/// <param name="type">The type of object to create.</param>
		public MapObject(string name, string type) : this(name, type, new Rect(), new PropertyCollection(), 0, new List<Vector2>(), 0, null) { }

		/// <summary>
		/// Creates a new MapObject.
		/// </summary>
		/// <param name="name">The name of the object.</param>
		/// <param name="type">The type of object to create.</param>
		/// <param name="bounds">The initial bounds of the object.</param>
		public MapObject(string name, string type, Rect bounds) : this(name, type, bounds, new PropertyCollection(), 0, new List<Vector2>(), 0, null) { }

		/// <summary>
		/// Creates a new MapObject.
		/// </summary>
		/// <param name="name">The name of the object.</param>
		/// <param name="type">The type of object to create.</param>
		/// <param name="bounds">The initial bounds of the object.</param>
		/// <param name="properties">The initial property collection or null to create an empty property collection.</param>
		/// <param name="rotation">This object's rotation</param>
		/// <param name="gid">Object's ID</param>
		/// <param name="parentObjectLayer">This MapObject's MapObjectLayer parent</param>
		/// <param name="points">This MapObject's Point list</param>
		public MapObject(string name, string type, Rect bounds, PropertyCollection properties, int gid, List<Vector2> points, float rotation, MapObjectLayer parentObjectLayer)
			: base(ObjectType.Box, bounds, rotation, points)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException(null, "name");

			Name = name;
			Type = type;
			Bounds = bounds;
			Properties = properties ?? new PropertyCollection();
			GID = gid;
			Points = points;
			Visible = true;
			Rotation = rotation;
			ParentObjectLayer = parentObjectLayer;
		}

		/// <summary>
		/// Creates a MapObject from node
		/// </summary>
		/// <param name="node">NanoXMLNode XML to parse</param>
		/// <param name="parentObjectLayer">This MapObject's MapObjectLayer parent</param>
		public MapObject(NanoXMLNode node, MapObjectLayer parentObjectLayer)
			: base(node)
        {
			if (node.GetAttribute("name") != null)
			{
				Name = node.GetAttribute("name").Value;
			}
			else
			{
				Name = "Object";
			}

			if (node.GetAttribute("type") != null)
			{
				Type = node.GetAttribute("type").Value;
			}
			else
				Type = string.Empty;

			if (node.GetAttribute("visible") != null)
			{
				Visible = int.Parse(node.GetAttribute("visible").Value, CultureInfo.InvariantCulture) == 1;
			}
			else
				Visible = true;

			if (node.GetAttribute("gid") != null)
				GID = int.Parse(node.GetAttribute("gid").Value, CultureInfo.InvariantCulture);
			else
				GID = 0;

			ParentObjectLayer = parentObjectLayer;

            NanoXMLNode propertiesNode = node["properties"];
            if (propertiesNode != null)
            {
                Properties = new PropertyCollection(propertiesNode);
            }
        }

		/// <summary>
		/// Creates this Tile Object (an object that has OriginalID) if applicable
		/// </summary>
		/// <param name="tiledMap">The base Tile Map</param>
		/// <param name="layerDepth">Layer's zDepth</param>
		/// <param name="sortingLayerName">Layer's SortingLayerName</param>
		/// <param name="parent">Transform to parent this object to</param>
		/// <param name="materials">List of TileSet Materials</param>
		public void CreateTileObject(Map tiledMap, string sortingLayerName, int layerDepth, List<Material> materials, Transform parent = null)
		{
			if(GID > 0) {
				Tile objTile = null;
				if(tiledMap.Orientation != Orientation.Orthogonal)
					objTile = tiledMap.Tiles[GID].Clone(new Vector2(0.5f, 0.5f));
				else
					objTile = tiledMap.Tiles[GID].Clone();

				//string sortingLayer = GetPropertyAsString("SortingLayer");
				//int sortingOrder = GetPropertyAsInt("SortingOrder");

				objTile.CreateTileObject(Name,
					parent != null ? parent : ParentObjectLayer.LayerGameObject.transform,
					sortingLayerName,
					tiledMap.DefaultSortingOrder + tiledMap.GetSortingOrder(Bounds.x, Bounds.y),
					tiledMap.TiledPositionToWorldPoint(Bounds.x, Bounds.y, layerDepth),
					materials);
				this.Bounds = new Rect(Bounds.x, Bounds.y - 1, 1, 1);
				/*Debug.Log(Bounds);
				tiledMap.AddBoxCollider(objTile.TileGameObject, this);*/
				objTile.TileGameObject.SetActive(Visible);
			}
		}

		/// <summary>
		/// Gets a string property
		/// </summary>
		/// <param name="property">Name of the property inside Tiled</param>
		/// <returns>The value of the property, String.Empty if property not found</returns>
		public string GetPropertyAsString(string property)
		{
			if (Properties == null)
				return string.Empty;
			return Properties.GetPropertyAsString(property);
		}
		/// <summary>
		/// Gets a boolean property
		/// </summary>
		/// <param name="property">Name of the property inside Tiled</param>
		/// <returns>The value of the property</returns>
		public bool GetPropertyAsBoolean(string property)
		{
			if (Properties == null)
				return false;
			return Properties.GetPropertyAsBoolean(property);
		}
		/// <summary>
		/// Gets an integer property
		/// </summary>
		/// <param name="property">Name of the property inside Tiled</param>
		/// <returns>The value of the property</returns>
		public int GetPropertyAsInt(string property)
		{
			if (Properties == null)
				return 0;
			return Properties.GetPropertyAsInt(property);
		}
		/// <summary>
		/// Gets a float property
		/// </summary>
		/// <param name="property">Name of the property inside Tiled</param>
		/// <returns>The value of the property</returns>
		public float GetPropertyAsFloat(string property)
		{
			if (Properties == null)
				return 0;
			return Properties.GetPropertyAsFloat(property);
		}

		/// <summary>
		/// Checks if a property exists
		/// </summary>
		/// <param name="property">Name of the property inside Tiled</param>
		/// <returns>true if property exists, false otherwise</returns>
		public bool HasProperty(string property)
		{
			if (Properties == null)
				return false;
			Property p;
			if (Properties.TryGetValue(property.ToLowerInvariant(), out p))
				return true;
			return false;
		}
	}
}
