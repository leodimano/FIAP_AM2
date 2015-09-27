/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace X_UniTMX
{
	/// <summary>
	/// Tile SpriteEffects apllied in a TileLayer
	/// </summary>
	public class SpriteEffects
	{
		/// <summary>
		/// Flag for Tile Flipped Horizontally
		/// </summary>
		public bool flippedHorizontally = false;
		/// <summary>
		/// Flag for Tile Flipped Vertically
		/// </summary>
		public bool flippedVertically = false;
		/// <summary>
		/// Flag for Tile Flipped AntiDiagonally (Diagonally reversed)
		/// </summary>
		public bool flippedAntiDiagonally = false;
	}

	/// <summary>
	/// A single Tile in a TileLayer.
	/// </summary>
	public class Tile
	{
		/// <summary>
		/// Gets this Tile's original ID (the first set in Tiled)
		/// </summary>
		public int OriginalID { get; private set; }

		/// <summary>
		/// Gets this Tile's current ID (this can be changed ingame when TileLayer.SetTile is called)
		/// </summary>
		public int CurrentID { get; set; }

		/// <summary>
        /// Gets the Texture2D to use when drawing the tile.
        /// </summary>
        public TileSet TileSet { get; set; }

        /// <summary>
        /// Gets the source rectangle of the tile.
        /// </summary>
        public Rect Source { get; private set; }

        /// <summary>
        /// Gets the collection of properties for the tile.
        /// </summary>
        public PropertyCollection Properties { get; private set; }

        /// <summary>
        /// Gets or sets a color associated with the tile.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the SpriteEffects applied when drawing this tile.
        /// </summary>
		public SpriteEffects SpriteEffects { get; set; }

		/// <summary>
		/// Gets or sets this Tile Unity's GameObject
		/// </summary>
        public GameObject TileGameObject { get; set; }

		/// <summary>
		/// Gets or sets this Tile's Sprite
		/// </summary>
		public Sprite TileSprite { get; set; }

		/// <summary>
		/// Gets the Map's Tile Width, used to calculate Texture's pixelsToUnits
		/// </summary>
		public int MapTileWidth { get; protected set; }

        /// <summary>
        /// Creates a new Tile object.
        /// </summary>
		/// <param name="tileSet">The TileSet that contains the tile image.</param>
        /// <param name="source">The source rectangle of the tile.</param>
		/// <param name="OriginalID">This Tile's ID</param>
		public Tile(TileSet tileSet, Rect source, int OriginalID) : this(tileSet, source, OriginalID, new PropertyCollection(), Vector2.zero) { }

        /// <summary>
        /// Creates a new Tile object.
        /// </summary>
		/// <param name="tileSet">The TileSet that contains the tile image.</param>
        /// <param name="source">The source rectangle of the tile.</param>
		/// <param name="OriginalID">This Tile's ID</param>
        /// <param name="properties">The initial property collection or null to create an empty property collection.</param>
		/// <param name="pivot">The Tile's Sprite Pivot Point</param>
		/// <param name="mapTileWidth">The Map's TileWidth this tile is inside, used to calculate sprite's pixelsToUnits</param>
		public Tile(TileSet tileSet, Rect source, int OriginalID, PropertyCollection properties, Vector2 pivot, int mapTileWidth = 0)
        {
            if (tileSet == null)
                throw new ArgumentNullException("tileSet");

			this.OriginalID = OriginalID;
			CurrentID = OriginalID;
            TileSet = tileSet;
            Source = source;
            Properties = properties ?? new PropertyCollection();
            Color = Color.white;
			SpriteEffects = new X_UniTMX.SpriteEffects();

			MapTileWidth = mapTileWidth;
			if (mapTileWidth <= 0)
				MapTileWidth = TileSet.TileWidth;
			CreateSprite(pivot);
        }

		/// <summary>
		/// Creates a new Tile without creating the Sprite
		/// </summary>
		/// <param name="tileSet">The TileSet that contains the tile image.</param>
		/// <param name="source">The source rectangle of the tile.</param>
		/// <param name="OriginalID">This Tile's ID</param>
		/// <param name="properties">The initial property collection or null to create an empty property collection.</param>
		internal Tile(TileSet tileSet, Rect source, int OriginalID, PropertyCollection properties, int mapTileWidth = 0)
		{
			this.OriginalID = OriginalID;
			CurrentID = OriginalID;
			TileSet = tileSet;
			Source = source;
			Properties = properties ?? new PropertyCollection();
			Color = Color.white;
			SpriteEffects = new X_UniTMX.SpriteEffects();
			if (mapTileWidth <= 0)
				MapTileWidth = TileSet.TileWidth;
		}

		/// <summary>
		/// Creates this Tile's Sprite
		/// </summary>
		/// <param name="pivot">Sprite Pivot Point</param>
		protected void CreateSprite(Vector2 pivot)
		{
			// Create Sprite
			TileSprite = Sprite.Create(TileSet.Texture, Source, pivot, MapTileWidth, (uint)(TileSet.Spacing * 2));
			TileSprite.name = OriginalID.ToString();
		}

		/// <summary>
		/// Creates this Tile's GameObject (TileGameObject)
		/// </summary>
		/// <param name="objectName">Desired name</param>
		/// <param name="parent">GameObject's parent</param>
		/// <param name="sortingLayerName">Sprite's sorting layer name</param>
		/// <param name="sortingLayerOrder">Sprite's sorting layer order</param>
		/// <param name="position">GameObject's position</param>
		/// <param name="materials">List of shared materials</param>
		/// <param name="opacity">This Object's Opacity</param>
		public void CreateTileObject(string objectName, Transform parent, string sortingLayerName, int sortingLayerOrder, Vector3 position, List<Material> materials, float opacity = 1.0f)
		{
			TileGameObject = new GameObject(objectName);
			TileGameObject.transform.parent = parent;
			
			SpriteRenderer tileRenderer = TileGameObject.AddComponent<SpriteRenderer>();
			
			tileRenderer.sprite = TileSprite;

			// Use Layer's name as Sorting Layer
			tileRenderer.sortingLayerName = sortingLayerName;
			tileRenderer.sortingOrder = sortingLayerOrder;

			TileGameObject.transform.localScale = new Vector2(1, 1);
			TileGameObject.transform.localPosition = new Vector3(position.x, position.y, position.z);

			if (this.SpriteEffects != null)
			{
				if (this.SpriteEffects.flippedHorizontally ||
					this.SpriteEffects.flippedVertically ||
					this.SpriteEffects.flippedAntiDiagonally)
				{
					// MARIO: Fixed flippedHorizontally, flippedVertically and flippedAntiDiagonally effects
					float ratioHW = TileSet.TileHeight / (float)MapTileWidth;

					//if (this.SpriteEffects.flippedHorizontally == false &&
					//   this.SpriteEffects.flippedVertically == false &&
					//   this.SpriteEffects.flippedAntiDiagonally == false)
					//{
					//	TileGameObject.transform.localScale = new Vector2(1, 1);
					//	TileGameObject.transform.localPosition = new Vector3(position.x, position.y, position.z);
					//}

					if (this.SpriteEffects.flippedHorizontally == true &&
					   this.SpriteEffects.flippedVertically == false &&
					   this.SpriteEffects.flippedAntiDiagonally == false)
					{
						TileGameObject.transform.localScale = new Vector2(-1, 1);
						TileGameObject.transform.localPosition = new Vector3(position.x + 1, position.y, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == false &&
					   this.SpriteEffects.flippedVertically == true &&
					   this.SpriteEffects.flippedAntiDiagonally == false)
					{
						TileGameObject.transform.localScale = new Vector2(1, -1);
						TileGameObject.transform.localPosition = new Vector3(position.x, position.y + ratioHW, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == true &&
					   this.SpriteEffects.flippedVertically == true &&
					   this.SpriteEffects.flippedAntiDiagonally == false)
					{
						TileGameObject.transform.localScale = new Vector2(-1, -1);
						TileGameObject.transform.localPosition = new Vector3(position.x + 1, position.y + ratioHW, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == false &&
					   this.SpriteEffects.flippedVertically == false &&
					   this.SpriteEffects.flippedAntiDiagonally == true)
					{
						TileGameObject.transform.Rotate(Vector3.forward, 90);
						TileGameObject.transform.localScale = new Vector2(-1, 1);
						TileGameObject.transform.localPosition = new Vector3(position.x + ratioHW, position.y + 1, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == true &&
					   this.SpriteEffects.flippedVertically == false &&
					   this.SpriteEffects.flippedAntiDiagonally == true)
					{
						TileGameObject.transform.Rotate(Vector3.forward, -90);
						TileGameObject.transform.localPosition = new Vector3(position.x, position.y + 1, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == false &&
					   this.SpriteEffects.flippedVertically == true &&
					   this.SpriteEffects.flippedAntiDiagonally == true)
					{
						TileGameObject.transform.Rotate(Vector3.forward, 90);
						TileGameObject.transform.localPosition = new Vector3(position.x + ratioHW, position.y, position.z);
					}

					if (this.SpriteEffects.flippedHorizontally == true &&
					   this.SpriteEffects.flippedVertically == true &&
					   this.SpriteEffects.flippedAntiDiagonally == true)
					{
						TileGameObject.transform.Rotate(Vector3.forward, 90);
						TileGameObject.transform.localScale = new Vector2(1, -1);
						TileGameObject.transform.localPosition = new Vector3(position.x, position.y, position.z);
					}
				}
			}

			for (int k = 0; k < materials.Count; k++)
            {
				if (materials[k].mainTexture.name == TileSet.Texture.name)
				{
					tileRenderer.sharedMaterial = materials[k];
					break;
				}
			}

			if (opacity < 1)
				tileRenderer.sharedMaterial.color = new Color(1, 1, 1, opacity);
		}

        /// <summary>
        /// Creates a copy of the current tile.
        /// </summary>
        /// <returns>A new Tile with the same properties as the current tile.</returns>
        public virtual Tile Clone()
        {
            Tile t = new Tile(TileSet, Source, OriginalID, Properties);
			t.TileSprite = TileSprite;
			t.SpriteEffects = SpriteEffects;
			t.MapTileWidth = MapTileWidth;
			return t;
        }

		/// <summary>
		/// Creates a copy of the current tile with a different pivot point.
		/// </summary>
		/// <param name="pivot">New pivot point</param>
		/// <returns>A new Tile with the same properties as the current tile.</returns>
		public virtual Tile Clone(Vector2 pivot)
		{
			return new Tile(TileSet, Source, OriginalID, Properties, pivot);
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
