/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TObject.Shared;
using UnityEngine;
using X_UniTMX.Utils;

namespace X_UniTMX
{
	/// <summary>
	/// Defines the possible orientations for a Map.
	/// </summary>
	public enum Orientation : byte
	{
		/// <summary>
		/// The tiles of the map are orthogonal.
		/// </summary>
		Orthogonal,

		/// <summary>
		/// The tiles of the map are isometric.
		/// </summary>
		Isometric,

		/// <summary>
		/// The tiles of the map are isometric (staggered).
		/// </summary>
		Staggered
	}

	/// <summary>
	/// Defines the possible Rendering orders for the tiles in a Map
	/// </summary>
	public enum RenderOrder : byte
	{
		/// <summary>
		/// Tiles are rendered bottom-top, right-left
		/// </summary>
		Right_Down,
		/// <summary>
		/// Tiles are rendered top-bottom, right-left
		/// </summary>
		Right_Up,
		/// <summary>
		/// Tiles are rendered bottom-top, left-right
		/// </summary>
		Left_Down,
		/// <summary>
		/// Tiles are rendered top-bottom, left-right
		/// </summary>
		Left_Up
	}

	/// <summary>
	/// A delegate used for searching for map objects.
	/// </summary>
	/// <param name="layer">The current layer.</param>
	/// <param name="mapObj">The current object.</param>
	/// <returns>True if this is the map object desired, false otherwise.</returns>
	public delegate bool MapObjectFinder(MapObjectLayer layer, MapObject mapObj);

	/// <summary>
	/// A full map from Tiled.
	/// </summary>
	public class Map
	{
		/// <summary>
		/// The difference in layer depth between layers.
		/// </summary>
		/// <remarks>
		/// The algorithm for creating the LayerDepth for each layer when enumerating from
		/// back to front is:
		/// float layerDepth = 1f - (LayerDepthSpacing * x);</remarks>
		public const int LayerDepthSpacing = 1;

		/// <summary>
		/// How accurate will be the colliders that are approximated to an ellipse format.
		/// </summary>
		/// <remarks>
		/// Increasing this value will generate higher-quality ellipsoides, at the cost of more vertices.
		/// This number is the number of generated vertices the ellipsoide will have.
		/// </remarks>
		public const int EllipsoideColliderApproximationFactor = 16;

		private readonly Dictionary<string, Layer> namedLayers = new Dictionary<string, Layer>();

		/// <summary>
		/// Gets the version of Tiled used to create the Map.
		/// </summary>
		public string Version { get; private set; }

		/// <summary>
		/// Gets the orientation of the map.
		/// </summary>
		public Orientation Orientation { get; private set; }

		/// <summary>
		/// This Map's Background Color.
		/// </summary>
		public Color BackgroundColor { get; private set; }

		/// <summary>
		/// Gets the width (in tiles) of the map.
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// Gets the height (in tiles) of the map.
		/// </summary>
		public int Height { get; private set; }

		/// <summary>
		/// Gets the width of a tile in the map.
		/// </summary>
		public int TileWidth { get; private set; }

		/// <summary>
		/// Gets the height of a tile in the map.
		/// </summary>
		public int TileHeight { get; private set; }

		/// <summary>
		/// Gets a list of the map's properties.
		/// </summary>
		public PropertyCollection Properties { get; private set; }

		/// <summary>
		/// Gets this Map's RenderOrder.
		/// </summary>
		public RenderOrder MapRenderOrder { get; private set; }

		/// <summary>
		/// Gets a collection of all of the tiles in the map.
		/// </summary>
		public Dictionary<int, Tile> Tiles { get; private set; }

		/// <summary>
		/// Gets a collection of all of the layers in the map.
		/// </summary>
		public List<Layer> Layers { get; private set; }

		/// <summary>
		/// Gets a collection of all of the tile sets in the map.
		/// </summary>
		public List<TileSet> TileSets { get; private set; }

		/// <summary>
		/// Gets this map's Game Object Parent
		/// </summary>
		public GameObject Parent { get; private set; }

		/// <summary>
		/// Gets this map's Game Object
		/// </summary>
		public GameObject MapObject { get; private set; }

		/// <summary>
		/// Map's Tile Layers' initial Sorting Order
		/// </summary>
		public int DefaultSortingOrder = 0;

		/// <summary>
		/// True if is loading a map from Streaming Path or HTTP url
		/// </summary>
		public bool UsingStreamingAssetsPath = false;

		/// <summary>
		/// This Map's NanoXMLNode node
		/// </summary>
		public NanoXMLNode MapNode;
		/// <summary>
		/// This Map's base Tile Material;
		/// </summary>
		public Material BaseTileMaterial;
		/// <summary>
		/// true to generate Unique Tile, array per layer
		/// </summary>
		public bool[] PerLayerMakeUniqueTiles;
		/// <summary>
		/// true to generate Unique Tiles on all tile layers
		/// </summary>
		public bool GlobalMakeUniqueTiles = false;

		private string _mapName = "Map";
		private string _mapPath = "Map";
		private string _mapExtension = ".tmx";
		private int _numberOfTileSetsToLoad = 0;
		private int _tileSetsToLoaded = 0;
		private int _tileObjectEllipsePrecision = 16;
		private bool _simpleTileObjectCalculation = false;
		private double _clipperArcTolerance = 0.25;
		private double _clipperMiterLimit = 2.0;
		private ClipperLib.JoinType _clipperJoinType = ClipperLib.JoinType.jtRound;
		private ClipperLib.EndType _clipperEndType = ClipperLib.EndType.etClosedPolygon;
		private float _clipperDeltaOffset = 0;

		/// <summary>
		/// Delegate that is called when this map finishes loading
		/// </summary>
		public Action<Map> OnMapFinishedLoading = null;

		#region Custom Properties
		/// <summary>
		/// Custom Object Type for Collider Objects
		/// </summary>
		public const string Object_Type_Collider = "Collider";
		/// <summary>
		/// Custom Object Type for Trigger Objects
		/// </summary>
		public const string Object_Type_Trigger = "Trigger";
		/// <summary>
		/// Custom Object Type for Objects with no collider
		/// </summary>
		public const string Object_Type_NoCollider = "NoCollider";

		/// <summary>
		/// Custom Property for Prefabs defining its name inside Resources folder. This will be used together with Property_PrefabPath to load the prefab inside Resources folder
		/// </summary>
		public const string Property_PrefabName = "-prefab";
		/// <summary>
		/// Custom Property for Prefabs defining its path inside Resources folder. This will be used together with Property_PrefabName to load the prefab inside Resources folder
		/// </summary>
		public const string Property_PrefabPath = "-prefab path";
		/// <summary>
		/// Custom Property for Prefabs defining its Z position. Useful for 2.5D and 3D games
		/// </summary>
		public const string Property_PrefabZDepth = "-prefab z depth";
		/// <summary>
		/// Custom Property for Prefabs defining to add a collider to the prefab
		/// </summary>
		public const string Property_PrefabAddCollider = "-prefab add collider ";
		/// <summary>
		/// Custom Property for Prefabs defining to send a message to all scripts attached to this prefab
		/// </summary>
		public const string Property_PrefabSendMessage = "-prefab send message ";
		/// <summary>
		/// Custom Property for Prefabs defining to set its position equals to its generated collider
		/// </summary>
		public const string Property_PrefabFixColliderPosition = "-prefab equals position collider";

		/// <summary>
		/// Custom Property for Colliders defining the GameObject's Physics Layer by ID
		/// </summary>
		public const string Property_Layer = "layer";
		/// <summary>
		/// Custom Property for Colliders defining the GameObject's Physics Layer by name.
		/// </summary>
		public const string Property_LayerName = "Layer Name";
		/// <summary>
		/// Custom Property for Colliders defining the GameObject's Tag
		/// </summary>
		public const string Property_Tag = "Tag";
		/// <summary>
		/// Custom Property for Colliders defining the renderer's Sorting Layer by name, if any renderer is present
		/// </summary>
		public const string Property_SortingLayerName = "sorting layer name";
		/// <summary>
		/// Custom Property for Colliders defining the renderer's Sorting Order, if any renderer is present
		/// </summary>
		public const string Property_SortingOrder = "sorting order";
		/// <summary>
		/// Custom Property for Colliders defining to generate a Mesh for debugging
		/// </summary>
		public const string Property_CreateMesh = "create mesh3d";
		/// <summary>
		/// Custom Property for Colliders defining a color for the material of this collider, if any.
		/// </summary>
		public const string Property_SetMaterialColor = "set material color";
		/// <summary>
		/// Custom Property for Colliders defining to add a component to the collider's GameObject
		/// </summary>
		public const string Property_AddComponent = "add component";
		/// <summary>
		/// Custom Property for Colliders defining to send a message to all scripts attached to the collider's GameObject
		/// </summary>
		public const string Property_SendMessage = "send message";
		#endregion

		#region Constructors
		/// <summary>
		/// Create a Tiled Map using the raw XML string as parameter.
		/// </summary>
		/// <param name="mapXML">Raw map XML string</param>
		/// <param name="MapName">Map's name</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="makeUnique">array with bools to make unique tiles for each tile layer. Defaults to false</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(	string mapXML, string MapName, string mapPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool[] makeUnique,
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)
		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;

			NanoXMLDocument document = new NanoXMLDocument(mapXML);

			_mapName = MapName;

			Parent = parent;

			DefaultSortingOrder = sortingOrder;
			PerLayerMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			_mapPath = mapPath;

			OnMapFinishedLoading = onMapFinishedLoading;

