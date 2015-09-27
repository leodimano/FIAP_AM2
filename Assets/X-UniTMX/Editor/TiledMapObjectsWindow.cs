using UnityEditor;
using UnityEngine;
using TObject.Shared;
using System.Collections.Generic;
using X_UniTMX.Utils;

namespace X_UniTMX
{
	/// <summary>
	/// A Window that lists all MapObject inside an ObjectLayer, with all theirs properties.
	/// </summary>
	public class TiledMapObjectsWindow : EditorWindow
	{

		static NanoXMLNode _objectLayerNode;
		List<MapObject> _mapObjects;
		List<bool> _objFoldout;
		List<bool> _objPropertiesFoldout;
		bool _mainFoldout = true;
		Vector2 _scrollPos = Vector2.zero;

		/// <summary>
		/// Initialize the Window
		/// </summary>
		/// <param name="objectLayerNode">NanoXMLNode of the MapObjectLayer from with MapObject will be read</param>
		public static void Init(NanoXMLNode objectLayerNode)
		{
			// Get existing open window or if none, make a new one:
			TiledMapObjectsWindow window = (TiledMapObjectsWindow)EditorWindow.GetWindow(typeof(TiledMapObjectsWindow));
			_objectLayerNode = objectLayerNode;
			string name = objectLayerNode.GetAttribute("name") != null ?
				objectLayerNode.GetAttribute("name").Value :
				"ObjectLayer";
			window.title = name;
			window.RebuildObjectsProperties();
		}

		/// <summary>
		/// Re-read MapObject properties if MapObjectLayer has changed.
		/// </summary>
		public void RebuildObjectsProperties()
		{
			if (_objectLayerNode == null)
				return;
			if (_mapObjects == null)
			{
				_mapObjects = new List<MapObject>();
				_objFoldout = new List<bool>();
				_objPropertiesFoldout = new List<bool>();
			}
			_mapObjects.Clear();
			_objFoldout.Clear();
			_objPropertiesFoldout.Clear();
			foreach (NanoXMLNode objectNode in _objectLayerNode.SubNodes)
			{
				if (!objectNode.Name.Equals("object"))
					continue;
				_mapObjects.Add(new MapObject(objectNode, null));
				_objFoldout.Add(true);
				_objPropertiesFoldout.Add(false);
			}
		}

		float _labelHeight = 20;

		void OnGUI()
		{
			if (_objectLayerNode == null)
			{
				EditorGUILayout.HelpBox("No Object Layer was selected!", MessageType.Error, true);
				return;
			}
			_mainFoldout = EditorGUILayout.Foldout(_mainFoldout, new GUIContent(title, TiledMapComponentEditor.objectIcon));
			EditorGUI.indentLevel++;
			if (_mainFoldout)
			{
				_scrollPos = GUILayout.BeginScrollView(_scrollPos);
				for (int i = 0; i < _mapObjects.Count; i++)
				{
					_objFoldout[i] = EditorGUILayout.Foldout(_objFoldout[i], _mapObjects[i].Name);
					if (_objFoldout[i])
					{
						EditorGUI.indentLevel++;
						using (new FixedWidthLabel("Type:"))
						{
							EditorGUILayout.SelectableLabel(_mapObjects[i].Type, GUILayout.MaxHeight(_labelHeight));
						}

						using (new FixedWidthLabel("Object Type:"))
						{
							switch (_mapObjects[i].ObjectType)
							{
								case ObjectType.Ellipse:
									EditorGUILayout.LabelField(new GUIContent(TiledMapComponentEditor.objectTypeIcon_Ellipse), GUILayout.MaxWidth(_labelHeight));
									break;
								case ObjectType.Polyline:
									EditorGUILayout.LabelField(new GUIContent(TiledMapComponentEditor.objectTypeIcon_Polyline), GUILayout.MaxWidth(_labelHeight));
									break;
								case ObjectType.Polygon:
									EditorGUILayout.LabelField(new GUIContent(TiledMapComponentEditor.objectTypeIcon_Polygon), GUILayout.MaxWidth(_labelHeight));
									break;
								default:
									EditorGUILayout.LabelField(new GUIContent(TiledMapComponentEditor.objectTypeIcon_Box), GUILayout.MaxWidth(_labelHeight));
									break;
							}
							EditorGUILayout.SelectableLabel(_mapObjects[i].ObjectType.ToString(), GUILayout.MaxHeight(_labelHeight));
						}

						using (new FixedWidthLabel("Rotation:"))
						{
							EditorGUILayout.SelectableLabel(_mapObjects[i].Rotation.ToString(), GUILayout.MaxHeight(_labelHeight));
						}

						using (new FixedWidthLabel("Bounds:"))
						{
							EditorGUILayout.SelectableLabel(_mapObjects[i].Bounds.ToString(), GUILayout.MaxHeight(_labelHeight));
						}

						if (_mapObjects[i].Properties != null)
						{
							_objPropertiesFoldout[i] = EditorGUILayout.Foldout(_objPropertiesFoldout[i], "Properties");
							if (_objPropertiesFoldout[i])
							{
								EditorGUI.indentLevel++;
								EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Screen.width - EditorGUI.indentLevel * 15));
								EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
								EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
								EditorGUILayout.EndHorizontal();
								foreach (var property in _mapObjects[i].Properties)
								{
									EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Screen.width - EditorGUI.indentLevel * 15));
									EditorGUILayout.SelectableLabel(property.Name, GUILayout.MaxHeight(_labelHeight));
									EditorGUILayout.SelectableLabel(property.RawValue, GUILayout.MaxHeight(_labelHeight));
									EditorGUILayout.EndHorizontal();
								}

								EditorGUI.indentLevel--;
							}
						}

						EditorGUILayout.Space();
						EditorGUI.indentLevel--;
					}
				}
				GUILayout.EndScrollView();
			}
			EditorGUI.indentLevel--;
		}
	}
}
