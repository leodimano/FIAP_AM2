/*!
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013 Guilherme "Chaoseiro" Maia
 * Released under the MIT license
 * Check LICENSE.MIT for more details.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using X_UniTMX;
using UnityEditor;
using TObject.Shared;
using UnityEngine;
using System.Reflection;

namespace X_UniTMX
{
	[CustomEditor (typeof(TiledMapComponent))]
	//[CanEditMultipleObjects] many errors where occurring when multiple editing components, decided to deactivate this function
	public class TiledMapComponentEditor : Editor
	{
		#region Menu GameObject
		[MenuItem ("GameObject/Create Other/Tiled Game Map")]
		static void CreateGameObject () {
			GameObject obj = new GameObject("Tiled Game Map");
			obj.AddComponent<TiledMapComponent>();
			Undo.RegisterCreatedObjectUndo(obj, "Created Tiled Game Map");
		}
		#endregion

		#region Icon Textures
		public static Texture2D imageIcon;
		public static Texture2D objectIcon;
		public static Texture2D layerIcon;
		public static Texture2D componentIcon;
		public static Texture2D objectTypeIcon_Box;
		public static Texture2D objectTypeIcon_Ellipse;
		public static Texture2D objectTypeIcon_Polyline;
		public static Texture2D objectTypeIcon_Polygon;
		#endregion

		private static bool _changedMap = false;

		private static TiledMapComponent _tiledMapComponent;

		#region Serialized Properties
		SerializedProperty materialDefaultFile;
		SerializedProperty DefaultSortingOrder;
		SerializedProperty isToLoadOnStart;

		SerializedProperty tileObjectElipsePrecision;
		SerializedProperty simpleTileObjectCalculation;
		SerializedProperty clipperArcTolerance;
		SerializedProperty clipperMiterLimit;
		SerializedProperty clipperJoinType;
		SerializedProperty clipperEndType;
		SerializedProperty clipperDeltaOffset;

		SerializedProperty GenerateTileCollisions;
		SerializedProperty foldoutTileCollisions;
		SerializedProperty TileCollisionsZDepth;
		SerializedProperty TileCollisionsWidth;
		SerializedProperty TileCollisionsIsInner;
		SerializedProperty TileCollisionsIsTrigger;
		SerializedProperty TileCollisionsIs2D;
		#endregion

		void OnEnable()
		{
			_tiledMapComponent = (TiledMapComponent)target;

			DirectoryInfo rootDir = new DirectoryInfo(Application.dataPath);
			FileInfo[] files = rootDir.GetFiles("TiledMapComponentEditor.cs", SearchOption.AllDirectories);
			string editorIconPath = Path.GetDirectoryName(files[0].FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets"));
			editorIconPath = editorIconPath + "/Icons";
			imageIcon = (Texture2D)AssetDatabase.LoadMainAssetAtPath( editorIconPath + "/layer-image.png");
			objectIcon = (Texture2D)AssetDatabase.LoadMainAssetAtPath( editorIconPath + "/layer-object.png");
			layerIcon = (Texture2D)AssetDatabase.LoadMainAssetAtPath( editorIconPath + "/layer-tile.png");
			componentIcon = (Texture2D)AssetDatabase.LoadMainAssetAtPath( editorIconPath + "/TiledMapComponent Icon.png");
			objectTypeIcon_Box = (Texture2D)AssetDatabase.LoadMainAssetAtPath(editorIconPath + "/insert-rectangle.png");
			objectTypeIcon_Ellipse = (Texture2D)AssetDatabase.LoadMainAssetAtPath(editorIconPath + "/insert-ellipse.png");
			objectTypeIcon_Polyline = (Texture2D)AssetDatabase.LoadMainAssetAtPath(editorIconPath + "/insert-polyline.png");
			objectTypeIcon_Polygon = (Texture2D)AssetDatabase.LoadMainAssetAtPath(editorIconPath + "/insert-polygon.png");

			// Serializable properties setup
			materialDefaultFile = serializedObject.FindProperty("materialDefaultFile");
			DefaultSortingOrder = serializedObject.FindProperty("DefaultSortingOrder");
			isToLoadOnStart = serializedObject.FindProperty("isToLoadOnStart");

			tileObjectElipsePrecision = serializedObject.FindProperty("tileObjectElipsePrecision");
			simpleTileObjectCalculation = serializedObject.FindProperty("simpleTileObjectCalculation");
			clipperArcTolerance = serializedObject.FindProperty("clipperArcTolerance");
			clipperMiterLimit = serializedObject.FindProperty("clipperMiterLimit");
			clipperJoinType = serializedObject.FindProperty("clipperJoinType");
			clipperEndType = serializedObject.FindProperty("clipperEndType");
			clipperDeltaOffset = serializedObject.FindProperty("clipperDeltaOffset");

			GenerateTileCollisions = serializedObject.FindProperty("GenerateTileCollisions");
			foldoutTileCollisions = serializedObject.FindProperty("foldoutTileCollisions");
			TileCollisionsZDepth = serializedObject.FindProperty("TileCollisionsZDepth");
			TileCollisionsWidth = serializedObject.FindProperty("TileCollisionsWidth");
			TileCollisionsIsInner = serializedObject.FindProperty("TileCollisionsIsInner");
			TileCollisionsIsTrigger = serializedObject.FindProperty("TileCollisionsIsTrigger");
			TileCollisionsIs2D = serializedObject.FindProperty("TileCollisionsIs2D");
		}

		private void ClearCurrentmap()
		{
			// Destroy any previous map entities
			var children = new List<GameObject>();
			foreach (Transform child in _tiledMapComponent.transform)
				children.Add(child.gameObject);
			children.ForEach(child => Undo.DestroyObjectImmediate(child));
			MeshFilter filter = _tiledMapComponent.GetComponent<MeshFilter>();
			if (filter)
				Undo.DestroyObjectImmediate(filter);
		}

		private void DoImportMapButtonGUI()
		{
			if (GUILayout.Button("Import as static Tile Map"))
			{
				ClearCurrentmap();
				
				if (_tiledMapComponent.Initialize())
				{
					Debug.Log("Map sucessfull loaded!");
				}
			}
		}

		private void ReadPropertiesAndVariables()
		{
			if (_tiledMapComponent.tileLayers != null && _tiledMapComponent.MakeUniqueTiles != null &&
				_tiledMapComponent.tileLayers.Length > _tiledMapComponent.MakeUniqueTiles.Length)
				_changedMap = true;
			if (_changedMap ||
				_tiledMapComponent.mapProperties == null ||
				_tiledMapComponent.objectLayerNodes == null ||
				_tiledMapComponent.tileLayersProperties == null ||
				_tiledMapComponent.objectLayersProperties == null ||
				_tiledMapComponent.imageLayersProperties == null ||
				_tiledMapComponent.objectLayers == null ||
				_tiledMapComponent.generateCollider == null ||
				_tiledMapComponent.collidersIs2D == null ||
				_tiledMapComponent.collidersWidth == null ||
				_tiledMapComponent.collidersZDepth == null ||
				_tiledMapComponent.collidersIsInner == null ||
				_tiledMapComponent.collidersIsTrigger == null ||
				_tiledMapComponent.tileLayers == null ||
				_tiledMapComponent.imageLayers == null ||
				_tiledMapComponent.tileLayersFoldoutProperties == null ||
				_tiledMapComponent.objectLayersFoldoutProperties == null ||
				_tiledMapComponent.imageLayersFoldoutProperties == null ||
				_tiledMapComponent.MakeUniqueTiles == null
				){
				NanoXMLDocument document = new NanoXMLDocument(_tiledMapComponent.MapTMX.text);
				NanoXMLNode mapNode = document.RootNode;
				List<Property> mapProperties = new List<Property>();
				List<NanoXMLNode> objectLayerNodes = new List<NanoXMLNode>();
				List<string> objectLayers = new List<string>();
				List<bool> generateCollider = new List<bool>();
				List<bool> collidersIs2D = new List<bool>();
				List<float> collidersWidth = new List<float>();
				List<float> collidersZDepth = new List<float>();
				List<bool> collidersIsInner = new List<bool>();
				List<bool> collidersIsTrigger = new List<bool>();
				List<string> tileLayers = new List<string>();
				List<string> imageLayers = new List<string>();

				List<bool> makeUniqueTiles = new List<bool>();
				
				List<bool> tileLayersFoldoutProperties = new List<bool>();
				List<bool> objectLayersFoldoutProperties = new List<bool>();
				List<bool> imageLayersFoldoutProperties = new List<bool>();

				Dictionary<int, List<Property>> tileLayersProperties = new Dictionary<int,List<Property>>();
				Dictionary<int, List<Property>> objectLayersProperties = new Dictionary<int,List<Property>>();
				Dictionary<int, List<Property>> imageLayersProperties = new Dictionary<int, List<Property>>();
				foreach (NanoXMLNode layerNode in mapNode.SubNodes)
				{
					if (layerNode.Name.Equals("properties"))
					{
						foreach (var property in layerNode.SubNodes)
						{
							mapProperties.Add(
								new Property(
									property.GetAttribute("name").Value,
									property.GetAttribute("value").Value
								)
							);
						}
						
					}
					if (layerNode.Name.Equals("objectgroup"))
					{
						objectLayerNodes.Add(layerNode);
						objectLayers.Add(layerNode.GetAttribute("name").Value);
						generateCollider.Add(false);
						collidersIs2D.Add(true);
						collidersWidth.Add(1);
						collidersZDepth.Add(0);
						collidersIsInner.Add(false);
						collidersIsTrigger.Add(false);
						// properties
						objectLayersFoldoutProperties.Add(false);
						objectLayersProperties.Add(objectLayerNodes.Count - 1, new List<Property>());
						foreach (var subNodes in layerNode.SubNodes)
						{
							if (subNodes.Name.Equals("properties"))
							{
								foreach (var property in subNodes.SubNodes)
								{
									objectLayersProperties[objectLayerNodes.Count - 1].Add(
										new Property(
											property.GetAttribute("name").Value,
											property.GetAttribute("value").Value
										)
									);
								}
							}
						}
					}
					if (layerNode.Name.Equals("layer"))
					{
						tileLayers.Add(layerNode.GetAttribute("name").Value);
						// Make Unique Tiles
						makeUniqueTiles.Add(false);
						// properties
						tileLayersFoldoutProperties.Add(false);
						tileLayersProperties.Add(tileLayers.Count - 1, new List<Property>());
						foreach (var subNodes in layerNode.SubNodes)
						{
							if (subNodes.Name.Equals("properties"))
							{
								foreach (var property in subNodes.SubNodes)
								{
									tileLayersProperties[tileLayers.Count - 1].Add(
										new Property(
											property.GetAttribute("name").Value,
											property.GetAttribute("value").Value
										)
									);
								}
							}
						}
					}
					if (layerNode.Name.Equals("imagelayer"))
					{
						imageLayers.Add(layerNode.GetAttribute("name").Value);
						// properties
						imageLayersFoldoutProperties.Add(false);
						imageLayersProperties.Add(imageLayers.Count - 1, new List<Property>());
						foreach (var subNodes in layerNode.SubNodes)
						{
							if (subNodes.Name.Equals("properties"))
							{
								foreach (var property in subNodes.SubNodes)
								{
									imageLayersProperties[imageLayers.Count - 1].Add(
										new Property(
											property.GetAttribute("name").Value,
											property.GetAttribute("value").Value
										)
									);
								}
							}
						}
					}
				}
				if (_changedMap || _tiledMapComponent.mapProperties == null) _tiledMapComponent.mapProperties = mapProperties.ToArray();
				if (_changedMap || _tiledMapComponent.objectLayerNodes == null) _tiledMapComponent.objectLayerNodes = objectLayerNodes.ToArray();
				if (_changedMap || _tiledMapComponent.tileLayersProperties == null) _tiledMapComponent.tileLayersProperties = new Dictionary<int,List<Property>>(tileLayersProperties);
				if (_changedMap || _tiledMapComponent.objectLayersProperties == null) _tiledMapComponent.objectLayersProperties = objectLayersProperties;
				if (_changedMap || _tiledMapComponent.imageLayersProperties == null) _tiledMapComponent.imageLayersProperties = imageLayersProperties;
				if (_changedMap || _tiledMapComponent.objectLayers == null) _tiledMapComponent.objectLayers = objectLayers.ToArray();
				if (_changedMap || _tiledMapComponent.generateCollider == null) _tiledMapComponent.generateCollider = generateCollider.ToArray();
				if (_changedMap || _tiledMapComponent.collidersIs2D == null) _tiledMapComponent.collidersIs2D = collidersIs2D.ToArray();
				if (_changedMap || _tiledMapComponent.collidersWidth == null) _tiledMapComponent.collidersWidth = collidersWidth.ToArray();
				if (_changedMap || _tiledMapComponent.collidersZDepth == null) _tiledMapComponent.collidersZDepth = collidersZDepth.ToArray();
				if (_changedMap || _tiledMapComponent.collidersIsInner == null) _tiledMapComponent.collidersIsInner = collidersIsInner.ToArray();
				if (_changedMap || _tiledMapComponent.collidersIsTrigger == null) _tiledMapComponent.collidersIsTrigger = collidersIsTrigger.ToArray();
				if (_changedMap || _tiledMapComponent.tileLayers == null) _tiledMapComponent.tileLayers = tileLayers.ToArray();
				if (_changedMap || _tiledMapComponent.imageLayers == null) _tiledMapComponent.imageLayers = imageLayers.ToArray();
				if (_changedMap || _tiledMapComponent.tileLayersFoldoutProperties == null) _tiledMapComponent.tileLayersFoldoutProperties = tileLayersFoldoutProperties.ToArray();
				if (_changedMap || _tiledMapComponent.objectLayersFoldoutProperties == null) _tiledMapComponent.objectLayersFoldoutProperties = objectLayersFoldoutProperties.ToArray();
				if (_changedMap || _tiledMapComponent.imageLayersFoldoutProperties == null) _tiledMapComponent.imageLayersFoldoutProperties = imageLayersFoldoutProperties.ToArray();

				if (_changedMap || _tiledMapComponent.MakeUniqueTiles == null) _tiledMapComponent.MakeUniqueTiles = makeUniqueTiles.ToArray();


				if(_changedMap) _changedMap = false;
			}
		}

		private void DoLayersGUI()
		{
			ReadPropertiesAndVariables();

			EditorGUIUtility.labelWidth = 250;
			_tiledMapComponent.foldoutLayers = EditorGUILayout.Foldout(_tiledMapComponent.foldoutLayers, new GUIContent("Map Layers",componentIcon));
			if (_tiledMapComponent.foldoutLayers)
			{
				EditorGUI.indentLevel++;
				DoMapPropertiesGUI();
				DoTileLayersGUI();
				DoObjectLayersGUI();
				DoImageLayersGUI();
				EditorGUI.indentLevel--;
			}
		}

		void DoMapPropertiesGUI()
		{
			if (_tiledMapComponent.mapProperties == null || _tiledMapComponent.mapProperties.Length < 1)
				return;

			_tiledMapComponent.foldoutMapProperties = EditorGUILayout.Foldout(_tiledMapComponent.foldoutMapProperties, new GUIContent("Map Properties") );
			EditorGUI.indentLevel++;
			if (_tiledMapComponent.foldoutMapProperties)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MaxWidth(150.0f));
				EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.MaxWidth(150.0f));
				EditorGUILayout.EndHorizontal();
				foreach (var property in _tiledMapComponent.mapProperties)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.SelectableLabel(property.Name, GUILayout.MaxHeight(20));
					EditorGUILayout.SelectableLabel(property.RawValue, GUILayout.MaxHeight(20));
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUI.indentLevel--;
		}

		void DoObjectLayersGUI()
		{
			if (_tiledMapComponent.objectLayers == null || _tiledMapComponent.objectLayers.Length < 1)
				return;
			_tiledMapComponent.foldoutObjectLayers = EditorGUILayout.Foldout(_tiledMapComponent.foldoutObjectLayers,new GUIContent("Object Layers",objectIcon) );
			EditorGUI.indentLevel++;
			if (_tiledMapComponent.foldoutObjectLayers)
			{
				_tiledMapComponent.addTileNameToColliderName = EditorGUILayout.Toggle("Add tile name to collider name?", _tiledMapComponent.addTileNameToColliderName);
				for (int i = 0; i < _tiledMapComponent.objectLayers.Length; i++)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.BeginHorizontal();
					EditorGUIUtility.labelWidth = 70;
					EditorGUILayout.LabelField(new GUIContent(_tiledMapComponent.objectLayers[i],objectIcon), GUILayout.ExpandWidth(true));
					if (GUILayout.Button("View Objects", GUILayout.ExpandWidth(true)))
						ShowObjectsWindow(_tiledMapComponent.objectLayerNodes[i]);
					EditorGUIUtility.labelWidth = 260;
					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel++;
					Undo.RecordObject(_tiledMapComponent, "Set Tiled Map Component Object Layer Variables");
					_tiledMapComponent.generateCollider[i] = EditorGUILayout.BeginToggleGroup("Generate Colliders/Prefabs?", _tiledMapComponent.generateCollider[i]);
					_tiledMapComponent.collidersIs2D[i] = EditorGUILayout.Toggle("Create 2D colliders for this layer?", _tiledMapComponent.collidersIs2D[i]);
					_tiledMapComponent.collidersIsTrigger[i] = EditorGUILayout.Toggle("Set this layer as a trigger layer?", _tiledMapComponent.collidersIsTrigger[i]);
					if (!_tiledMapComponent.collidersIs2D[i])
					{
						EditorGUILayout.LabelField("For 3D configuration:");
						_tiledMapComponent.collidersWidth[i] = EditorGUILayout.FloatField("This layer width", _tiledMapComponent.collidersWidth[i]);
						_tiledMapComponent.collidersZDepth[i] = EditorGUILayout.FloatField("This layer Z depth", _tiledMapComponent.collidersZDepth[i]);
						_tiledMapComponent.collidersIsInner[i] = EditorGUILayout.Toggle("Set this layer with inner collisions?", _tiledMapComponent.collidersIsInner[i]);
					}
					EditorGUILayout.EndToggleGroup();
					EditorGUI.indentLevel--;
					DoObjectLayerPropertyGUI(_tiledMapComponent, i);
					EditorGUI.indentLevel--;
					EditorGUILayout.Space();
				}
			}
			EditorGUI.indentLevel--;
		}

		void DoObjectLayerPropertyGUI(TiledMapComponent _tiledMapComponent, int layerNumber)
		{
			DoPropertiesGUI(_tiledMapComponent, layerNumber, _tiledMapComponent.objectLayersProperties, _tiledMapComponent.objectLayersFoldoutProperties);
		}

		void DoTileLayersGUI()
		{
			if (_tiledMapComponent.tileLayers == null || _tiledMapComponent.tileLayers.Length < 1)
				return;
			_tiledMapComponent.foldoutObjectsInLayer = EditorGUILayout.Foldout(_tiledMapComponent.foldoutObjectsInLayer, new GUIContent("Tile Layers",layerIcon));
			EditorGUI.indentLevel++;
			if (_tiledMapComponent.foldoutObjectsInLayer)
			{
				for (int i = 0; i < _tiledMapComponent.tileLayers.Length; i++)
				{
					EditorGUILayout.LabelField(new GUIContent(_tiledMapComponent.tileLayers[i],layerIcon), GUILayout.Height(20));
					Undo.RecordObject(_tiledMapComponent, "Set Tiled Map Component Tile Layer Variables");
					EditorGUI.indentLevel++;
					if (i < _tiledMapComponent.MakeUniqueTiles.Length)
						_tiledMapComponent.MakeUniqueTiles[i] = EditorGUILayout.ToggleLeft("Make Unique Tiles?", _tiledMapComponent.MakeUniqueTiles[i]);
					EditorGUI.indentLevel--;
					DoTileLayerPropertyGUI(_tiledMapComponent, i);
				}
			}
			EditorGUI.indentLevel--;
		}

		void DoTileLayerPropertyGUI(TiledMapComponent _tiledMapComponent, int layerNumber)
		{
			DoPropertiesGUI(_tiledMapComponent, layerNumber, _tiledMapComponent.tileLayersProperties, _tiledMapComponent.tileLayersFoldoutProperties);
		}

		void DoImageLayersGUI()
		{
			if (_tiledMapComponent.imageLayers == null || _tiledMapComponent.imageLayers.Length < 1)
				return;
			_tiledMapComponent.foldoutImageLayers = EditorGUILayout.Foldout(_tiledMapComponent.foldoutImageLayers, new GUIContent("Image Layers",imageIcon));
			EditorGUI.indentLevel++;
			if (_tiledMapComponent.foldoutImageLayers)
			{
			    for (int i = 0; i < _tiledMapComponent.imageLayers.Length; i++)
				{
					EditorGUILayout.LabelField(new GUIContent(_tiledMapComponent.imageLayers[i],imageIcon), GUILayout.Height(20));
					DoImageLayerPropertyGUI(_tiledMapComponent, i);
				}
			}
			EditorGUI.indentLevel--;
		}

		void DoImageLayerPropertyGUI(TiledMapComponent _tiledMapComponent, int layerNumber)
		{
			DoPropertiesGUI(_tiledMapComponent, layerNumber, _tiledMapComponent.imageLayersProperties, _tiledMapComponent.imageLayersFoldoutProperties);
		}

		void DoPropertiesGUI(TiledMapComponent _tiledMapComponent, int layerNumber, Dictionary<int, List<Property>> properties, bool[] foldout)
		{
			if (properties == null || properties[layerNumber] == null || properties[layerNumber].Count < 1)
				return;

			EditorGUI.indentLevel++;
			foldout[layerNumber] = EditorGUILayout.Foldout(foldout[layerNumber], new GUIContent("Properties"));
			if (foldout[layerNumber])
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MaxWidth(150.0f));
				EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.MaxWidth(150.0f));
				EditorGUILayout.EndHorizontal();
				foreach (var property in properties[layerNumber])
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.SelectableLabel(property.Name, GUILayout.MaxHeight(20));
					EditorGUILayout.SelectableLabel(property.RawValue, GUILayout.MaxHeight(20));
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}

		void DoTileCollisionsGUI()
		{
			EditorGUIUtility.labelWidth = 260;
			Undo.RecordObject(_tiledMapComponent, "Set Tiled Map Component Tile Collisions");
			GenerateTileCollisions.boolValue = EditorGUILayout.BeginToggleGroup(new GUIContent("Generate Tile Collisions?",TiledMapComponentEditor.objectIcon), GenerateTileCollisions.boolValue);
			EditorGUI.indentLevel++;
			foldoutTileCollisions.boolValue = EditorGUILayout.Foldout(foldoutTileCollisions.boolValue, new GUIContent("Tile Collisions",TiledMapComponentEditor.objectIcon));
			if (foldoutTileCollisions.boolValue)
			{
				tileObjectElipsePrecision.intValue = EditorGUILayout.IntField(new GUIContent("Tile Objects ellipse precision",TiledMapComponentEditor.objectTypeIcon_Ellipse), tileObjectElipsePrecision.intValue);
				TileCollisionsIs2D.boolValue = EditorGUILayout.Toggle("Create 2D colliders for tile collisions?", TileCollisionsIs2D.boolValue);
				TileCollisionsIsTrigger.boolValue = EditorGUILayout.Toggle("Set tile collisions as a trigger?", TileCollisionsIsTrigger.boolValue);
                
				simpleTileObjectCalculation.boolValue = EditorGUILayout.ToggleLeft("Simple Calculation Tile Objects", simpleTileObjectCalculation.boolValue);
                if(!simpleTileObjectCalculation.boolValue) {
					clipperArcTolerance.floatValue = EditorGUILayout.FloatField("Clipper Arc Tolerance", (float)clipperArcTolerance.floatValue);
					clipperDeltaOffset.floatValue = EditorGUILayout.FloatField("Clipper Delta Offset", (float)clipperDeltaOffset.floatValue);
					EditorGUILayout.PropertyField(clipperEndType, new GUIContent("Clipper Offset End Type"));//(ClipperLib.EndType)EditorGUILayout.EnumPopup("Clipper Offset End Type", (Enum)clipperEndType.enumValueIndex);
					EditorGUILayout.PropertyField(clipperJoinType, new GUIContent("Clipper Offset Join Type"));//clipperJoinType.enumValueIndex = (ClipperLib.JoinType)EditorGUILayout.EnumPopup("Clipper Offset Join Type", (Enum)clipperJoinType.enumValueIndex);
					clipperMiterLimit.floatValue = EditorGUILayout.FloatField("Clipper Miter Limit", (float)clipperMiterLimit.floatValue);
				}

				if (!TileCollisionsIs2D.boolValue)
				{
					EditorGUILayout.LabelField("For 3D configuration:");
					TileCollisionsWidth.floatValue = EditorGUILayout.FloatField("Tile collisions width", TileCollisionsWidth.floatValue);
					TileCollisionsZDepth.floatValue = EditorGUILayout.FloatField("Tile collisions Z depth", TileCollisionsZDepth.floatValue);
					TileCollisionsIsInner.boolValue = EditorGUILayout.Toggle("Set tile collisions with inner collisions?", TileCollisionsIsInner.boolValue);
				}
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.EndToggleGroup();
		}

		void ShowObjectsWindow(NanoXMLNode objectLayerNode)
		{
			TiledMapObjectsWindow.Init(objectLayerNode);
		}

		private void DoClearMapButtonGUI()
		{
			if (GUILayout.Button("Clear Tile Map"))
			{
				ClearCurrentmap();
				Debug.Log("Map cleared!");
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			Undo.RecordObject(_tiledMapComponent, "Set Tiled Map Component Variables");
			EditorGUIUtility.labelWidth = 150;
			//if (!serializedObject.isEditingMultipleObjects)
			//{
			EditorGUI.BeginChangeCheck();
			TextAsset oldRefMapTMX = _tiledMapComponent.MapTMX;
			_tiledMapComponent.MapTMX = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Map xml:", componentIcon), _tiledMapComponent.MapTMX, typeof(TextAsset), true);
			if (EditorGUI.EndChangeCheck())
			{
				if (oldRefMapTMX == null)
				{
					_changedMap = true;
				}
				else
				{
					if (EditorUtility.DisplayDialog("Confirm!", "Some configuration can be lost, ok?", "OK", "Cancel"))
					{
						_changedMap = true;
					}
				}
			}
			//}
			if(_tiledMapComponent.MapTMX != null) {
				// Think a better solution for clean path... AssetDatabase only works inside the Editor
				string[] listPath = AssetDatabase.GetAssetPath(_tiledMapComponent.MapTMX).Split('/');
				if(listPath[1].Equals("Resources") == false)
				{
					EditorGUILayout.HelpBox("Map file must be in Resources folder!", MessageType.Error, true);
					return;
				}
				_tiledMapComponent.MapTMXPath = "";
				for(int i = 2; i < listPath.Length - 1; i++)
				{
					_tiledMapComponent.MapTMXPath += listPath[i] + "/";
				}
				//if (!serializedObject.isEditingMultipleObjects)
				EditorGUILayout.LabelField ("Path Map:", _tiledMapComponent.MapTMXPath);
                
				materialDefaultFile.objectReferenceValue = (Material)EditorGUILayout.ObjectField("Default material tile map", materialDefaultFile.objectReferenceValue, typeof(Material), true);
				if(materialDefaultFile.objectReferenceValue != null)
				{
					if (GUILayout.Button("Reload XML MAP"))
					{
						if(EditorUtility.DisplayDialog("Confirm!", "Some object layer configuration can be lost, ok?","OK","Cancel")) {
							_changedMap = true;
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("Missing default material for map, please select a material!", MessageType.Error, true);
				}
				isToLoadOnStart.boolValue = EditorGUILayout.Toggle("Load this on awake?", isToLoadOnStart.boolValue);
				//MakeUniqueTiles.boolValue = EditorGUILayout.Toggle("Make unique tiles?", MakeUniqueTiles.boolValue);

				DefaultSortingOrder.intValue = EditorGUILayout.IntField("Default Sorting Order", DefaultSortingOrder.intValue);

				DoTileCollisionsGUI();
				//if (!serializedObject.isEditingMultipleObjects)
				DoLayersGUI();
				if (materialDefaultFile.objectReferenceValue != null)
				{
					DoImportMapButtonGUI();
					DoClearMapButtonGUI();
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("Missing map file, please select a XML map!", MessageType.Error, true);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