			Initialize(document);
		}

		/// <summary>
		/// Create a Tiled Map using the raw XML string as parameter.
		/// </summary>
		/// <param name="mapXML">Raw map XML string</param>
		/// <param name="MapName">Map's name</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="makeUnique">array with bools to make unique tiles for each tile layer. Defaults to false</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(string mapXML, string MapName, string mapPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool makeUnique, 
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)
		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;

			NanoXMLDocument document = new NanoXMLDocument(mapXML);

			_mapName = MapName;

			Parent = parent;

			DefaultSortingOrder = sortingOrder;
			GlobalMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			_mapPath = mapPath;

			OnMapFinishedLoading = onMapFinishedLoading;

			Initialize(document);
		}

		/// <summary>
		/// Create a Tiled Map using TextAsset as parameter
		/// </summary>
		/// <param name="mapText">Map's TextAsset</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="makeUnique">array with bools to make unique tiles for each tile layer. Defaults to false</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(	TextAsset mapText, string mapPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool[] makeUnique, 
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,		           
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)

		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;

			NanoXMLDocument document = new NanoXMLDocument(mapText.text);
			
			_mapName = mapText.name;
			
			Parent = parent;
			
			DefaultSortingOrder = sortingOrder;
			PerLayerMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			_mapPath = mapPath;

			OnMapFinishedLoading = onMapFinishedLoading;
			
			Initialize(document);
		}

		/// <summary>
		/// Create a Tiled Map using TextAsset as parameter
		/// </summary>
		/// <param name="mapText">Map's TextAsset</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="mapPath">Path to XML folder, so we can read relative paths for tilesets</param>
		/// <param name="makeUnique">array with bools to make unique tiles for each tile layer</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(TextAsset mapText, string mapPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool makeUnique, 
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)
		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;

			NanoXMLDocument document = new NanoXMLDocument(mapText.text);

			_mapName = mapText.name;

			Parent = parent;

			DefaultSortingOrder = sortingOrder;
			GlobalMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			_mapPath = mapPath;

			OnMapFinishedLoading = onMapFinishedLoading;

			Initialize(document);
		}



		/// <summary>
		/// Create a Tiled Map loading the XML from a StreamingAssetPath or a HTTP path (in the pc or web)
		/// </summary>
		/// <param name="wwwPath">Map's path with http for web files or without streaming assets path for local files</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="makeUnique">array with bools to make unique tiles for each tile layer</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(	string wwwPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool[] makeUnique,  
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,		           
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)
		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;
			_mapName = Path.GetFileNameWithoutExtension(wwwPath);
			_mapExtension = Path.GetExtension(wwwPath);
			if (string.IsNullOrEmpty(_mapExtension))
				_mapExtension = ".tmx";

			Parent = parent;

			DefaultSortingOrder = sortingOrder;
			PerLayerMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			if (!wwwPath.Contains("://"))
				_mapPath = string.Concat(Application.streamingAssetsPath, Path.AltDirectorySeparatorChar);
			else
			{
				// remove _mapName from wwwPath
				_mapPath = wwwPath.Replace(string.Concat(_mapName, _mapExtension), "");
			}

			OnMapFinishedLoading = onMapFinishedLoading;

			new Task(LoadFromPath(wwwPath), true);
		}

		/// <summary>
		/// Create a Tiled Map loading the XML from a StreamingAssetPath or a HTTP path (in the pc or web)
		/// </summary>
		/// <param name="wwwPath">Map's path with http for web files or without streaming assets path for local files</param>
		/// <param name="parent">This map's gameobject parent</param>
		/// <param name="baseTileMaterial">Base material to be used for the Tiles</param>
		/// <param name="sortingOrder">Base sorting order for the tile layers</param>
		/// <param name="makeUnique">Make unique tiles for all tile layers.</param>
		/// <param name="onMapFinishedLoading">Callback for when map finishes loading</param>
		/// <param name="simpleTileObjectCalculation">true to generate simplified tile collisions</param>
		/// <param name="tileObjectEllipsePrecision">Tile collisions ellipsoide approximation precision</param>
		/// <param name="clipperArcTolerance">Clipper arc angle tolerance</param>
		/// <param name="clipperDeltaOffset">Clipper delta offset</param>
		/// <param name="clipperEndType">Clipper Polygon end type</param>
		/// <param name="clipperJoinType">Clipper join type</param>
		/// <param name="clipperMiterLimit">Clipper limit for Miter join type</param>
		public Map(string wwwPath, GameObject parent,
					Material baseTileMaterial, int sortingOrder, bool makeUnique, 
					Action<Map> onMapFinishedLoading = null,
					int tileObjectEllipsePrecision = 16, bool simpleTileObjectCalculation = true,
					double clipperArcTolerance = 0.25, double clipperMiterLimit = 2.0,
					ClipperLib.JoinType clipperJoinType = ClipperLib.JoinType.jtRound,
					ClipperLib.EndType clipperEndType = ClipperLib.EndType.etClosedPolygon,
					float clipperDeltaOffset = 0)
		{
			_tileObjectEllipsePrecision = tileObjectEllipsePrecision;
			_simpleTileObjectCalculation = simpleTileObjectCalculation;
			_clipperArcTolerance = clipperArcTolerance;
			_clipperDeltaOffset = clipperDeltaOffset;
			_clipperEndType = clipperEndType;
			_clipperJoinType = clipperJoinType;
			_clipperMiterLimit = clipperMiterLimit;
			_mapName = Path.GetFileNameWithoutExtension(wwwPath);
			_mapExtension = Path.GetExtension(wwwPath);
			if (string.IsNullOrEmpty(_mapExtension))
				_mapExtension = ".tmx";

			Parent = parent;

			DefaultSortingOrder = sortingOrder;
			GlobalMakeUniqueTiles = makeUnique;
			BaseTileMaterial = baseTileMaterial;
			if (!wwwPath.Contains("://"))
				_mapPath = string.Concat(Application.streamingAssetsPath, Path.AltDirectorySeparatorChar);
			else
			{
				// remove _mapName from wwwPath
				_mapPath = wwwPath.Replace(string.Concat(_mapName, _mapExtension), "");
			}

			OnMapFinishedLoading = onMapFinishedLoading;

			new Task(LoadFromPath(wwwPath), true);
		}
		#endregion

		/// <summary>
		/// Load Map XML from a WWW path
		/// </summary>
		/// <param name="wwwPath">WWW path to load XML from</param>
		/// <returns>is loaded or not</returns>
		IEnumerator LoadFromPath(string wwwPath)
		{
			string result;
			string filePath = string.Concat(_mapPath, _mapName, _mapExtension);
			
			if (!filePath.Contains("://"))
				filePath = string.Concat("file://", filePath);
			//Debug.Log(filePath);

			WWW www = new WWW(filePath);
			yield return www;
			result = www.text;
			
			UsingStreamingAssetsPath = true;

			NanoXMLDocument document = new NanoXMLDocument(result);

			Initialize(document);
		}

		/// <summary>
		/// Initializes, Reads this Map's info
		/// </summary>
		/// <param name="document">NanoXMLDocument containing Map's XML</param>
		void Initialize(NanoXMLDocument document)
		{
			MapNode = document.RootNode;
			Orientation = (Orientation)Enum.Parse(typeof(Orientation), MapNode.GetAttribute("orientation").Value, true);
			Width = int.Parse(MapNode.GetAttribute("width").Value, CultureInfo.InvariantCulture);
			Height = int.Parse(MapNode.GetAttribute("height").Value, CultureInfo.InvariantCulture);
			TileWidth = int.Parse(MapNode.GetAttribute("tilewidth").Value, CultureInfo.InvariantCulture);
			TileHeight = int.Parse(MapNode.GetAttribute("tileheight").Value, CultureInfo.InvariantCulture);

			if (MapNode.GetAttribute("version") != null)
			{
				Version = MapNode.GetAttribute("version").Value;
			}
			else
				Version = string.Empty;

			if (MapNode.GetAttribute("renderorder") != null)
			{
				string renderOrder = MapNode.GetAttribute("renderorder").Value;
				MapRenderOrder = (RenderOrder)Enum.Parse(typeof(RenderOrder), renderOrder.Replace('-', '_'), true);
			}
			else
				MapRenderOrder = RenderOrder.Right_Down;

			if (MapNode.GetAttribute("backgroundcolor") != null)
			{
				string color = MapNode.GetAttribute("backgroundcolor").Value;
				string r = color.Substring(1, 2);
				string g = color.Substring(3, 2);
				string b = color.Substring(5, 2);
				this.BackgroundColor = new Color(
					(byte)Convert.ToInt32(r, 16),
					(byte)Convert.ToInt32(g, 16),
					(byte)Convert.ToInt32(b, 16));
			}

			if (_mapName == null)
				_mapName = "Map";

			if (!_mapPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
				_mapPath = _mapPath + Path.AltDirectorySeparatorChar;

			MapObject = new GameObject(_mapName);
			Transform mapObjectTransform = MapObject.transform;
			mapObjectTransform.parent = Parent.transform;

			mapObjectTransform.localPosition = Vector3.zero;
			MapObject.layer = mapObjectTransform.parent.gameObject.layer;

			NanoXMLNode propertiesElement = MapNode["properties"];
			if (propertiesElement != null)
				Properties = new PropertyCollection(propertiesElement);

			TileSets = new List<TileSet>();
			Tiles = new Dictionary<int, Tile>();
			_tileSetsToLoaded = 0;
			_numberOfTileSetsToLoad = 0;
			// First get how many tilesets we need to load
			foreach (NanoXMLNode node in MapNode.SubNodes)
			{
				if (node.Name.Equals("tileset"))
					_numberOfTileSetsToLoad++;
			}

			// Maps might not have any tileset, being just a tool for object placement :P
			if (_numberOfTileSetsToLoad < 1)
			{
				ContinueLoadingTiledMapAfterTileSetsLoaded();
			}

			// Then load all of them. After all loaded, continue with map loading
			foreach (NanoXMLNode node in MapNode.SubNodes)
			{
				if (node.Name.Equals("tileset"))
				{
					if (node.GetAttribute("source") != null)
					{
						int firstID = int.Parse(node.GetAttribute("firstgid").Value, CultureInfo.InvariantCulture);
						if (UsingStreamingAssetsPath)
						{
							// Run coroutine for www using TaskManager
							new Task(LoadExternalTileSet(node, _mapPath, firstID), true);
						}
						else
						{
							// Parse the path
							string path = node.GetAttribute("source").Value;
							string rootPath = Directory.GetParent(_mapPath).FullName;
							string appPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));

							while (path.StartsWith("../"))
							{
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
							path = rootPath + path;
							//path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

							TextAsset externalTileSetTextAsset = Resources.Load<TextAsset>(path);

							BuildExternalTileSet(externalTileSetTextAsset.text, Directory.GetParent(path).ToString(), firstID);
						}
					}
					else
					{
						new TileSet(node, _mapPath, this, UsingStreamingAssetsPath, OnFinishedLoadingTileSet);
					}
				}
			}
		}

		#region Load TileSets
		/// <summary>
		/// Load an external TileSet
		/// </summary>
		/// <param name="node">TileSet's NanoXMLNode</param>
		/// <param name="mapPath">Map's root directory</param>
		/// <param name="firstID">TileSet's firstID (external TileSet does not save this info)</param>
		/// <returns>is loaded or not</returns>
		IEnumerator LoadExternalTileSet(NanoXMLNode node, string mapPath, int firstID = 1)
		{
			string externalTileSetData = node.GetAttribute("source").Value;
			string filePath = mapPath;
			if (Application.isWebPlayer)
			{
				filePath = Path.Combine(filePath, Path.GetFileName(externalTileSetData));
			}
			else
			{
				if (Path.GetDirectoryName(externalTileSetData).Length > 0)
					filePath += Path.GetDirectoryName(externalTileSetData) + Path.AltDirectorySeparatorChar;
				if (filePath.Equals("/")) filePath = "";

				// if there's no :// assume we are using StreamingAssets path
				if (!filePath.Contains("://"))
					filePath = string.Concat("file://", Path.Combine(filePath, Path.GetFileName(externalTileSetData)));
			}
			
			WWW www = new WWW(filePath);
			yield return www;
			externalTileSetData = www.text;

			BuildExternalTileSet(externalTileSetData, mapPath, firstID);
		}

		/// <summary>
		/// Finally build TileSet info using read data
		/// </summary>
		/// <param name="tileSetData">TileSet raw XML</param>
		/// <param name="path">External TileSet root directory</param>
		/// <param name="firstID">TileSet's firstID (external TileSet does not save this info)</param>
		void BuildExternalTileSet(string tileSetData, string path, int firstID = 1)
		{
			NanoXMLDocument externalTileSet = new NanoXMLDocument(tileSetData);

			NanoXMLNode externalTileSetNode = externalTileSet.RootNode;
			new TileSet(externalTileSetNode, path, this, UsingStreamingAssetsPath, OnFinishedLoadingTileSet, firstID);
		}

		void OnFinishedLoadingTileSet(TileSet tileSet)
		{
			TileSets.Add(tileSet);
			foreach (KeyValuePair<int, Tile> item in tileSet.Tiles)
			{
				this.Tiles.Add(item.Key, item.Value);
			}
			_tileSetsToLoaded++;

			if (_tileSetsToLoaded >= _numberOfTileSetsToLoad)
				ContinueLoadingTiledMapAfterTileSetsLoaded();
		}

		void ContinueLoadingTiledMapAfterTileSetsLoaded()
		{
			// Generate Materials for Map batching
			List<Material> materials = new List<Material>();
			// Generate Materials
			int i = 0;
			for (i = 0; i < TileSets.Count; i++)
			{
				Material layerMat = new Material(BaseTileMaterial);
				layerMat.mainTexture = TileSets[i].Texture;
				materials.Add(layerMat);
			}

			Layers = new List<Layer>();
			i = 0;
			int tileLayerCount = 0;
			foreach (NanoXMLNode layerNode in MapNode.SubNodes)
			{
				if (!(layerNode.Name.Equals("layer") || layerNode.Name.Equals("objectgroup") || layerNode.Name.Equals("imagelayer")))
					continue;

				Layer layerContent;

				int layerDepth = 1 - (LayerDepthSpacing * i);

				if (layerNode.Name.Equals("layer"))
				{
					bool makeUnique = GlobalMakeUniqueTiles;
					if (PerLayerMakeUniqueTiles != null && tileLayerCount < PerLayerMakeUniqueTiles.Length)
						makeUnique = PerLayerMakeUniqueTiles[tileLayerCount];

					layerContent = new TileLayer(layerNode, this, layerDepth, makeUnique, materials);
					tileLayerCount++;
				}
				else if (layerNode.Name.Equals("objectgroup"))
				{
					layerContent = new MapObjectLayer(layerNode, this, layerDepth, materials);
				}
				else if (layerNode.Name.Equals("imagelayer"))
				{
					layerContent = new ImageLayer(layerNode, this, _mapPath, BaseTileMaterial);
				}
				else
				{
					throw new Exception("Unknown layer name: " + layerNode.Name);
				}

				// Layer names need to be unique for our lookup system, but Tiled
				// doesn't require unique names.
				string layerName = layerContent.Name;
				int duplicateCount = 2;

				// if a layer already has the same name...
				if (Layers.Find(l => l.Name == layerName) != null)
				{
					// figure out a layer name that does work
					do
					{
						layerName = string.Format("{0}{1}", layerContent.Name, duplicateCount);
						duplicateCount++;
					} while (Layers.Find(l => l.Name == layerName) != null);

					// log a warning for the user to see
					Debug.Log("Renaming layer \"" + layerContent.Name + "\" to \"" + layerName + "\" to make a unique name.");

					// save that name
					layerContent.Name = layerName;
				}
				layerContent.LayerDepth = layerDepth;
				Layers.Add(layerContent);
				namedLayers.Add(layerName, layerContent);
				i++;
			}

			if (OnMapFinishedLoading != null)
				OnMapFinishedLoading(this);
		}
		#endregion

		#region Sorting Order Calculation
		/// <summary>
		/// Calculate Tile's SortingOrder based on Map's RenderOrder, Map's Orientation and the Tile's index
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <returns>SortingOrder to be used for a Renderer in this Tile index</returns>
		public int GetSortingOrder(int x, int y)
		{
			int sortingOrder = 0;
			switch (MapRenderOrder)
			{
				case RenderOrder.Right_Down:
					sortingOrder = y * Width + x;
					break;
				case RenderOrder.Right_Up:
					sortingOrder = (Height - y) * Width + x;
					break;
				case RenderOrder.Left_Down:
					sortingOrder = y * Width + Height - x;
					break;
				case RenderOrder.Left_Up:
					sortingOrder = (Height - y) * Width + Height - x;
					break;
			}
			
			return sortingOrder;
		}

		/// <summary>
		/// Calculate Tile's SortingOrder based on Map's RenderOrder, Map's Orientation and the Tile's index
		/// </summary>
		/// <param name="x">Tile X index</param>
		/// <param name="y">Tile Y index</param>
		/// <returns>SortingOrder to be used for a Renderer in this Tile index</returns>
		public int GetSortingOrder(float x, float y)
		{
			return GetSortingOrder(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
		}
		#endregion

		#region Position Converters
		/// <summary>
		/// Converts a point in world space into tiled space.
		/// </summary>
		/// <param name="worldPoint">The point in world space to convert into tiled space.</param>
		/// <returns>The point in Tiled space.</returns>
		public Vector2 WorldPointToTiledPosition(Vector2 worldPoint)
		{
			Vector2 p = new Vector2();

			if (Orientation == X_UniTMX.Orientation.Orthogonal)
			{
				// simple conversion to tile indices
				p.x = worldPoint.x;
				p.y = -worldPoint.y;
			}
			else if (Orientation == X_UniTMX.Orientation.Isometric)
			{
				float ratio = TileHeight / (float)TileWidth;
				// for some easier calculations, convert wordPoint to pixels
				Vector2 point = new Vector2(worldPoint.x * TileWidth, -worldPoint.y / ratio * TileHeight);

				// Code almost straight from Tiled's libtiled :P

				point.x -= Height * TileWidth / 2.0f;
				float tileX = point.x / (float)TileWidth;
				float tileY = point.y / (float)TileHeight;

				p.x = tileY + tileX;
				p.y = tileY - tileX;
			}
			else if (Orientation == X_UniTMX.Orientation.Staggered)
			{
				float ratio = TileHeight / (float)TileWidth;
				// for some easier calculations, convert wordPoint to pixels
				Vector2 point = new Vector2(worldPoint.x * (float)TileWidth, -worldPoint.y / ratio * (float)TileHeight);

				float halfTileHeight = TileHeight / 2.0f;

				// Code almost straight from Tiled's libtiled :P

				// Getting grid-aligned tile index
				float tileX = point.x / (float)TileWidth;
				float tileY = point.y / (float)TileHeight * 2;

				// Relative x and y pos to tile
				float relX = point.x - tileX * (float)TileWidth;
				float relY = point.y - tileY / 2.0f * (float)TileHeight;

				if (halfTileHeight - relX * ratio > relY)
				{
					p.y = tileY - 1;
					if (tileY % 2 > 0)
						p.x = tileX;
					else
						p.x = tileX - 1;
				}
				else if (-halfTileHeight + relX * ratio > relY)
				{
					p.y = tileY - 1;
					if (tileY % 2 > 0)
						p.x = tileX + 1;
					else
						p.x = tileX;
				}
				else if (halfTileHeight + relX * ratio < relY)
				{
					p.y = tileY + 1;
					if (tileY % 2 > 0)
						p.x = tileX;
					else
						p.x = tileX - 1;
				}
				else if (halfTileHeight * 3 - relX * ratio < relY)
				{
					p.y = tileY + 1;
					if (tileY % 2 > 0)
						p.x = tileX + 1;
					else
						p.x = tileX;
				}
				else
				{
					p.x = tileX;
					p.y = tileY;
				}
			}

			return p;
		}

		/// <summary>
		/// Converts a point in world space into tile indices that can be used to index into a TileLayer.
		/// </summary>
		/// <param name="worldPoint">The point in world space to convert into tile indices.</param>
		/// <returns>A Point containing the X/Y indices of the tile that contains the point.</returns>
		public Vector2 WorldPointToTileIndex(Vector2 worldPoint)
		{
			Vector2 p = new Vector2();
			
			if (Orientation == X_UniTMX.Orientation.Orthogonal)
			{
				// simple conversion to tile indices
				p.x = worldPoint.x;
				p.y = -worldPoint.y;
			}
			else if (Orientation == X_UniTMX.Orientation.Isometric)
			{
				float ratio = TileHeight / (float)TileWidth;
				// for some easier calculations, convert wordPoint to pixels
				Vector2 point = new Vector2(worldPoint.x * TileWidth, -worldPoint.y / ratio * TileHeight);
				
				// Code almost straight from Tiled's libtiled :P
				point.x -= Height * TileWidth / 2.0f;
				float tileX = point.x / (float)TileWidth;
				float tileY = point.y / (float)TileHeight;

				p.x = tileY + tileX;
				p.y = tileY - tileX;
			}
			else if (Orientation == X_UniTMX.Orientation.Staggered)
			{
				float ratio = TileHeight / (float)TileWidth;
				// for some easier calculations, convert wordPoint to pixels
				Vector2 point = new Vector2(worldPoint.x * TileWidth, -worldPoint.y / ratio * TileHeight);
				
				float halfTileHeight = TileHeight / 2.0f;

				// Code almost straight from Tiled's libtiled :P

				// Getting grid-aligned tile index
				int tileX = Mathf.FloorToInt(point.x / (float)TileWidth);
				int tileY = Mathf.FloorToInt(point.y / (float)TileHeight) * 2;

				// Relative x and y pos to tile
				float relX = point.x - tileX * TileWidth;
				float relY = point.y - tileY / 2.0f * TileHeight;
				
				if (halfTileHeight - relX * ratio > relY)
				{
					p.y = tileY - 1;
					if (tileY % 2 > 0)
						p.x = tileX;
					else
						p.x = tileX - 1;
				}
				else if (-halfTileHeight + relX * ratio > relY)
				{
					p.y = tileY - 1;
					if (tileY % 2 > 0)
						p.x = tileX + 1;
					else
						p.x = tileX;
				}
				else if (halfTileHeight + relX * ratio < relY)
				{
					p.y = tileY + 1;
					if (tileY % 2 > 0)
						p.x = tileX;
					else
						p.x = tileX - 1;
				}
				else if (halfTileHeight * 3 - relX * ratio < relY)
				{
					p.y = tileY + 1;
					if (tileY % 2 > 0)
						p.x = tileX + 1;
					else
						p.x = tileX;
				}
				else
				{
					p.x = tileX;
					p.y = tileY;
				}
			}
			
			p.x = Mathf.FloorToInt(p.x);
			p.y = Mathf.FloorToInt(p.y);
			return p;
		}

		/// <summary>
		/// Converts a tile index or position into world coordinates
		/// </summary>
		/// <param name="posX">Tile index or position of object in tiled</param>
		/// <param name="posY">Tile index or position of object in tiled</param>
		/// <param name="tile">Tile to get size from</param>
		/// <returns>World's X and Y position</returns>
		public Vector2 TiledPositionToWorldPoint(float posX, float posY, Tile tile = null)
		{
			Vector2 p = Vector2.zero;
			float currentTileWidth = TileWidth;
			float currentTileHeight = TileHeight;
			if (tile == null)
			{
				Dictionary<int, Tile>.ValueCollection.Enumerator enumerator = Tiles.Values.GetEnumerator();
				enumerator.MoveNext();
				if (enumerator.Current != null && enumerator.Current.TileSet != null)
				{
					currentTileWidth = enumerator.Current.TileSet.TileWidth;
					currentTileHeight = enumerator.Current.TileSet.TileHeight;
				}
			}
			else
			{
				if (tile.TileSet != null)
				{
					currentTileWidth = tile.TileSet.TileWidth;
					currentTileHeight = tile.TileSet.TileHeight;
				}
			}

			if (Orientation == Orientation.Orthogonal)
			{
				p.x = posX * (TileWidth / currentTileWidth);
				p.y = -posY * (TileHeight / currentTileHeight) * (currentTileHeight / currentTileWidth);
			}
			else if (Orientation == Orientation.Isometric)
			{
				p.x = (TileWidth / 2.0f * (Width - posY + posX)) / (float)TileWidth;//(TileWidth / 2.0f * (Width / 2.0f - posY + posX)) / (float)TileWidth;//
				p.y = -Height + TileHeight * (Height - ((posX + posY) / (TileWidth / (float)TileHeight)) / 2.0f) / (float)TileHeight;				
			}
			else if (Orientation == X_UniTMX.Orientation.Staggered)
			{
				p.x = posX * (TileWidth / currentTileWidth);
				if (Mathf.FloorToInt(Mathf.Abs(posY)) % 2 > 0)
					p.x += 0.5f;
				p.y = -posY * (TileHeight / 2.0f / currentTileHeight) * (currentTileHeight / currentTileWidth);
			}

			return p;
		}

		/// <summary>
		/// Converts a tile index or position into 3D world coordinates
		/// </summary>
		/// <param name="posX">Tile index or position of object in tiled</param>
		/// <param name="posY">Tile index or position of object in tiled</param>
		/// <param name="posZ">zIndex of object</param>
		/// <param name="tile">Tile to get size from</param>
		/// <returns>World's X, Y and Z position</returns>
		public Vector3 TiledPositionToWorldPoint(float posX, float posY, float posZ, Tile tile = null)
		{
			Vector3 p = new Vector3();

			Vector2 p2d = TiledPositionToWorldPoint(posX, posY, tile);
			// No need to change Z value, this function is just a helper
			p.x = p2d.x;
			p.y = p2d.y;
			p.z = posZ;
			return p;
		}

		/// <summary>
		/// Converts a tile index or position into 3D world coordinates
		/// </summary>
		/// <param name="position">Tile index or position of object in Tiled</param>
		/// <param name="tile">Tile to get size from</param>
		/// <returns>World's X and Y position</returns>
		public Vector2 TiledPositionToWorldPoint(Vector2 position, Tile tile = null)
		{
			return TiledPositionToWorldPoint(position.x, position.y, tile);
		}
		#endregion

		#region Getters
		/// <summary>
		/// Returns the set of all objects in the map.
		/// </summary>
		/// <returns>A new set of all objects in the map.</returns>
		public IEnumerable<MapObject> GetAllObjects()
		{
			foreach (var layer in Layers)
			{
				MapObjectLayer objLayer = layer as MapObjectLayer;
				if (objLayer == null)
					continue;

				foreach (var obj in objLayer.Objects)
				{
					yield return obj;
				}
			}
		}

		/// <summary>
		/// Finds an object in the map using a delegate.
		/// </summary>
		/// <remarks>
		/// This method is used when an object is desired, but there is no specific
		/// layer to find the object on. The delegate allows the caller to create 
		/// any logic they want for finding the object. A simple example for finding
		/// the first object named "goal" in any layer would be this:
		/// 
		/// var goal = map.FindObject((layer, obj) => return obj.Name.Equals("goal"));
		/// 
		/// You could also use the layer name or any other logic to find an object.
		/// The first object for which the delegate returns true is the object returned
		/// to the caller. If the delegate never returns true, the method returns null.
		/// </remarks>
		/// <param name="finder">The delegate used to search for the object.</param>
		/// <returns>The MapObject if the delegate returned true, null otherwise.</returns>
		public MapObject FindObject(MapObjectFinder finder)
		{
			foreach (var layer in Layers)
			{
				MapObjectLayer objLayer = layer as MapObjectLayer;
				if (objLayer == null)
					continue;

				foreach (var obj in objLayer.Objects)
				{
					if (finder(objLayer, obj))
						return obj;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds a collection of objects in the map using a delegate.
		/// </summary>
		/// <remarks>
		/// This method performs basically the same process as FindObject, but instead
		/// of returning the first object for which the delegate returns true, it returns
		/// a collection of all objects for which the delegate returns true.
		/// </remarks>
		/// <param name="finder">The delegate used to search for the object.</param>
		/// <returns>A collection of all MapObjects for which the delegate returned true.</returns>
		public IEnumerable<MapObject> FindObjects(MapObjectFinder finder)
		{
			foreach (var layer in Layers)
			{
				MapObjectLayer objLayer = layer as MapObjectLayer;
				if (objLayer == null)
					continue;

				foreach (var obj in objLayer.Objects)
				{
					if (finder(objLayer, obj))
						yield return obj;
				}
			}
		}

		/// <summary>
		/// Gets a layer by name.
		/// </summary>
		/// <param name="name">The name of the layer to retrieve.</param>
		/// <returns>The layer with the given name.</returns>
		public Layer GetLayer(string name)
		{
			if (namedLayers.ContainsKey(name))
				return namedLayers[name];
			return null;
		}

		/// <summary>
		/// Gets a tile layer by name.
		/// </summary>
		/// <param name="name">The name of the tile layer to retrieve.</param>
		/// <returns>The tile layer with the given name.</returns>
		public TileLayer GetTileLayer(string name)
		{
			if (namedLayers.ContainsKey(name))
				return namedLayers[name] as TileLayer;
			return null;
		}

		/// <summary>
		/// Gets an object layer by name.
		/// </summary>
		/// <param name="name">The name of the object layer to retrieve.</param>
		/// <returns>The object layer with the given name.</returns>
		public MapObjectLayer GetObjectLayer(string name)
		{
			if (namedLayers.ContainsKey(name))
				return namedLayers[name] as MapObjectLayer;
			return null;
		}

		/// <summary>
		/// Gets the number of TileLayer in this Map
		/// </summary>
		/// <returns>number of TileLayer</returns>
		public int GetTileLayersCount()
		{
			int count = 0;
			foreach (var layerPair in namedLayers)
			{
				if (layerPair.Value is TileLayer)
					count++;
			}

			return count;
		}

		/// <summary>
		/// Gets the number of MapObjectLayer in this Map
		/// </summary>
		/// <returns>number of MapObjectLayer</returns>
		public int GetObjectLayersCount()
		{
			int count = 0;
			foreach (var layerPair in namedLayers)
			{
				if (layerPair.Value is MapObjectLayer)
					count++;
			}

			return count;
		}

		/// <summary>
		/// Gets the number of ImageLayer in this Map
		/// </summary>
		/// <returns>number of ImageLayer</returns>
		public int GetImageLayersCount()
		{
			int count = 0;
			foreach (var layerPair in namedLayers)
			{
				if (layerPair.Value is ImageLayer)
					count++;
			}

			return count;
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
		#endregion

		#region Colliders Generators
		#region Tile Collisions Generator

		protected GameObject Generate2DTileCollision(TileLayer layer, int counter, Transform parent, List<Vector2> points, bool isTrigger = false, float zDepth = 1)
		{
			GameObject newSubCollider = new GameObject("Tile Collisions " + layer.Name + "_" + counter);
			newSubCollider.transform.parent = parent;
			newSubCollider.transform.localPosition = new Vector3(0, 0, zDepth);

			// Add the last point equals to the first to close the collider area
			// it's necessary only if the first point is diffent from the first one
			if (points[0].x != points[points.Count - 1].x || points[0].y != points[points.Count - 1].y)
			{
				points.Add(points[0]);
			}

			EdgeCollider2D edgeCollider = newSubCollider.AddComponent<EdgeCollider2D>();
			edgeCollider.isTrigger = isTrigger;

			Vector2[] pointsVec = points.ToArray();

			for (int j = 0; j < pointsVec.Length; j++)
			{
				pointsVec[j] = TiledPositionToWorldPoint(pointsVec[j]);
			}

			edgeCollider.points = pointsVec;
			return newSubCollider;
		}

		protected GameObject Generate3DTileCollision(TileLayer layer, int counter, Transform parent, List<Vector2> points, bool isTrigger = false, float zDepth = 1, float colliderWidth = 1, bool innerCollision = false)
		{
			GameObject newSubCollider = new GameObject("Tile Collisions " + layer.Name + "_" + counter);
			newSubCollider.transform.parent = parent;
			newSubCollider.transform.localPosition = new Vector3(0, 0, zDepth);

			Mesh colliderMesh = new Mesh();
			colliderMesh.name = "TileCollider_" + layer.Name + "_" + counter;
			MeshCollider mc = newSubCollider.AddComponent<MeshCollider>();

			mc.isTrigger = isTrigger;

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();

			GenerateVerticesAndTris(points, vertices, triangles, zDepth, colliderWidth, innerCollision, true, true);

			// Connect last point with first point (create the face between them)
			triangles.Add(vertices.Count - 1);
			triangles.Add(1);
			triangles.Add(0);

			triangles.Add(0);
			triangles.Add(vertices.Count - 2);
			triangles.Add(vertices.Count - 1);

			FillFaces(points, triangles);

			colliderMesh.vertices = vertices.ToArray();
			colliderMesh.uv = new Vector2[colliderMesh.vertices.Length];
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.triangles = triangles.ToArray();
			colliderMesh.RecalculateNormals();

			mc.sharedMesh = colliderMesh;

			newSubCollider.isStatic = true;
			
			return newSubCollider;
		}

		/// <summary>
		/// Generate Colliders based on Tile Collisions
		/// </summary>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <returns>A GameObject containing all generated colliders</returns>
		public GameObject GenerateTileCollisions(bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1, bool innerCollision = false)
		{
			GameObject tileColisions = new GameObject("Tile Collisions");
			tileColisions.transform.parent = MapObject.transform;
			tileColisions.transform.localPosition = Vector3.zero;

			ClipperLib.Clipper clipper = new ClipperLib.Clipper();
			List<List<ClipperLib.IntPoint>> pathsList = new List<List<ClipperLib.IntPoint>>();
			List<List<ClipperLib.IntPoint>> solution = new List<List<ClipperLib.IntPoint>>();
			// Iterate over each Tile Layer, grab all TileObjects inside this layer and use their Paths with ClipperLib to generate one polygon collider
			foreach (var layer in Layers)
			{
				if (layer is TileLayer)
				{
					clipper.Clear();
					solution.Clear();
					pathsList.Clear();
					TileLayer tileLayer = layer as TileLayer;
					for (int x = 0; x < tileLayer.Tiles.Width; x++)
					{
						for (int y = 0; y < tileLayer.Tiles.Height; y++)
						{
							Tile t = tileLayer.Tiles[x, y];
							if (t == null || t.TileSet == null || t.TileSet.TilesObjects == null)
								continue;
							if (t.TileSet.TilesObjects.ContainsKey(t.OriginalID))
							{
								List<TileObject> tileObjs = t.TileSet.TilesObjects[t.OriginalID];
								foreach (var tileObj in tileObjs)
								{
									pathsList.Add(tileObj.GetPath(x, y, t.SpriteEffects, _tileObjectEllipsePrecision));
								}
							}
						}
					}
					// Add the paths to be merged to ClipperLib
					clipper.AddPaths(pathsList, ClipperLib.PolyType.ptSubject, true);
					// Merge it!
					//clipper.PreserveCollinear = false;
					//clipper.ReverseSolution = true;
					clipper.StrictlySimple = _simpleTileObjectCalculation;
					if (!clipper.Execute(ClipperLib.ClipType.ctUnion, solution))
						continue;
					clipper.Execute(ClipperLib.ClipType.ctUnion, solution);
					// Now solution should contain all vertices of the collision object, but they are still multiplied by TileObject.ClipperScale!

					#region Implementation of increase and decrease offset polygon.
					if(_simpleTileObjectCalculation == false) {
						// Link of the example of ClipperLib:
						// http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Classes/ClipperOffset/_Body.htm

						ClipperLib.ClipperOffset co = new ClipperLib.ClipperOffset(_clipperMiterLimit,_clipperArcTolerance);
						foreach(List<ClipperLib.IntPoint> item in solution) {
							co.AddPath(item, _clipperJoinType,_clipperEndType);
						}
						solution.Clear();
						co.Execute(ref solution, _clipperDeltaOffset*TileObject.ClipperScale);
					}
					#endregion

					// Generate this path's collision
					GameObject newCollider = new GameObject("Tile Collisions " + layer.Name);
					newCollider.transform.parent = tileColisions.transform;
					newCollider.transform.localPosition = new Vector3(0, 0, zDepth);

					// Finally, generate the edge collider
					int counter = 0;
					
					for (int i = 0; i < solution.Count; i++)
					{
						if (solution[i].Count < 1)
							continue;

						List<Vector2> points = new List<Vector2>();

						for (int j = 0; j < solution[i].Count; j++)
						{
							points.Add(
								new Vector2(
									solution[i][j].X / (float)TileObject.ClipperScale,
									solution[i][j].Y / (float)TileObject.ClipperScale
								)
							);
						}
						
						if (used2DColider)
							Generate2DTileCollision(tileLayer, counter, newCollider.transform, points, isTrigger, zDepth);
						else
							Generate3DTileCollision(tileLayer, counter, newCollider.transform, points, isTrigger, zDepth, colliderWidth, innerCollision);

						counter++;
					}
					
					newCollider.isStatic = true;
				}
			}			

			return tileColisions;
		}
		#endregion

		#region Box Colliders
		private void AddBoxCollider3D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject gameObjectMesh = null;
			// Orthogonal and Staggered maps can use BoxCollider, Isometric maps must use polygon collider
			if (Orientation != X_UniTMX.Orientation.Isometric)
			{
				if (obj.GetPropertyAsBoolean(Property_CreateMesh))
				{
					gameObjectMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
					gameObjectMesh.name = obj.Name;
					gameObjectMesh.transform.parent = gameObject.transform;
					gameObjectMesh.transform.localPosition = Vector3.zero;
					gameObjectMesh.GetComponent<Collider>().isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);
				}
				else
				{
					gameObject.AddComponent<BoxCollider>();
					gameObject.GetComponent<Collider>().isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);
				}
				gameObject.transform.localScale = new Vector3(obj.Bounds.width, obj.Bounds.height, colliderWidth);
			}
			else
			{
				List<Vector2> points = new List<Vector2>();
				points.Add(new Vector2(obj.Bounds.xMin - obj.Bounds.x, obj.Bounds.yMax - obj.Bounds.y));
				points.Add(new Vector2(obj.Bounds.xMin - obj.Bounds.x, obj.Bounds.yMin - obj.Bounds.y));
				points.Add(new Vector2(obj.Bounds.xMax - obj.Bounds.x, obj.Bounds.yMin - obj.Bounds.y));
				points.Add(new Vector2(obj.Bounds.xMax - obj.Bounds.x, obj.Bounds.yMax - obj.Bounds.y));
				X_UniTMX.MapObject isoBox = new MapObject(obj.Name, obj.Type, obj.Bounds, obj.Properties, obj.GID, points, obj.Rotation, obj.ParentObjectLayer);

				AddPolygonCollider3D(gameObject, isoBox, isTrigger, zDepth, colliderWidth);
				//gameObject = GeneratePolygonCollider3D(isoBox, isTrigger, zDepth, colliderWidth);
			}

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody>();
				gameObject.GetComponent<Rigidbody>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			if(gameObjectMesh != null)
				ApplyCustomProperties(gameObjectMesh, obj);
			else
				ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GenerateBoxCollider3D(MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x + (obj.Bounds.width / 2.0f), obj.Bounds.y + (obj.Bounds.height / 2.0f), zDepth);
			
			AddBoxCollider3D(newCollider, obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}

		private void AddBoxCollider2D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			// Orthogonal and Staggered maps can use BoxCollider, Isometric maps must use polygon collider
			if (Orientation != X_UniTMX.Orientation.Isometric)
			{
				BoxCollider2D bx = gameObject.AddComponent<BoxCollider2D>();
				bx.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

				bx.offset = new Vector2(obj.Bounds.width / 2.0f, -obj.Bounds.height / 2.0f);
				bx.size = new Vector2(obj.Bounds.width, obj.Bounds.height);
			}
			else if (Orientation == X_UniTMX.Orientation.Isometric)
			{
				PolygonCollider2D pc = gameObject.AddComponent<PolygonCollider2D>();
				pc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);
				Vector2[] points = new Vector2[4];
				points[0] = TiledPositionToWorldPoint(obj.Bounds.xMin - obj.Bounds.x, obj.Bounds.yMax - obj.Bounds.y);
				points[1] = TiledPositionToWorldPoint(obj.Bounds.xMin - obj.Bounds.x, obj.Bounds.yMin - obj.Bounds.y);
				points[2] = TiledPositionToWorldPoint(obj.Bounds.xMax - obj.Bounds.x, obj.Bounds.yMin - obj.Bounds.y);
				points[3] = TiledPositionToWorldPoint(obj.Bounds.xMax - obj.Bounds.x, obj.Bounds.yMax - obj.Bounds.y);
				points[0].x -= Width / 2.0f;
				points[1].x -= Width / 2.0f;
				points[2].x -= Width / 2.0f;
				points[3].x -= Width / 2.0f;
				pc.SetPath(0, points);
			}

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody2D>();
				gameObject.GetComponent<Rigidbody2D>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GenerateBoxCollider2D(MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);

			AddBoxCollider2D(newCollider, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
			
		}
		/// <summary>
		/// Generate a Box collider mesh for 3D, or a BoxCollider2D for 2D (a PolygonCollider2D will be created for Isometric maps).
		/// </summary>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public GameObject GenerateBoxCollider(MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			return used2DColider ? 
				GenerateBoxCollider2D(obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic) : 
				GenerateBoxCollider3D(obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
		}

		/// <summary>
		/// Adds a Box Collider 2D or 3D to an existing GameObject using one MapObject as properties source
		/// </summary>
		/// <param name="gameObject">GameObject to add a Box Collider</param>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		public void AddBoxCollider(GameObject gameObject, MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			if (used2DColider)
				AddBoxCollider2D(gameObject, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);
			else
				AddBoxCollider3D(gameObject, obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
		}
		#endregion

		#region Ellipse/Circle/Capsule Colliders
		private void ApproximateEllipse2D(GameObject newCollider, MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			// since there's no "EllipseCollider2D", we must create one by approximating a polygon collider
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);
			
			PolygonCollider2D polygonCollider = newCollider.AddComponent<PolygonCollider2D>();

			polygonCollider.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			int segments = EllipsoideColliderApproximationFactor;

			// Segments per quadrant
			int incFactor = Mathf.FloorToInt(segments / 4.0f);
			float minIncrement = 2 * Mathf.PI / (incFactor * segments / 2.0f);
			int currentInc = 0;
			// grow represents if we are going right on x-axis (true) or left (false)
			bool grow = true;

			Vector2[] points = new Vector2[segments];
			// Ellipsoide center
			Vector2 center = new Vector2(obj.Bounds.width / 2.0f, obj.Bounds.height / 2.0f);

			float r = 0;
			float angle = 0;
			for (int i = 0; i < segments; i++)
			{
				// Calculate radius at each point
				angle += currentInc * minIncrement;

				r = obj.Bounds.width * obj.Bounds.height / Mathf.Sqrt(Mathf.Pow(obj.Bounds.height * Mathf.Cos(angle), 2) + Mathf.Pow(obj.Bounds.width * Mathf.Sin(angle), 2)) / 2.0f;
				// Define the point localization using the calculated radius, angle and center
				points[i] = r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) + center;

				points[i] = TiledPositionToWorldPoint(points[i].x, points[i].y);

				// Offset points where needed
				if (Orientation == X_UniTMX.Orientation.Isometric)
					points[i].x -= Width / 2.0f;
				if (Orientation == X_UniTMX.Orientation.Staggered)
					points[i].y *= TileWidth / (float)TileHeight * 2.0f;

				// if we are "growing", increment the angle, else, start decrementing it to close the polygon
				if (grow)
					currentInc++;
				else
					currentInc--;
				if (currentInc > incFactor - 1 || currentInc < 1)
					grow = !grow;

				// POG :P -> Orthogonal and Staggered Isometric generated points are slightly offset on Y
				if (Orientation != X_UniTMX.Orientation.Isometric)
				{
					if (i < 1 || i == segments / 2 - 1)
						points[i].y -= obj.Bounds.height / 20.0f;
					if (i >= segments - 1 || i == segments / 2)
						points[i].y += obj.Bounds.height / 20.0f;
				}
			}

			polygonCollider.SetPath(0, points);
		}

		private void ApproximateEllipse3D(GameObject newCollider, MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			// since there's no "EllipseCollider", we must create one by approximating a polygon collider
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);

			Mesh colliderMesh = new Mesh();
			colliderMesh.name = "Collider_" + obj.Name;
			MeshCollider mc = newCollider.AddComponent<MeshCollider>();
			mc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			int segments = EllipsoideColliderApproximationFactor;

			// Segments per quadrant
			int incFactor = Mathf.FloorToInt(segments / 4.0f);
			float minIncrement = 2 * Mathf.PI / (incFactor * segments / 2.0f);
			int currentInc = 0;
			bool grow = true;

			Vector2[] points = new Vector2[segments];

			float width = obj.Bounds.width;
			float height = obj.Bounds.height;

			Vector2 center = new Vector2(width / 2.0f, height / 2.0f);
			
			float r = 0;
			float angle = 0;
			for (int i = 0; i < segments; i++)
			{
				// Calculate radius at each point
				//angle = i * increment;
				angle += currentInc * minIncrement;
				r = width * height / Mathf.Sqrt(Mathf.Pow(height * Mathf.Cos(angle), 2) + Mathf.Pow(width * Mathf.Sin(angle), 2)) / 2.0f;
				points[i] = r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) + center;
				if (Orientation == X_UniTMX.Orientation.Staggered)
					points[i].y *= -1;

				if(grow)
					currentInc++;
				else
					currentInc--;
				if (currentInc > incFactor - 1 || currentInc < 1)
					grow = !grow;

				// POG :P
				if (Orientation != X_UniTMX.Orientation.Isometric)
				{
					if (i < 1 || i == segments / 2 - 1)
						points[i].y -= obj.Bounds.height / 20.0f;
					if (i >= segments - 1 || i == segments / 2)
						points[i].y += obj.Bounds.height / 20.0f;
				}
			}

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();

			GenerateVerticesAndTris(new List<Vector2>(points), vertices, triangles, zDepth, colliderWidth, false, !(Orientation == X_UniTMX.Orientation.Staggered));

			// Connect last point with first point (create the face between them)
			triangles.Add(vertices.Count - 1);
			triangles.Add(1);
            triangles.Add(0);
            
			triangles.Add(0);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

			FillFaces(points, triangles);
            
			colliderMesh.vertices = vertices.ToArray();
			colliderMesh.uv = new Vector2[colliderMesh.vertices.Length];
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.uv2 = colliderMesh.uv;
            colliderMesh.triangles = triangles.ToArray();
			colliderMesh.RecalculateNormals();

			mc.sharedMesh = colliderMesh;
		}

		private void AddEllipseCollider3D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject gameObjectMesh = null;
			if (Orientation != X_UniTMX.Orientation.Isometric && obj.Bounds.width == obj.Bounds.height)
			{
				CapsuleCollider cc = null;
				if (obj.GetPropertyAsBoolean(Property_CreateMesh))
				{
					gameObjectMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
					gameObjectMesh.name = obj.Name;
					gameObjectMesh.transform.parent = gameObject.transform;
					gameObjectMesh.transform.localPosition = new Vector3(obj.Bounds.height / 2.0f, -obj.Bounds.width / 2.0f);

					cc = gameObjectMesh.GetComponent<Collider>() as CapsuleCollider;
					gameObjectMesh.GetComponent<Collider>().isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);
					gameObjectMesh.transform.localScale = new Vector3(obj.Bounds.width, colliderWidth, obj.Bounds.height);
					gameObjectMesh.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
				}
				else
				{
					cc = gameObject.AddComponent<CapsuleCollider>();

					cc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

					cc.center = new Vector3(obj.Bounds.height / 2.0f, -obj.Bounds.width / 2.0f);

					cc.direction = 0;
					cc.radius = obj.Bounds.height / 2.0f;
					cc.height = obj.Bounds.width;
				}
			}
			else
			{
				ApproximateEllipse3D(gameObject, obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
			}

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody>();
				gameObject.GetComponent<Rigidbody>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			if(gameObjectMesh)
				ApplyCustomProperties(gameObjectMesh, obj);
			else
				ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GenerateEllipseCollider3D(MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;

			AddEllipseCollider3D(newCollider, obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}

		private void AddEllipseCollider2D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			if (Orientation != X_UniTMX.Orientation.Isometric && obj.Bounds.width == obj.Bounds.height)
			{
				CircleCollider2D cc = gameObject.AddComponent<CircleCollider2D>();
				cc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

				gameObject.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);
				cc.offset = new Vector2(obj.Bounds.width / 2.0f, -obj.Bounds.height / 2.0f);

				cc.radius = obj.Bounds.width / 2.0f;

			}
			else
			{
				ApproximateEllipse2D(gameObject, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);
			}


			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody2D>();
				gameObject.GetComponent<Rigidbody2D>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GenerateEllipseCollider2D(MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;

			AddEllipseCollider2D(newCollider, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}

		/// <summary>
		/// Generate an Ellipse Collider mesh.
		/// To mimic Tiled's Ellipse Object properties, a Capsule collider is created if map projection is Orthogonal and ellipse inside Tiled is a circle.
		/// For 2D, a CircleCollider2D will be created if ellipse is a circle, else a PolygonCollider will be approximated to an ellipsoid, for 3D, a PolygonCollider mesh will be approximated to an ellipsoid
		/// </summary>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public GameObject GenerateEllipseCollider(MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			return used2DColider ? 
				GenerateEllipseCollider2D(obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic) : 
				GenerateEllipseCollider3D(obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
		}

		/// <summary>
		/// Adds an Ellipse Collider to an existing GameObject using one MapObject as properties source
		/// </summary>
		/// <param name="gameObject">GameObject to add an Ellipse Collider</param>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		public void AddEllipseCollider(GameObject gameObject, MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			if (used2DColider)
				AddEllipseCollider2D(gameObject, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);
			else
				AddEllipseCollider3D(gameObject, obj, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
		}

		#endregion

		#region 3D Helpers
		private void CreateFrontBackPoints(Vector3 refPoint, out Vector3 refFront, out Vector3 refBack, float zDepth, float colliderWidth, bool calculateWorldPos = true, bool ignoreOrientation = false) {
			if (calculateWorldPos)
			{
				refFront = TiledPositionToWorldPoint(refPoint.x, refPoint.y, zDepth - colliderWidth / 2.0f);
				refBack = TiledPositionToWorldPoint(refPoint.x, refPoint.y, zDepth + colliderWidth / 2.0f);
			}
			else
			{
				refFront = new Vector3(refPoint.x, refPoint.y, zDepth - colliderWidth / 2.0f);
				refBack = new Vector3(refPoint.x, refPoint.y, zDepth + colliderWidth / 2.0f);
			}
			if (!ignoreOrientation && Orientation == X_UniTMX.Orientation.Isometric)
			{
				refFront.x -= Width / 2.0f;
				refBack.x -= Width / 2.0f;
			}
        }

		private void GenerateVerticesAndTris(List<Vector2> points, List<Vector3> generatedVertices, List<int> generatedTriangles, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool calculateWorldPos = true, bool ignoreOrientation = false)
		{
			Vector3 firstPoint = (Vector3)points[0];
			Vector3 
				firstFront = Vector3.zero, 
				firstBack = Vector3.zero, 
				secondPoint = Vector3.zero, 
				secondFront = Vector3.zero, 
				secondBack = Vector3.zero;

            CreateFrontBackPoints(firstPoint,out firstFront,out firstBack,zDepth,colliderWidth,calculateWorldPos, ignoreOrientation);

			if (innerCollision)
			{
				generatedVertices.Add(firstBack); // 3
				generatedVertices.Add(firstFront); // 2
			}
			else
			{
				generatedVertices.Add(firstFront); // 3
				generatedVertices.Add(firstBack); // 2
			}

			// Calculate line planes
			for (int i = 1; i < points.Count; i++)
			{
                secondPoint = (Vector3)points[i];
				CreateFrontBackPoints(secondPoint,out secondFront,out secondBack,zDepth,colliderWidth,calculateWorldPos, ignoreOrientation);

				if (innerCollision)
				{
					generatedVertices.Add(secondBack); // 1
					generatedVertices.Add(secondFront); // 0
				}
				else
				{
					generatedVertices.Add(secondFront); // 1
					generatedVertices.Add(secondBack); // 0
				}

				generatedTriangles.Add((i - 1) * 2 + 3);
				generatedTriangles.Add((i - 1) * 2 + 2);
                generatedTriangles.Add((i - 1) * 2 + 0);
                
                generatedTriangles.Add((i - 1) * 2 + 0);
				generatedTriangles.Add((i - 1) * 2 + 1);
                generatedTriangles.Add((i - 1) * 2 + 3);
                
                firstPoint = secondPoint;
				firstFront = secondFront;
				firstBack = secondBack;
            }
        }

		private void FillFaces(List<Vector2> points, List<int> generatedTriangles)
		{
			FillFaces(points.ToArray(), generatedTriangles);
		}

		private void FillFaces(Vector2[] points, List<int> generatedTriangles)
		{
			// First we pass to the algorithm the object points
			Triangulator tr = new Triangulator(points);
			int[] indices = tr.Triangulate();
			// now, indices[] contains the vertices in a triangulated order, but the mesh has 2 vertices per indice[] (front and back)
			// so we must iterate this list and add front and back triangles accordingly
			// we get each triangle from indices[] and add to triangles list the corrected indices based on vertices list
			for (int i = 0; i < indices.Length; i += 3)
			{
				generatedTriangles.Add(indices[i + 2] * 2);
				generatedTriangles.Add(indices[i + 1] * 2);
				generatedTriangles.Add(indices[i] * 2);

				generatedTriangles.Add(indices[i] * 2 + 1);
				generatedTriangles.Add(indices[i + 1] * 2 + 1);
				generatedTriangles.Add(indices[i + 2] * 2 + 1);
			}
		}
		#endregion

		#region Polyline Colliders
		private void AddPolylineCollider3D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			Mesh colliderMesh = new Mesh();
			colliderMesh.name = "Collider_" + obj.Name;
			MeshCollider mc = gameObject.AddComponent<MeshCollider>();

			mc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();

			GenerateVerticesAndTris(obj.Points, vertices, triangles, zDepth, colliderWidth, innerCollision);

			colliderMesh.vertices = vertices.ToArray();
			colliderMesh.uv = new Vector2[colliderMesh.vertices.Length];
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.triangles = triangles.ToArray();
			colliderMesh.RecalculateNormals();

			mc.sharedMesh = colliderMesh;

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody>();
				gameObject.GetComponent<Rigidbody>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			if (obj.GetPropertyAsBoolean(Property_CreateMesh))
			{
				if (gameObject.GetComponent<MeshFilter>() == null) 
					gameObject.AddComponent<MeshFilter>();

				if (gameObject.GetComponent<MeshRenderer>() == null) 
					gameObject.AddComponent<MeshRenderer>();

				MeshFilter _meshFilter = gameObject.GetComponent<MeshFilter>();
				if (mc != null)
				{
					mc.sharedMesh.RecalculateBounds();
					mc.sharedMesh.RecalculateNormals();
					MathfExtensions.CalculateMeshTangents(mc.sharedMesh);
					_meshFilter.sharedMesh = mc.sharedMesh;
				}
			}
			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GeneratePolylineCollider3D(MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;

			AddPolylineCollider3D(newCollider, obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);
			return newCollider;
		}

		private void AddPolylineCollider2D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();

			edgeCollider.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			Vector2[] points = obj.Points.ToArray();

			for (int i = 0; i < points.Length; i++)
			{
				points[i] = TiledPositionToWorldPoint(points[i].x, points[i].y);
				if (Orientation == X_UniTMX.Orientation.Isometric)
					points[i].x -= Width / 2.0f;
			}

			edgeCollider.points = points;

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody2D>();
				gameObject.GetComponent<Rigidbody2D>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GeneratePolylineCollider2D(MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);

			AddPolylineCollider2D(newCollider, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}

		/// <summary>
		/// Generate a Polyline collider mesh, or a sequence of EdgeCollider2D for 2D collisions.
		/// </summary>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public GameObject GeneratePolylineCollider(MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			return used2DColider ? 
				GeneratePolylineCollider2D(obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic) : 
				GeneratePolylineCollider3D(obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
		}

		/// <summary>
		/// Adds a Polyline collider mesh, or a sequence of EdgeCollider2D for 2D collisions, to an existing GameObject using one MapObject as properties source
		/// </summary>
		/// <param name="gameObject">GameObject to add a Polyline Collider</param>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		public void AddPolylineCollider(GameObject gameObject, MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			if (used2DColider)
				AddPolylineCollider2D(gameObject, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);
			else
				AddPolylineCollider3D(gameObject, obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
		}
		#endregion

		#region Polygon Colliders
		private void AddPolygonCollider2D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

			polygonCollider.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			Vector2[] points = obj.Points.ToArray();

			for (int i = 0; i < points.Length; i++)
			{
				points[i] = TiledPositionToWorldPoint(points[i].x, points[i].y);
				if (Orientation == X_UniTMX.Orientation.Isometric)
					points[i].x -= Width / 2.0f;
			}

			polygonCollider.SetPath(0, points);

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody2D>();
				gameObject.GetComponent<Rigidbody2D>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);

			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GeneratePolygonCollider2D(MapObject obj, bool isTrigger = false, float zDepth = 0, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);

			AddPolygonCollider2D(newCollider, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}

		private void AddPolygonCollider3D(GameObject gameObject, MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			Mesh colliderMesh = new Mesh();
			colliderMesh.name = "Collider_" + obj.Name;
			MeshCollider mc = gameObject.AddComponent<MeshCollider>();

			mc.isTrigger = isTrigger || obj.Type.Equals(Object_Type_Trigger);

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();

			GenerateVerticesAndTris(obj.Points, vertices, triangles, zDepth, colliderWidth, innerCollision);

			// Connect last point with first point (create the face between them)
			triangles.Add(vertices.Count - 1);
			triangles.Add(1);
			triangles.Add(0);

			triangles.Add(0);
			triangles.Add(vertices.Count - 2);
			triangles.Add(vertices.Count - 1);

			// Fill Faces
			FillFaces(obj.Points, triangles);

			colliderMesh.vertices = vertices.ToArray();
			colliderMesh.uv = new Vector2[colliderMesh.vertices.Length];
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.uv2 = colliderMesh.uv;
			colliderMesh.triangles = triangles.ToArray();
			colliderMesh.RecalculateNormals();

			mc.sharedMesh = colliderMesh;

			if (createRigidbody)
			{
				gameObject.AddComponent<Rigidbody>();
				gameObject.GetComponent<Rigidbody>().isKinematic = rigidbodyIsKinematic;
			}

			if (obj.Rotation != 0)
			{
				gameObject.transform.localRotation = Quaternion.AngleAxis(obj.Rotation, Vector3.forward);
			}

			if (obj.GetPropertyAsBoolean(Property_CreateMesh))
			{
				if (gameObject.GetComponent<MeshFilter>() == null)
					gameObject.AddComponent<MeshFilter>();
				
				if (gameObject.GetComponent<MeshRenderer>() == null)
					gameObject.AddComponent<MeshRenderer>();
				
				MeshFilter _meshFilter = gameObject.GetComponent<MeshFilter>();
				if (mc != null)
				{
					mc.sharedMesh.RecalculateBounds();
					mc.sharedMesh.RecalculateNormals();
					MathfExtensions.CalculateMeshTangents(mc.sharedMesh);
					_meshFilter.sharedMesh = mc.sharedMesh;
				}
			}

			ApplyCustomProperties(gameObject, obj);
		}

		private GameObject GeneratePolygonCollider3D(MapObject obj, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject newCollider = new GameObject(obj.Name);
			newCollider.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
			newCollider.transform.localPosition = TiledPositionToWorldPoint(obj.Bounds.x, obj.Bounds.y, zDepth);

			AddPolygonCollider3D(newCollider, obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);

			newCollider.isStatic = true;
			newCollider.SetActive(obj.Visible);

			return newCollider;
		}
		
		/// <summary>
		/// Generate a Polygon collider mesh, or a PolygonCollider2D for 2D collisions.
		/// </summary>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public GameObject GeneratePolygonCollider(MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			return used2DColider ? 
				GeneratePolygonCollider2D(obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic) : 
				GeneratePolygonCollider3D(obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
		}

		/// <summary>
		/// Adds a Polygon collider mesh, or a PolygonCollider2D for 2D collisions, to an existing GameObject using one MapObject as properties source
		/// </summary>
		/// <param name="gameObject">GameObject to add a Polygon Collider</param>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">True for Trigger Collider, false otherwise</param>
		/// <param name="zDepth">Z Depth of the collider.</param>
		/// <param name="colliderWidth">Width of the collider, in Units</param>
		/// <param name="used2DColider">True to generate a 2D collider, false to generate a 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		public void AddPolygonCollider(GameObject gameObject, MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1.0f, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			if (used2DColider)
				AddPolygonCollider2D(gameObject, obj, isTrigger, zDepth, createRigidbody, rigidbodyIsKinematic);
			else
				AddPolygonCollider3D(gameObject, obj, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
		}
		#endregion

		/// <summary>
		/// Generate a collider based on object type
		/// </summary>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">true to generate Trigger collider</param>
		/// <param name="used2DColider">True to generate 2D colliders, otherwise 3D colliders will be generated.</param>
		/// <param name="zDepth">Z Depth of the 3D collider.</param>
		/// <param name="colliderWidth">>Width of the 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public GameObject GenerateCollider(MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			GameObject col = null;

			switch (obj.ObjectType)
			{
				case ObjectType.Box:
					col = GenerateBoxCollider(obj, used2DColider, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Ellipse:
					col = GenerateEllipseCollider(obj, used2DColider, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Polygon:
					col = GeneratePolygonCollider(obj, used2DColider, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Polyline:
					col = GeneratePolylineCollider(obj, used2DColider, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
					break;
			}

			return col;
		}

		/// <summary>
		/// Adds a collider to an existing GameObject based on obj type.
		/// </summary>
		/// <param name="gameObject">GameObject to add a collider</param>
		/// <param name="obj">MapObject which properties will be used to generate this collider.</param>
		/// <param name="isTrigger">true to generate Trigger collider</param>
		/// <param name="used2DColider">True to generate 2D colliders, otherwise 3D colliders will be generated.</param>
		/// <param name="zDepth">Z Depth of the 3D collider.</param>
		/// <param name="colliderWidth">>Width of the 3D collider.</param>
		/// <param name="innerCollision">If true, calculate normals facing the anchor of the collider (inside collisions), else, outside collisions.</param>
		/// <param name="createRigidbody">True to attach a Rigidbody to the created collider</param>
		/// <param name="rigidbodyIsKinematic">Sets if the attached rigidbody is kinematic or not</param>
		public void AddCollider(GameObject gameObject, MapObject obj, bool used2DColider = true, bool isTrigger = false, float zDepth = 0, float colliderWidth = 1, bool innerCollision = false, bool createRigidbody = false, bool rigidbodyIsKinematic = true)
		{
			switch (obj.ObjectType)
			{
				case ObjectType.Box:
					AddBoxCollider(gameObject, obj, used2DColider, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Ellipse:
					AddEllipseCollider(gameObject, obj, used2DColider, isTrigger, zDepth, colliderWidth, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Polygon:
					AddPolygonCollider(gameObject, obj, used2DColider, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
					break;
				case ObjectType.Polyline:
					AddPolylineCollider(gameObject, obj, used2DColider, isTrigger, zDepth, colliderWidth, innerCollision, createRigidbody, rigidbodyIsKinematic);
					break;
			}
		}

		/// <summary>
		/// Generates Colliders from an MapObject Layer. Every Object in it will generate a GameObject with a Collider.
		/// </summary>
		/// <param name="objectLayerName">MapObject Layer's name</param>
		/// <param name="collidersAreTrigger">true to generate Trigger colliders, false otherwhise.</param>
		/// <param name="is2DCollider">true to generate 2D colliders, false for 3D colliders</param>
		/// <param name="collidersZDepth">Z position of the colliders</param>
		/// <param name="collidersWidth">Width for 3D colliders</param>
		/// <param name="collidersAreInner">true to generate inner collisions for 3D colliders</param>
		/// <returns></returns>
		public GameObject[] GenerateCollidersFromLayer(string objectLayerName, bool is2DCollider = true, bool collidersAreTrigger = false, float collidersZDepth = 0, float collidersWidth = 1, bool collidersAreInner = false)
		{
			List<GameObject> generatedGameObjects = new List<GameObject>();

			MapObjectLayer collisionLayer = GetObjectLayer(objectLayerName);
			if (collisionLayer != null)
			{
				List<MapObject> colliders = collisionLayer.Objects;
				foreach (MapObject colliderObjMap in colliders)
				{
					GameObject newColliderObject = null;
					if (colliderObjMap.Type.Equals(Object_Type_NoCollider) == false)
					{
						newColliderObject = GenerateCollider(colliderObjMap, is2DCollider, collidersAreTrigger, collidersZDepth, collidersWidth, collidersAreInner);
					}

					AddPrefabs(colliderObjMap, newColliderObject, is2DCollider);

					// if this colider has transfer, so delete this colider
					//if (colliderObjMap.GetPropertyAsBoolean("destroy collider after instantiate prefabs"))
					//{
					//	GameObject.Destroy(newColliderObject.gameObject);
					//}
					if(newColliderObject) generatedGameObjects.Add(newColliderObject);
				}
			}
			else
			{
				Debug.LogWarning("There's no Layer \"" + objectLayerName + "\" in tile map.");
			}

			return generatedGameObjects.ToArray();
		}
		#endregion

		#region Prefabs Generation
		/// <summary>
		/// Generate a prefab based in object colider layer
		/// </summary>
		/// <param name="obj">Object which properties will be used to generate a prefab.</param>
		/// <param name="newColliderObject">if null add relative parent object,.</param>
		/// <param name="addTileName">true to add Map's name to the prefab name</param>
		/// <param name="is2DColliders">true to generate 2D colliders</param>
		/// <returns>Generated Game Object containing the Collider.</returns>
		public void AddPrefabs(MapObject obj, GameObject newColliderObject = null, bool is2DColliders = false, bool addTileName = true)
		{
			int indexPrefab = 0;
			while (obj.HasProperty(string.Concat(indexPrefab.ToString(), Property_PrefabName)))
			{
				string prefabName = obj.GetPropertyAsString(indexPrefab + Property_PrefabName);
				string baseResourcePath = obj.GetPropertyAsString(indexPrefab + Property_PrefabPath);
				UnityEngine.Object resourceObject = Resources.Load(baseResourcePath + prefabName);
				Resources.UnloadUnusedAssets();
				if (resourceObject != null)
				{
					float zDepth = obj.GetPropertyAsFloat(indexPrefab + Property_PrefabZDepth);
					GameObject newPrefab = UnityEngine.Object.Instantiate(resourceObject) as GameObject;

					newPrefab.transform.parent = obj.ParentObjectLayer != null ? obj.ParentObjectLayer.LayerGameObject.transform : MapObject.transform;
					newPrefab.transform.localPosition = TiledPositionToWorldPoint(new Vector3(obj.Bounds.center.x, obj.Bounds.center.y, zDepth));

					// copy coliders from newColliderObject
					// only copy if type of this object is diferent of "NoCollider"
					if (obj.Type.Equals(Object_Type_NoCollider) == false)
					{
						if (obj.GetPropertyAsBoolean(indexPrefab + Property_PrefabAddCollider))
						{
							//CopyCollider(obj, ref newColliderObject, ref newPrefab, is2DColliders);
							AddCollider(newPrefab, obj, obj.Type.Equals(Object_Type_Trigger), is2DColliders, zDepth);
						}
						else
							// since custom properties are automatically added when a collider is added but this prefab has no collider, we must enforce them to be parsed
							ApplyCustomProperties(newPrefab, obj);

					}
					else
						// since custom properties are automatically added when a collider is added but this prefab has no collider, we must enforce them to be parsed
						ApplyCustomProperties(newPrefab, obj);

					if (obj.GetPropertyAsBoolean(indexPrefab + Property_PrefabFixColliderPosition))
					{
						// Mario: Fixed wrong position in instantiate prefabs
						if(newColliderObject != null && newPrefab != null)
							newPrefab.transform.position = newColliderObject.transform.position;
					}

					newPrefab.name = (addTileName ? (_mapName + "_") : "") + obj.Name;
					int indexMessage = 1;
					string prefabMensage = obj.GetPropertyAsString(indexPrefab + Property_PrefabSendMessage + indexMessage);
					while (string.IsNullOrEmpty(prefabMensage) == false)
					{
						string[] menssage = prefabMensage.Split(new[] { '|' }, StringSplitOptions.None);
						if (menssage.Length == 2)
						{
							newPrefab.BroadcastMessage(menssage[0], menssage[1]);
						}
						if (menssage.Length == 1)
						{
							newPrefab.BroadcastMessage(menssage[0]);
						}
						indexMessage++;
						prefabMensage = obj.GetPropertyAsString(indexPrefab + Property_PrefabSendMessage + indexMessage);
					}

				}
				else
				{
					Debug.LogError("Prefab doesn't exist at: Resources/" + baseResourcePath + prefabName);
				}
				indexPrefab++;
			}
		}
		#endregion

		#region Parse and Apply MapObjects X-UniTMX Properties
		/// <summary>
		/// Applies to gameObject any custom X-UniTMX properties present on obj
		/// </summary>
		/// <param name="gameObject">GameObject to apply custom properties to</param>
		/// <param name="obj">MapObject to read custom properties from</param>
		public void ApplyCustomProperties(GameObject gameObject, MapObject obj)
		{
			// nothing to do here...
			if (gameObject == null || obj == null)
				return;

			// Set a layer number for gameObject
			if (obj.HasProperty(Property_Layer))
				gameObject.layer = obj.GetPropertyAsInt(Property_Layer);

			if (obj.HasProperty(Property_LayerName))
				gameObject.layer = LayerMask.NameToLayer(obj.GetPropertyAsString(Property_LayerName));

			// Add a tag for gameObject
			if (obj.HasProperty(Property_Tag))
				gameObject.tag = obj.GetPropertyAsString(Property_Tag);
			// Add Components for this gameObject
			int c = 1;
			while (obj.HasProperty(Property_AddComponent + c))
			{
				UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(gameObject, "Assets/X-UniTMX/Code/Map.cs (2684,5)", obj.GetPropertyAsString(Property_AddComponent + c));
				c++;
			}
			c = 1;
			while (obj.HasProperty(Property_SendMessage + c))
			{
				string messageToSend = obj.GetPropertyAsString(Property_SendMessage + c);
				string[] menssage = messageToSend.Split('|');
				if (menssage.Length == 2)
				{
					gameObject.BroadcastMessage(menssage[0], menssage[1]);
				}
				if (menssage.Length == 1)
				{
					gameObject.BroadcastMessage(menssage[0]);
				}
				c++;
			}

			if (gameObject.GetComponent<Renderer>() != null)
			{
				if (obj.HasProperty(Property_SortingLayerName))
					gameObject.GetComponent<Renderer>().sortingLayerName = obj.GetPropertyAsString(Property_SortingLayerName);

				if (obj.HasProperty(Property_SortingOrder))
					gameObject.GetComponent<Renderer>().sortingOrder = obj.GetPropertyAsInt(Property_SortingOrder);

				if (obj.HasProperty(Property_SetMaterialColor))
				{
					string[] splitColor = obj.GetPropertyAsString(Property_SetMaterialColor).Split(',');
					if (splitColor.Length >= 1)
					{
						gameObject.GetComponent<Renderer>().material = new Material(BaseTileMaterial);
						gameObject.GetComponent<Renderer>().material.color = new Color32(
							((byte)(int.Parse(string.IsNullOrEmpty(splitColor[0]) ? "255" : splitColor[0]))),
							splitColor.Length >= 2 ? ((byte)(int.Parse(splitColor[1]))) : (byte)255,
							splitColor.Length >= 3 ? ((byte)(int.Parse(splitColor[2]))) : (byte)255,
							splitColor.Length >= 4 ? ((byte)(int.Parse(splitColor[3]))) : (byte)255);
					}
				}
			}
		}
		#endregion

		public override string ToString()
		{
			return string.Concat(
				"Map Size (", Width.ToString(), ", ", Height.ToString(), ")",
				"\nTile Size (", TileWidth.ToString(), ", ", TileHeight.ToString(), ")",
				"\nOrientation: ", Orientation.ToString(),
				"\nTiled Version: ", Version.ToString());
		}
	}
}
