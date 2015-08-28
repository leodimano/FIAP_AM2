using UnityEngine;
using UnityEditor;
using ProGrids;
using System.Collections;

public class pg_Preferences {

	static Color _gridColorX;
	static Color _gridColorY;
	static Color _gridColorZ;
	static bool _useAxisConstraints;

	/** Defaults **/
	public static Color GRID_COLOR_X = new Color(.9f, .46f, .46f, .447f);
	public static Color GRID_COLOR_Y = new Color(.46f, .9f, .46f, .447f);
	public static Color GRID_COLOR_Z = new Color(.46f, .46f, .9f, .447f);
	public static bool USE_AXIS_CONSTRAINTS = true;

	static bool prefsLoaded = false;

	/** GUI ITEMS **/
	static Rect resetRect = new Rect(0f, 0f, 0f, 0f);

	[PreferenceItem ("ProGrids")]
	public static void PreferencesGUI ()
	{
		if(!prefsLoaded)
		{
			prefsLoaded = LoadPreferences();
			OnWindowResize();
		}

		EditorGUILayout.HelpBox("Changes will take effect on the next ProGrids open.", MessageType.Info);

		GUILayout.Label("Grid Colors per Axis", EditorStyles.boldLabel);
		_gridColorX = EditorGUILayout.ColorField("X Axis", _gridColorX);
		_gridColorY = EditorGUILayout.ColorField("Y Axis", _gridColorY);
		_gridColorZ = EditorGUILayout.ColorField("Z Axis", _gridColorZ);

		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(new GUIContent("Axis Constraints", "If toggled, objects will be automatically grid aligned on all axes when moving."));
		_useAxisConstraints = EditorGUILayout.Toggle(_useAxisConstraints);
		GUILayout.EndHorizontal();
		if(GUI.Button(resetRect, "Reset"))
			ResetPrefs();

		if(GUI.changed)
			SetPreferences();
	}

	public static bool LoadPreferences()
	{
		_gridColorX = (EditorPrefs.HasKey("gridColorX")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorX")) : GRID_COLOR_X;
		_gridColorY = (EditorPrefs.HasKey("gridColorY")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorY")) : GRID_COLOR_Y;
		_gridColorZ = (EditorPrefs.HasKey("gridColorZ")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorZ")) : GRID_COLOR_Z;
		_useAxisConstraints = (EditorPrefs.HasKey(k.UseAxisConstraints)) ? EditorPrefs.GetBool(k.UseAxisConstraints) : USE_AXIS_CONSTRAINTS;

		return true;
	}

	public static void SetPreferences()
	{
		EditorPrefs.SetString("gridColorX", _gridColorX.ToString("f3"));
		EditorPrefs.SetString("gridColorY", _gridColorY.ToString("f3"));
		EditorPrefs.SetString("gridColorZ", _gridColorZ.ToString("f3"));
	}

	public static void ResetPrefs()
	{
		if(EditorUtility.DisplayDialog("Delete ProGrids editor preferences?", "Are you sure you want to delete these?, this action cannot be undone.", "Yes", "No")) {
			EditorPrefs.DeleteKey("gridColorX");
			EditorPrefs.DeleteKey("gridColorY");
			EditorPrefs.DeleteKey("gridColorZ");
			EditorPrefs.DeleteKey(k.UseAxisConstraints);
		}

		LoadPreferences();
	}

	public static void OnWindowResize()
	{
		int pad = 10, buttonWidth = 100, buttonHeight = 20;
		resetRect = new Rect(Screen.width-pad-buttonWidth, Screen.height-pad-buttonHeight, buttonWidth, buttonHeight);
	}
}
