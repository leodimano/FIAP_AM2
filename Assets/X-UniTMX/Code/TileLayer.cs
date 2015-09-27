/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using UnityEngine;
using System.Collections.Generic;
using TObject.Shared;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using X_UniTMX.Utils;

namespace X_UniTMX
{
	/// <summary>
	/// A Map layer containing Tiles.
	/// </summary>
	public class TileLayer : Layer
	{
		// The data coming in combines flags for whether the tile is flipped as well as
		// the actual index. These flags are used to first figure out if it's flipped and
		// then to remove those flags and get us the actual ID.
		private const uint FlippedHorizontallyFlag = 0x80000000;
		private const uint FlippedVerticallyFlag = 0x40000000;
		private const uint FlippedAntiDiagonallyFlag = 0x20000000;

        /// <summary>
		/// Gets the layout of tiles on the layer.
		/// </summary>
		public TileGrid Tiles { get; private set; }

		/// <summary>
		/// TileLayer's Raw data
		/// </summary>
		public uint[] Data;

		/// <summary>
		/// Base Map this TileLayer is inside
		/// </summary>
		protected Map BaseMap;

		/// <summary>
		/// Base list of Materials generated for BaseMap
		/// </summary>
		protected List<Material> BaseMaterials;

		/// <summary>
		/// Set as true to generate one GameObject per tile, false to generate a single Quad for this layer.
		/// </summary>
		public bool MakeUniqueTiles = true;

		/// <summary>
		/// If not generating unique tiles, the layer needs to generate different meshes and give them to a game object
		/// </summary>
		protected List<GameObject> LayerGameObjects = null;

		/// <summary>
		/// List of TileSet referenced in this TileLayer
		/// </summary>
		List<TileSet> LayerTileSets = null;

		/// <summary>
		/// Creates a Tile Layer from node
		/// </summary>
		/// <param name="node">XML node to parse</param>
		/// <param name="map">TileLayer parent Map</param>
		/// <param name="layerDepth">This Layer's zDepth</param>
		/// <param name="makeUnique">true to generate Unique Tiles</param>
		/// <param name="materials">List of Materials containing the TileSet textures</param>
		public TileLayer(NanoXMLNode node, Map map, int layerDepth, bool makeUnique, List<Material> materials)
            : base(node)
		{
            NanoXMLNode dataNode = node["data"];
            Data = new uint[Width * Height];
			LayerDepth = layerDepth;

			MakeUniqueTiles = makeUnique;
			
            // figure out what encoding is being used, if any, and process
            // the data appropriately
            if (dataNode.GetAttribute("encoding") != null)
            {
                string encoding = dataNode.GetAttribute("encoding").Value;

                if (encoding == "base64")
                {
                    ReadAsBase64(dataNode);
                }
                else if (encoding == "csv")
                {
                    ReadAsCsv(dataNode);
                }
                else
                {
                    throw new Exception("Unknown encoding: " + encoding);
                }
            }
            else
            {
                // XML format simply lays out a lot of <tile gid="X" /> nodes inside of data.

                int i = 0;
				foreach (NanoXMLNode tileNode in dataNode.SubNodes)
				{
					if (tileNode.Name.Equals("tile"))
					{
						Data[i] = uint.Parse(tileNode.GetAttribute("gid").Value, CultureInfo.InvariantCulture);
						i++;
					}
				}

                if (i != Data.Length)
                    throw new Exception("Not enough tile nodes to fill data");
            }

			Initialize(map, Data, materials);
        }

