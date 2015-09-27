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
	/// An abstract base for a Layer in a Map.
	/// </summary>
	public abstract class Layer
	{
		/// <summary>
		/// Gets the name of the layer.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the width (in tiles) of the layer.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height (in tiles) of the layer.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets or sets the depth of the layer.
		/// </summary>
		public int LayerDepth { get; set; }

		/// <summary>
		/// Gets or sets the whether the layer is visible.
		/// </summary>
		public bool Visible { get; set; }

		/// <summary>
		/// Gets or sets the opacity of the layer.
		/// </summary>
		public float Opacity { get; set; }

		/// <summary>
		/// Gets the list of properties for the layer.
		/// </summary>
		public PropertyCollection Properties { get; private set; }

		/// <summary>
		/// Layer's Game Object
		/// </summary>
		public GameObject LayerGameObject { get; private set; }

		internal Layer(string name, int width, int height, int layerDepth, bool visible, float opacity, PropertyCollection properties)
		{
			this.Name = name;
			this.Width = width;
			this.Height = height;
			this.LayerDepth = layerDepth;
			this.Visible = visible;
			this.Opacity = opacity;
			this.Properties = properties;
			LayerGameObject = new GameObject(Name);
		}

		protected Layer(NanoXMLNode node)
        {
            //string Type = node.Name;
            Name = node.GetAttribute("name").Value;
			if (string.IsNullOrEmpty(Name))
				Name = "Layer";

			if (node.GetAttribute("width") != null)
				Width = int.Parse(node.GetAttribute("width").Value, CultureInfo.InvariantCulture);
			else
				Width = 0;
			if (node.GetAttribute("height") != null)
				Height = int.Parse(node.GetAttribute("height").Value, CultureInfo.InvariantCulture);
			else
				Height = 0;

			if (node.GetAttribute("opacity") != null)
			{
				Opacity = float.Parse(node.GetAttribute("opacity").Value, CultureInfo.InvariantCulture);
			}
			else
				Opacity = 1;

			if (node.GetAttribute("visible") != null)
			{
				Visible = int.Parse(node.GetAttribute("visible").Value, CultureInfo.InvariantCulture) == 1;
			}
			else
				Visible = true;

            NanoXMLNode propertiesNode = node["properties"];
            if (propertiesNode != null)
            {
                Properties = new PropertyCollection(propertiesNode);
            }

			LayerGameObject = new GameObject(Name);
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
