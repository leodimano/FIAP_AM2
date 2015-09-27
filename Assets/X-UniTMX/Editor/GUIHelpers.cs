/*! 
 * X-UniTMX: A tiled map editor file importer for Unity3d
 * https://bitbucket.org/Chaoseiro/x-unitmx
 * 
 * Copyright 2013-2014 Guilherme "Chaoseiro" Maia
 *           2014 Mario Madureira Fontes
 *           
 *	FixedWidthLabel class thanks to http://www.toxicfork.com/90/fixed-width-labels-for-fields-in-unity3d
 */
using System;
using UnityEditor;
using UnityEngine;

namespace X_UniTMX.Utils
{
	//FixedWidthLabel class. Extends IDisposable, so that it can be used with the "using" keyword.
	public class FixedWidthLabel : IDisposable
	{
		private readonly ZeroIndent indentReset; //helper class to reset and restore indentation

		public FixedWidthLabel(GUIContent label)//	constructor.
		{//						state changes are applied here.
			EditorGUILayout.BeginHorizontal();// create a new horizontal group
			EditorGUILayout.LabelField(label,
				GUILayout.Width(GUI.skin.label.CalcSize(label).x +// actual label width
					15 * EditorGUI.indentLevel));//indentation from the left side. It's 9 pixels per indent level

			indentReset = new ZeroIndent();//helper class to have no indentation after the label
		}

		public FixedWidthLabel(string label)
			: this(new GUIContent(label))//alternative constructor, if we don't want to deal with GUIContents
		{
		}

		public void Dispose() //restore GUI state
		{
			indentReset.Dispose();//restore indentation
			EditorGUILayout.EndHorizontal();//finish horizontal group
		}
	}

	class ZeroIndent : IDisposable //helper class to clear indentation
	{
		private readonly int originalIndent;//the original indentation value before we change the GUI state
		public ZeroIndent()
		{
			originalIndent = EditorGUI.indentLevel;//save original indentation
			EditorGUI.indentLevel = 0;//clear indentation
		}

		public void Dispose()
		{
			EditorGUI.indentLevel = originalIndent;//restore original indentation
		}
	}
}