        private void ReadAsCsv(NanoXMLNode dataNode)
        {
            // split the text up into lines
            string[] lines = dataNode.Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // iterate each line
            for (int i = 0; i < lines.Length; i++)
            {
                // split the line into individual pieces
                string[] indices = lines[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // iterate the indices and store in our data
                for (int j = 0; j < indices.Length; j++)
                {
                    Data[i * Width + j] = uint.Parse(indices[j], CultureInfo.InvariantCulture);
                }
            }
        }

        private void ReadAsBase64(NanoXMLNode dataNode)
        {
            // get a stream to the decoded Base64 text
			Stream data = new MemoryStream(Convert.FromBase64String(dataNode.Value), false);

            // figure out what, if any, compression we're using. the compression determines
            // if we need to wrap our data stream in a decompression stream
            if (dataNode.GetAttribute("compression") != null)
            {
                string compression = dataNode.GetAttribute("compression").Value;

                if (compression == "gzip")
                {
					data = new Ionic.Zlib.GZipStream(data, Ionic.Zlib.CompressionMode.Decompress, false);
                }
                else if (compression == "zlib")
                {
                    data = new Ionic.Zlib.ZlibStream(data, Ionic.Zlib.CompressionMode.Decompress, false);
                }
                else
                {
                    throw new InvalidOperationException("Unknown compression: " + compression);
                }
            }

            // simply read in all the integers
            using (data)
            {
                using (BinaryReader reader = new BinaryReader(data))
                {
                    for (int i = 0; i < Data.Length; i++)
                    {
                        Data[i] = reader.ReadUInt32();
                    }
                }
            }
        }

		private void Initialize(Map map, uint[] data, List<Material> materials)
		{
			Tiles = new TileGrid(Width, Height);
			BaseMap = map;
			BaseMaterials = materials;
			LayerTileSets = new List<TileSet>();
			// data is left-to-right, top-to-bottom
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					uint id = data[y * Width + x];

					// compute the SpriteEffects to apply to this tile
					SpriteEffects spriteEffects = new SpriteEffects();

					// MARIO: new method to verify flipped tiles
					spriteEffects.flippedHorizontally = (id & FlippedHorizontallyFlag) == FlippedHorizontallyFlag;
					spriteEffects.flippedVertically = (id & FlippedVerticallyFlag) == FlippedVerticallyFlag;
					spriteEffects.flippedAntiDiagonally = (id & FlippedAntiDiagonallyFlag) == FlippedAntiDiagonallyFlag;
                    
					// MARIO: new strip out the flip flags to get the real ID
					// Fixed for AntiDiagonallyFlgs
                    id &= ~(FlippedHorizontallyFlag |
					        FlippedVerticallyFlag |
					        FlippedAntiDiagonallyFlag);
                    
					// get the tile
					Tile t = null;
					BaseMap.Tiles.TryGetValue((int)id, out t);
					
					// if the tile is non-null...
					if (t != null)
					{
						// if we want unique instances, clone it
						if (MakeUniqueTiles)
						{
							t = t.Clone();
							t.SpriteEffects = spriteEffects;
						}

						// otherwise we may need to clone if the tile doesn't have the correct effects
						// in this world a flipped tile is different than a non-flipped one; just because
						// they have the same source rect doesn't mean they're equal.
						else if (t.SpriteEffects != spriteEffects)
						{
							t = t.Clone();
							t.SpriteEffects = spriteEffects;
						}

						// Add this Tile's TileSet to LayerTileSets list, if it is not there yet
						if (!LayerTileSets.Contains(t.TileSet))
							LayerTileSets.Add(t.TileSet);
					}

					// put that tile in our grid
					Tiles[x, y] = t;
				}
			}
			
			GenerateLayer();
		}
		
		// Renders the tile vertices.
		// Basically, it reads the tiles and creates its 4 vertexes (forming a rectangle or square according to settings) or sprite
		private void GenerateLayer()
		{
			int startX = Width - 1;
			int endX = -1;
			int startY = Height - 1;
			int endY = -1;
			int directionX = -1;
			int directionY = -1;
			//float zOffset = 0.001f;
			// To create the tiles, we must follow the order dictated by map.MapRenderOrder
			// Not unique tiles have inverted bottom-top top-bottom render order :X
			switch (BaseMap.MapRenderOrder)
			{
				case RenderOrder.Right_Down:
					if (MakeUniqueTiles)
					{
						startY = Height - 1;
						endY = -1;
						directionY = -1;
					}
					else
					{
						startX = 0;
						endX = Width;
						directionX = 1;
						startY = 0;
						endY = Height;
						directionY = 1;
					}
					break;
				case RenderOrder.Right_Up:
					if (MakeUniqueTiles)
					{
						startY = 0;
						endY = Height;
						directionY = 1;
					}
					else
					{
						startX = 0;
						endX = Width;
						directionX = 1;
						startY = Height - 1;
						endY = -1;
						directionY = -1;
					}
					break;
				case RenderOrder.Left_Up:
					if (MakeUniqueTiles)
					{
						startX = 0;
						endX = Width;
						directionX = 1;
						startY = 0;
						endY = Height;
						directionY = 1;
					}
					else
					{
						startY = Height - 1;
						endY = -1;
						directionY = -1;
					}
					break;
				case RenderOrder.Left_Down:
					if (MakeUniqueTiles)
					{
						startX = 0;
						endX = Width;
						directionX = 1;
					}
					else
					{
						startY = 0;
						endY = Height;
						directionY = 1;
					}
					break;
			}

			// If we are not going to generate unique tiles, then we need a mesh, a mesh filter and a mesh renderer
			if (!MakeUniqueTiles)
				CreateLayerMesh(startX, endX, startY, endY, directionX, directionY);
			else
				CreateUniqueTiles(startX, endX, startY, endY, directionX, directionY);

			LayerGameObject.transform.parent = BaseMap.MapObject.transform;
			LayerGameObject.transform.localPosition = new Vector3(0, 0, this.LayerDepth);
			LayerGameObject.isStatic = true;

			LayerGameObject.SetActive(Visible);
		}

		void CreateUniqueTiles(int startX, int endX, int startY, int endY, int directionX, int directionY)
		{
			Tile t;
			for (int x = startX;
				startX > endX ? x > endX : x < endX;
				x += directionX)
			{
				for (int y = startY;
					startY > endY ? y > endY : y < endY;
					y += directionY)
				{
					t = Tiles[x, y];
					if (t != null)
					{
						CreateTileGameObject(t, x, y);
					}
				}
			}
		}

		Vector3 GetTileWorldPosition(int tileX, int tileY, TileSet tileSet)
		{
			Vector3 pos = Vector3.zero;
			// Set Tile's position according to map orientation
			// Can't use Map.TiledPositionToWorldPoint as sprites' anchors doesn't follow tile anchor point
			if (BaseMap.Orientation == Orientation.Orthogonal)
			{
				//pos = new Vector3(
				//	tileX * (BaseMap.TileWidth / (float)tileSet.TileWidth),
				//	(-tileY - 1) * (BaseMap.TileHeight / (float)tileSet.TileHeight) * ((float)tileSet.TileHeight / (float)tileSet.TileWidth),
				//	0);
				float ratio = tileSet.TileHeight / (float)tileSet.TileWidth;
				float mapRatio = BaseMap.TileHeight / (float)BaseMap.TileWidth;
				pos = new Vector3(tileX, 1, 0);
				if(ratio != 1)
					pos.y = (-tileY - 1) * (BaseMap.TileHeight / (float)tileSet.TileHeight) * ratio;
				else
					pos.y = (-tileY - 1) * mapRatio;
					
			}
			else if (BaseMap.Orientation == Orientation.Isometric)
			{
				pos = new Vector3(
					(BaseMap.TileWidth / 2.0f * (BaseMap.Width - tileY + tileX) - tileSet.TileWidth / 2.0f) / (float)BaseMap.TileWidth,
					-BaseMap.Height + BaseMap.TileHeight * (BaseMap.Height - ((tileX + tileY) / (BaseMap.TileWidth / (float)BaseMap.TileHeight)) / 2.0f) / (float)BaseMap.TileHeight - (BaseMap.TileHeight / (float)BaseMap.TileWidth),
					0);
			}
			else if (BaseMap.Orientation == Orientation.Staggered)
			{
				// In Staggered maps, odd rows and even rows are handled differently
				if (tileY % 2 < 1)
				{
					// Even row
					pos.x = tileX * (BaseMap.TileWidth / (float)tileSet.TileWidth);
					pos.y = (-tileY - 2) * (BaseMap.TileHeight / 2.0f / (float)tileSet.TileHeight) * ((float)tileSet.TileHeight / (float)tileSet.TileWidth);
				}
				else
				{
					// Odd row
					pos.x = tileX * (BaseMap.TileWidth / (float)tileSet.TileWidth) + (BaseMap.TileWidth / (float)tileSet.TileWidth) / 2.0f;
					pos.y = (-tileY - 2) * (BaseMap.TileHeight / 2.0f / (float)tileSet.TileHeight) * ((float)tileSet.TileHeight / (float)tileSet.TileWidth);
				}
			}

			// Add TileSet Tile Offset
			pos.x += tileSet.TileOffsetX / (float)BaseMap.TileWidth;
			pos.y += tileSet.TileOffsetY / (float)BaseMap.TileWidth;

			return pos;
		}

		void CreateTileGameObject(Tile t, int x, int y)
		{
			// Create Tile's GameObject
			t.CreateTileObject(Name + "[" + x + ", " + y + "]",
				LayerGameObject.transform,
				Name,
				BaseMap.DefaultSortingOrder + BaseMap.GetSortingOrder(x, y),
				GetTileWorldPosition(x, y, t.TileSet),
				BaseMaterials,
				Opacity);

			if (t.TileSet.AnimatedTiles.ContainsKey(t.OriginalID))
			{
				AnimatedSprite _animatedTile = t.TileGameObject.AddComponent<AnimatedSprite>();
				// Tiled defaults to LOOP
				_animatedTile.AnimationMode = SpriteAnimationMode.LOOP;
				foreach (var tileFrame in t.TileSet.AnimatedTiles[t.OriginalID].TileFrames)
				{
					Tile tile;
					if (BaseMap.Tiles.TryGetValue(tileFrame.TileID, out tile))
					{
						_animatedTile.AddSpriteFrame(tile.TileSprite, tileFrame.Duration);
					}
					else
					{
						Debug.LogWarning("Invalid Tile ID while building tile animation: " + tileFrame.TileID);
					}
				}
			}
		}

		void GetVerticesForTile(Tile t, int x, int y, int preVertexCount, out Vector3[] vertices, out int[] triangles)
		{
			vertices = new Vector3[4];
			Vector3 tilePos = GetTileWorldPosition(x, y, t.TileSet);
			float tileHeightInUnits = t.Source.height / (float)BaseMap.TileWidth;
			float tileWidthInUnits = t.Source.width / (float)BaseMap.TileWidth;

			// normal vertices:
			// 1 ----- 3
			// | \	   |
			// |   \   |
			// |     \ |
			// 0 ----- 2
			vertices[0] = tilePos;
			vertices[1] = tilePos + new Vector3(0, tileHeightInUnits);
			vertices[2] = tilePos + new Vector3(tileWidthInUnits, 0);
			vertices[3] = tilePos + new Vector3(tileWidthInUnits, tileHeightInUnits);
			triangles = new int[] {
					preVertexCount    , preVertexCount + 1, preVertexCount + 2,
					preVertexCount + 2, preVertexCount + 1, preVertexCount + 3,
				};

			// Then, rotate / flip if needed
			if (t.SpriteEffects != null)
			{
				if (t.SpriteEffects.flippedAntiDiagonally ||
					t.SpriteEffects.flippedHorizontally ||
					t.SpriteEffects.flippedVertically)
				{
					float ratioHW = t.TileSet.TileHeight / (float)t.TileSet.TileWidth;

					Vector3 flipAnchor = tilePos + new Vector3(0.5f, tileHeightInUnits / 2.0f);
					Vector3 rotateAnchor = tilePos;

					if (t.SpriteEffects.flippedHorizontally == true &&
					   t.SpriteEffects.flippedVertically == false &&
					   t.SpriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].FlipPointHorizontally(flipAnchor);
						}
					}

					if (t.SpriteEffects.flippedHorizontally == false &&
					   t.SpriteEffects.flippedVertically == true &&
					   t.SpriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].FlipPointVertically(flipAnchor);
						}
					}

					if (t.SpriteEffects.flippedHorizontally == true &&
					   t.SpriteEffects.flippedVertically == true &&
					   t.SpriteEffects.flippedAntiDiagonally == false)
					{
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].FlipPointDiagonally(flipAnchor);
						}
					}

					if (t.SpriteEffects.flippedHorizontally == false &&
					   t.SpriteEffects.flippedVertically == false &&
					   t.SpriteEffects.flippedAntiDiagonally == true)
					{
						flipAnchor = tilePos + new Vector3(0, 0.5f);
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].RotatePoint(rotateAnchor, new Vector3(0, 0, 90));
							vertices[i] = vertices[i].FlipPointVertically(flipAnchor) + new Vector3(ratioHW, 0);
						}
					}

					if (t.SpriteEffects.flippedHorizontally == true &&
					   t.SpriteEffects.flippedVertically == false &&
					   t.SpriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].RotatePoint(rotateAnchor, new Vector3(0, 0, -90)) + Vector3.up;
						}
					}

					if (t.SpriteEffects.flippedHorizontally == false &&
					   t.SpriteEffects.flippedVertically == true &&
					   t.SpriteEffects.flippedAntiDiagonally == true)
					{
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].RotatePoint(rotateAnchor, new Vector3(0, 0, 90)) + new Vector3(ratioHW, 0);
						}
					}

					if (t.SpriteEffects.flippedHorizontally == true &&
					   t.SpriteEffects.flippedVertically == true &&
					   t.SpriteEffects.flippedAntiDiagonally == true)
					{
						flipAnchor = tilePos + new Vector3(0, -0.5f);
						for (int i = 0; i < vertices.Length; i++)
						{
							vertices[i] = vertices[i].RotatePoint(rotateAnchor, new Vector3(0, 0, -90));
							vertices[i] = vertices[i].FlipPointVertically(flipAnchor) + Vector3.up;
						}
					}
				}
			}
		}

		void CreateLayerMesh(int startX, int endX, int startY, int endY, int directionX, int directionY)
		{
			// Meshes max number of vertices: 65000 (16250 tiles)
			uint maxVerticesNumber = 65000;

			LayerGameObjects = new List<GameObject>();
			uint count = 0;
			// First we build a list of TileSets referenced in this TileLayer
			// Then we loop through the list of TileSets and:
			//	Create a Mesh
			//	Populate the Mesh with quads, each quad being a tile that uses this tileset
			//	If the mesh's number of vertices will surpass UInt16.MaxValue, we must create another mesh to hold the tiles
			// If the Tile is an AnimatedTile, it will generate an unique tile instead of filling the mesh
			foreach (var tileSet in LayerTileSets)
			{
				GameObject tileSetGameObject = new GameObject(string.Concat(Name, "_", tileSet.Name, "_", count.ToString()));
				Mesh tileSetMesh = new Mesh();
				MeshFilter tileSetMeshFilter = tileSetGameObject.AddComponent<MeshFilter>();
				MeshRenderer tileSetMeshRenderer = tileSetGameObject.AddComponent<MeshRenderer>();

				List<Vector3> vertices = new List<Vector3>();
				List<int> triangles = new List<int>();
				List<Vector2> uv = new List<Vector2>();
				List<Vector3> normals = new List<Vector3>();
				UInt16 vertexCount = 0;

				Tile t;
				for (int x = startX;
					startX > endX ? x > endX : x < endX;
					x += directionX)
				{
					for (int y = startY;
						startY > endY ? y > endY : y < endY;
						y += directionY)
					{
						t = Tiles[x, y];
						if (t != null && t.TileSet.Equals(tileSet))
						{
							// Animated tiles must be created as unique tiles, so they can have monobehaviours with update function
							if (t.TileSet.AnimatedTiles.ContainsKey(t.OriginalID))
							{
								CreateTileGameObject(t, x, y);
								continue;
							}

							Vector3[] verts = new Vector3[4];
							int[] tris = new int[6];

							GetVerticesForTile(t, x, y, vertexCount, out verts, out tris);

							vertices.AddRange( verts );

							triangles.AddRange( tris );

							normals.AddRange(new Vector3[] {
								Vector3.forward,
								Vector3.forward,
								Vector3.forward,
								Vector3.forward
							});

							vertexCount += 4;

							uv.AddRange(new Vector2[] {
								new Vector2((t.Source.xMin + 0.5f) / (float)tileSet.Texture.width, (t.Source.yMin + 0.5f) / (float)tileSet.Texture.height),
								new Vector2((t.Source.xMin + 0.5f) / (float)tileSet.Texture.width, (t.Source.yMax - 0.5f) / (float)tileSet.Texture.height),
								new Vector2((t.Source.xMax - 0.5f) / (float)tileSet.Texture.width, (t.Source.yMin + 0.5f) / (float)tileSet.Texture.height),
								new Vector2((t.Source.xMax - 0.5f) / (float)tileSet.Texture.width, (t.Source.yMax - 0.5f) / (float)tileSet.Texture.height)
							});

							// Check if we reached Unity's mesh maximum number of vertices
							if (vertexCount >= maxVerticesNumber)
							{
								count++;
								tileSetMesh.vertices = vertices.ToArray();
								tileSetMesh.uv = uv.ToArray();
								tileSetMesh.triangles = triangles.ToArray();
								tileSetMesh.normals = normals.ToArray();
								tileSetMesh.Optimize();

								tileSetMeshFilter.mesh = tileSetMesh;

								tileSetGameObject.transform.parent = LayerGameObject.transform;
								tileSetGameObject.isStatic = true;
								LayerGameObjects.Add(tileSetGameObject);

								for (int k = 0; k < BaseMaterials.Count; k++)
								{
									if (BaseMaterials[k].mainTexture.name == tileSet.Texture.name)
										tileSetMeshRenderer.sharedMaterial = BaseMaterials[k];
								}
								// Use Layer's name as Sorting Layer
								tileSetMeshRenderer.sortingLayerName = Name;
								tileSetMeshRenderer.sortingOrder = BaseMap.DefaultSortingOrder;

								vertexCount = 0;
								tileSetGameObject = new GameObject(string.Concat(Name, "_", tileSet.Name, "_", count.ToString()));
								tileSetMesh = new Mesh();
								tileSetMeshFilter = tileSetGameObject.AddComponent<MeshFilter>();
								tileSetMeshRenderer = tileSetGameObject.AddComponent<MeshRenderer>();

								vertices.Clear();
								triangles.Clear();
								uv.Clear();
								normals.Clear();
							}
						}
					}
				}
				tileSetMesh.vertices = vertices.ToArray();
				tileSetMesh.uv = uv.ToArray();
				tileSetMesh.triangles = triangles.ToArray();
				tileSetMesh.normals = normals.ToArray();
				tileSetMesh.Optimize();

				tileSetMeshFilter.mesh = tileSetMesh;

				for (int k = 0; k < BaseMaterials.Count; k++)
				{
					if (BaseMaterials[k].mainTexture.name == tileSet.Texture.name)
						tileSetMeshRenderer.sharedMaterial = BaseMaterials[k];
				}
				// Use Layer's name as Sorting Layer
				tileSetMeshRenderer.sortingLayerName = Name;
				tileSetMeshRenderer.sortingOrder = BaseMap.DefaultSortingOrder;

				tileSetGameObject.isStatic = true;
				tileSetGameObject.transform.parent = LayerGameObject.transform;
				LayerGameObjects.Add(tileSetGameObject);
			}
		}

		#region Set Tile functions
		/// <summary>
		/// Sets a Tile in position x and y to be Tile with ID equals newTileID (a Global Tile ID)
		/// This does not works if the layer is not made of Unique Tiles.
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <param name="newTileID">Global Tile ID to change existing tile to. If -1 is passed, erase current Tile</param>
		/// <returns>true if newTileID was found and change succeded, false otherwise</returns>
		public bool SetTile(int x, int y, int newTileID)
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height || !MakeUniqueTiles)
				return false;

			if (newTileID < 0)
			{
				if (Tiles[x, y] != null)
					GameObject.Destroy(Tiles[x, y].TileGameObject);
				Tiles[x, y] = null;
				return true;
			}
			Tile t = null;
			if (BaseMap.Tiles.TryGetValue(newTileID, out t))
			{
				if (Tiles[x, y] != null)
				{
					Tiles[x, y].TileSprite = t.TileSprite;
					Tiles[x, y].CurrentID = t.OriginalID;
					(Tiles[x, y].TileGameObject.GetComponent<Renderer>() as SpriteRenderer).sprite = t.TileSprite;
				}
				else
				{
					Tile newTile = t.Clone();
					CreateTileGameObject(newTile, x, y);
					Tiles[x, y] = newTile;
				}
				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Sets a Tile in position x and y to be Tile with ID equals newTileID (a Global Tile ID)
		/// This does not works if the layer is not made of Unique Tiles.
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <param name="newTileID">Global Tile ID to change existing tile to. If -1 is passed, erase current Tile</param>
		/// <returns>true if newTileID was found and change succeded, false otherwise</returns>
		public bool SetTile(float x, float y, int newTileID)
		{
			return SetTile(Mathf.FloorToInt(x), Mathf.FloorToInt(y), newTileID);
		}

		/// <summary>
		/// Sets a Tile in position x and y to be Tile with ID equals newTileID (a Local Tile ID from tileSet)
		/// This does not works if the layer is not made of Unique Tiles.
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <param name="newTileID">Local Tile ID to change existing tile to. If -1 is passed, erase current Tile</param>
		/// <param name="tileSet">TileSet to read newTileID from</param>
		/// <returns>true if newTileID inside tileSet was found and change succeded, false otherwise</returns>
		public bool SetTile(int x, int y, int newTileID, TileSet tileSet)
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height || !MakeUniqueTiles)
				return false;
			if (newTileID < 0)
			{
				if (Tiles[x, y] != null)
					GameObject.Destroy(Tiles[x, y].TileGameObject);
				Tiles[x, y] = null;
				return true;
			}
			Tile t = null;
			if (tileSet.Tiles.TryGetValue(newTileID, out t))
			{
				if (Tiles[x, y] != null)
				{
					Tiles[x, y].TileSprite = t.TileSprite;
					Tiles[x, y].CurrentID = t.OriginalID;
					(Tiles[x, y].TileGameObject.GetComponent<Renderer>() as SpriteRenderer).sprite = t.TileSprite;
				}
				else
				{
					Tile newTile = t.Clone();
					CreateTileGameObject(newTile, x, y);
					Tiles[x, y] = newTile;
				}
				return true;
			}

			return false;
		}

		/// <summary>
		/// Sets a Tile in position x and y to be Tile with ID equals newTileID (a Local Tile ID from tileSet)
		/// This does not works if the layer is not made of Unique Tiles.
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <param name="newTileID">Local Tile ID to change existing tile to. If -1 is passed, erase current Tile</param>
		/// <param name="tileSet">TileSet to read newTileID from</param>
		/// <returns>true if newTileID inside tileSet was found and change succeded, false otherwise</returns>
		public bool SetTile(float x, float y, int newTileID, TileSet tileSet)
		{
			return SetTile(Mathf.FloorToInt(x), Mathf.FloorToInt(y), newTileID, tileSet);
		}
		#endregion
	}
}