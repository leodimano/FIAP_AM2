/**
 *	@sixbyseven studio
 *	7/8/2013
 */
#if UNITY_4_3 || UNITY_4_3_0 || UNITY_4_3_1 || UNITY_4_3_2 || UNITY_4_3_3 || UNITY_4_3_4 || UNITY_4_3_5
#define UNITY_4_3
#elif UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_3_0 || UNITY_4_3_1 || UNITY_4_3_2 || UNITY_4_3_3 || UNITY_4_3_4 || UNITY_4_3_5
#define UNITY_4
#elif UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define UNITY_3
#endif

#define PRO

using UnityEngine;
using UnityEditor;
using SixBySeven;
using System.Collections;
using System.Reflection;
using ProGrids;

[System.Serializable]
[InitializeOnLoad]
public class pg_Editor : EditorWindow
{

    #region MEMBERS

    public static pg_Editor pg;
    Color pgButtonColor = new Color(.35f, .35f, .35f, 1f);


    private bool useAxisConstraints = true;
    private bool snapEnabled = true;
    private SnapUnit snapUnit = SnapUnit.Meter;
#if PRO
    private float snapValue = 1f;						// the actual snap value, taking into account unit size
    private float t_snapValue = 1f;						// what the user sees
#else
	private float snapValue = .25f;
	private float t_snapValue = .25f;					
#endif
    private bool drawGrid = true;
    private bool drawAngles = false;
    private float angleValue = 45f;
    #endregion

    #region CONSTANT

#if PRO
    const int WINDOW_HEIGHT = 270;
#else
	const int WINDOW_HEIGHT = 290;
#endif

    const float METER = 1f;
#if PRO
    const float CENTIMETER = .01f;
    const float MILLIMETER = .001f;
    const float INCH = 0.0253999862840074f;
    const float FOOT = 0.3048f;
    const float YARD = 1.09361f;
    const float PARSEC = 5f;
#endif

    const int MAX_LINES = 150;							// the maximum amount of lines to display on screen in either direction

    const int BUTTON_SIZE = 45;
    private static Texture2D gui_SnapToGrid;
    private static Texture2D gui_SnapOn;
    private static Texture2D gui_SnapOff;
    private static Texture2D gui_VisOn;
    private static Texture2D gui_VisOff;
    private static Texture2D gui_AnglesOn;
    private static Texture2D gui_AnglesOff;


    private GUIContent gc_SnapToGrid = new GUIContent(gui_SnapToGrid, "Snaps all selected objects to grid.");
#if PRO
    private GUIContent gc_SnapEnabled = new GUIContent(gui_SnapOff, "Toggles snapping on or off.");
    private GUIContent gc_SnapUnit = new GUIContent("", "The unit of measurement to draw guides to.");
    private GUIContent gc_DrawGrid = new GUIContent(gui_VisOn, "Toggles drawing of guide lines on or off.  Note that object snapping is not affected by this setting.");
    private GUIContent gc_DrawAngles = new GUIContent(gui_AnglesOn, "If on, ProGrids will draw angled line guides.  Angle is settable in degrees.");
    private GUIContent gc_AngleValue = new GUIContent("", "The degree at which angle guides will be drawn.");
#else
	private GUIContent gc_SnapEnabled = new GUIContent(gui_SnapToGrid, "Toggles snapping on or off.");
	private GUIContent gc_SnapUnit = new GUIContent("", "The unit of measurement to draw guides to.  Pro only feature - QuickGrids is limited to Meter increments.");
	private GUIContent gc_DrawGrid = new GUIContent(gui_VisOn, "Toggles drawing of guide lines on or off.  Note that object snapping is not affected by this setting.");
	private GUIContent gc_DrawAngles = new GUIContent(gui_AnglesOn, "If on, ProGrids will draw angled line guides.  Angle is settable in degrees.  Pro only feature.");
	private GUIContent gc_AngleValue = new GUIContent("", "The degree at which angle guides will be drawn. Pro only feature.");
#endif

    #endregion

    #region PREFERENCES
    /** Defaults **/
    public Color GRID_COLOR_X = new Color(.9f, .46f, .46f, .447f);
    public Color GRID_COLOR_Y = new Color(.46f, .9f, .46f, .447f);
    public Color GRID_COLOR_Z = new Color(.46f, .46f, .9f, .447f);

    /** Settings **/
    public Color gridColorX, gridColorY, gridColorZ;

    // private bool lockOrthographic;

    public void LoadPreferences()
    {
#if PRO
        if (EditorPrefs.HasKey(k.SnapValueEditorPref) && EditorPrefs.HasKey(k.SnapUnitEditorPref))
            SetSnapValue(SnapUnitWithString(EditorPrefs.GetString(k.SnapUnitEditorPref)), EditorPrefs.GetFloat(k.SnapValueEditorPref));
#endif

        if (EditorPrefs.HasKey(k.UseAxisConstraints))
            useAxisConstraints = EditorPrefs.GetBool(k.UseAxisConstraints);

        SixBySeven.Shared.useAxisConstraints = useAxisConstraints;

        gridColorX = (EditorPrefs.HasKey("gridColorX")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorX")) : GRID_COLOR_X;
        gridColorY = (EditorPrefs.HasKey("gridColorY")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorY")) : GRID_COLOR_Y;
        gridColorZ = (EditorPrefs.HasKey("gridColorZ")) ? pg_Util.ColorWithString(EditorPrefs.GetString("gridColorZ")) : GRID_COLOR_Z;

        //		lockOrthographic = (EditorPrefs.HasKey("lockOrthographic") ? EditorPrefs.GetBool("lockOrthographic") : false);
    }

    private GUISkin sixBySevenSkin;
    #endregion

    #region ENUM

    public enum Axes
    {
        X,
        Y,
        Z,
        NegX,
        NegY,
        NegZ
    }

    public enum SnapUnit
    {
        Meter,
#if PRO
        Centimeter,
        Millimeter,
        Inch,
        Foot,
        Yard,
        Parsec
#endif
    }

    public float SnapUnitValue(SnapUnit su)
    {
        switch (su)
        {
            case SnapUnit.Meter:
                return METER;
#if PRO
            case SnapUnit.Centimeter:
                return CENTIMETER;
            case SnapUnit.Millimeter:
                return MILLIMETER;
            case SnapUnit.Inch:
                return INCH;
            case SnapUnit.Foot:
                return FOOT;
            case SnapUnit.Yard:
                return YARD;
            case SnapUnit.Parsec:
                return PARSEC;
#endif
            default:
                return METER;
        }
    }
    #endregion

    #region INITIALIZATION

    [MenuItem("Tools/ProGrids/ProGrids Window", false, 15)]
    public static void InitProGrids()
    {
        EditorWindow.GetWindow(typeof(pg_Editor), false, "PG", true).autoRepaintOnSceneChange = true;
        SceneView.RepaintAll();
    }

    // hax
    public void OnInspectorGUI()
    {
        if (EditorWindow.focusedWindow != this)
        {
            Repaint();
        }
    }

    public void OnEnable()
    {
        pg = this;

        HookSceneView();
        LoadGUIResources();
        LoadPreferences();
        autoRepaintOnSceneChange = true;
        SetSharedSnapValues(snapEnabled, snapValue);

        toggleStyle.alignment = TextAnchor.MiddleCenter;

        this.minSize = new Vector2(BUTTON_SIZE + 4, WINDOW_HEIGHT);
        this.maxSize = new Vector2(BUTTON_SIZE + 4, WINDOW_HEIGHT);
    }

    public void OnFocus()
    {
        SetSharedSnapValues(snapEnabled, snapValue);
        SceneView.RepaintAll();
    }

    public void OnDisable()
    {
        SceneView.RepaintAll();
        SetSharedSnapValues(false, snapValue);
    }

    public void OnDestroy()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    public void HookSceneView()
    {
        if (SceneView.onSceneGUIDelegate != this.OnSceneGUI)
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }
    }

    public void LoadGUIResources()
    {
        toggleStyle.margin = new RectOffset(5, 5, 5, 5);

        gui_SnapToGrid = (Texture2D)Resources.Load("GUI/ProGridsGUI_SnapToGrid");
        gui_SnapOn = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_OnLight");
        gui_SnapOff = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_OffLight");
        gui_VisOn = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_VisOn");
        gui_VisOff = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_VisOff");
        gui_AnglesOn = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_AnglesOn");
        gui_AnglesOff = (Texture2D)Resources.Load("GUI/ProGridsToggles/ProGridsGUI_AnglesOff");

        gc_SnapToGrid.image = gui_SnapToGrid;
    }
    #endregion

    #region INTERFACE

    RectOffset pad = new RectOffset(0, 0, 0, 0);
    RectOffset oldPad = new RectOffset(2, 2, 2, 2);
    GUIStyle toggleStyle = GUIStyle.none;
    const int TOGGLE_WIDTH = 20;
    Color oldColor;
    public void OnGUI()
    {
        if (!EditorGUIUtility.isProSkin)
        {
            oldColor = GUI.backgroundColor;
            GUI.backgroundColor = pgButtonColor;
        }

        GUI.skin.button.padding = pad;

        if (pgButton(gc_SnapToGrid))
            SnapToGrid(Selection.transforms);

        gc_SnapEnabled.image = (snapEnabled) ? gui_SnapOn : gui_SnapOff;
        if (pgButton(gc_SnapEnabled))
            SetSnapEnabled(!snapEnabled);

        gc_DrawGrid.image = (drawGrid) ? gui_VisOn : gui_VisOff;
        if (pgButton(gc_DrawGrid))
            SetGridEnabled(!drawGrid);


        EditorGUI.BeginChangeCheck();
        snapUnit = (SnapUnit)EditorGUILayout.EnumPopup(gc_SnapUnit, snapUnit,
            GUILayout.MinWidth(BUTTON_SIZE),
            GUILayout.MaxWidth(BUTTON_SIZE));

#if !PRO
		GUI.enabled = false;
#endif

        gc_DrawAngles.image = (drawAngles) ? gui_AnglesOn : gui_AnglesOff;
        if (GUILayout.Button(gc_DrawAngles, toggleStyle, GUILayout.MinWidth(BUTTON_SIZE)))
            SetDrawAngles(!drawAngles);

#if PRO
        GUI.enabled = drawAngles;
#endif

        GUILayout.Label("Angle");

        EditorGUI.BeginChangeCheck();
        angleValue = EditorGUILayout.FloatField(gc_AngleValue, angleValue,
            GUILayout.MinWidth(BUTTON_SIZE),
            GUILayout.MaxWidth(BUTTON_SIZE));
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
#if PRO
        GUI.enabled = true;
#endif

        GUILayout.Label("Snap");
        t_snapValue = EditorGUILayout.FloatField("", t_snapValue,
            GUILayout.MinWidth(BUTTON_SIZE),
            GUILayout.MaxWidth(BUTTON_SIZE));
        if (EditorGUI.EndChangeCheck())
        {
#if PRO
            SetSnapValue(snapUnit, t_snapValue);
#endif
        }

#if !PRO
		GUI.enabled = true;
			
		GUI.backgroundColor = Color.cyan;
		if(GUILayout.Button("More", GUILayout.MinHeight(18)))
			OpenProGridsPopup();
		GUI.backgroundColor = Color.white;

#endif

        if (!EditorGUIUtility.isProSkin)
            GUI.backgroundColor = oldColor;
        GUI.skin.button.padding = oldPad;
    }

    public bool pgButton(GUIContent content)
    {
        if (GUILayout.Button(content,
            GUILayout.MinWidth(BUTTON_SIZE),
            GUILayout.MaxWidth(BUTTON_SIZE),
            GUILayout.MinHeight(BUTTON_SIZE),
            GUILayout.MaxHeight(BUTTON_SIZE)
            ))
            return true;
        return false;
    }

    public bool pgButton(GUIContent content, GUIStyle style)
    {
        if (GUILayout.Button(content, style,
            GUILayout.MinWidth(BUTTON_SIZE), GUILayout.MaxWidth(BUTTON_SIZE),
            GUILayout.MinHeight(BUTTON_SIZE), GUILayout.MaxHeight(BUTTON_SIZE)))
            return true;
        return false;
    }

    public void OpenProGridsPopup()
    {
        if (EditorUtility.DisplayDialog(
            "Upgrade to ProGrids",				// Title
            "Enables all kinds of super-cool features, like different snap values, more units of measurement, and angles.",						  // Message
            "Upgrade",							// Okay
            "Cancel"							// Cancel
            ))
            // #if UNITY_4
            // AssetStore.OpenURL(k.ProGridsUpgradeURL);
            // #else
            Application.OpenURL(k.ProGridsUpgradeURL);
        // #endif
    }
    #endregion

    #region ONSCENEGUI

    private Transform lastTransform;

    public Vector3 lastPosition = Vector3.zero;

    public void OnSceneGUI(SceneView scnview)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;	// don't snap stuff in play mode

        Event e = Event.current;

        Camera cam = Camera.current;

        if (cam.orthographic && IsRounded(scnview.rotation.eulerAngles.normalized) && drawGrid)
            DrawGrid(cam);


        if (e.type == EventType.ValidateCommand)
        {
            OnValidateCommand(Event.current.commandName);
            return;
        }

        // Always keep track of the selection
        if (!Selection.transforms.Contains(lastTransform))
        {
            if (Selection.activeTransform)
            {
                lastTransform = Selection.activeTransform;
                lastPosition = Selection.activeTransform.position;
            }
        }

        if (!snapEnabled || GUIUtility.hotControl < 1)
            return;

        /**
         *	Snapping
         */
        if (Selection.activeTransform)
        {
            if (lastTransform.position != lastPosition)
            {
                Transform selected = lastTransform;

                Vector3 old = selected.position;
                Vector3 mask = old - lastPosition;

                if (useAxisConstraints)
                    selected.position = SnapValue(old);
                else
                    selected.position = SnapValue(old, mask);

                Vector3 offset = selected.position - old;

                OffsetTransforms(Selection.transforms, selected, offset);

                lastPosition = selected.position;
            }
        }
    }
    #endregion

    #region GRAPHICS

    public void DrawGrid(Camera cam)
    {
        Axes camAxis = AxesWithVector(Camera.current.transform.TransformDirection(Vector3.forward).normalized);

        if (drawGrid)
        {
            switch (camAxis)
            {
                case Axes.X:
                case Axes.NegX:
                    DrawGridGraphics(cam, camAxis, gridColorX);
                    break;

                case Axes.Y:
                case Axes.NegY:
                    DrawGridGraphics(cam, camAxis, gridColorY);
                    break;

                case Axes.Z:
                case Axes.NegZ:
                    DrawGridGraphics(cam, camAxis, gridColorZ);
                    break;
            }
        }
    }

    Color previousColor;
    private void DrawGridGraphics(Camera cam, Axes camAxis, Color col)
    {
        previousColor = Handles.color;
        Handles.color = col;

        // !-- TODO: Update this stuff only when necessary.  Currently it runs evvverrrryyy frame
        Vector3 bottomLeft = SnapToFloor(cam.ScreenToWorldPoint(Vector2.zero));
        Vector3 bottomRight = SnapToFloor(cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, 0f)));
        Vector3 topLeft = SnapToFloor(cam.ScreenToWorldPoint(new Vector2(0f, cam.pixelHeight)));
        Vector3 topRight = SnapToFloor(cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, cam.pixelHeight)));

        Vector3 axis = VectorWithAxes(camAxis);

        float width = Vector3.Distance(bottomLeft, bottomRight);
        float height = Vector3.Distance(bottomRight, topRight);

        // Shift lines to 1m forward of the camera
        bottomLeft += axis * 2;
        topRight += axis * 2;
        bottomRight += axis * 2;
        topLeft += axis * 2;

        /** 
         *	Draw Vertical Lines 
         *	Add two because we want the grid to cover the entire screen.
         */
        Vector3 start = bottomLeft - cam.transform.up * (height + snapValue * 2);
        Vector3 end = bottomLeft + cam.transform.up * (height + snapValue * 2);

        float _snapVal = snapValue;

        int segs = (int)Mathf.Ceil(width / _snapVal) + 2;

        float n = 2;
        while (segs > MAX_LINES)
        {
            _snapVal = _snapVal * n;
            segs = (int)Mathf.Ceil(width / _snapVal);
            n++;
        }

        for (int i = -1; i < segs; i++)
        {
            Handles.DrawLine(
                start + (i * (cam.transform.right * _snapVal)),
                end + (i * (cam.transform.right * _snapVal)));
        }

        /** 
         * Draw Horizontal Lines
         */
        start = topLeft - cam.transform.right * (width + snapValue * 2);
        end = topLeft + cam.transform.right * (width + snapValue * 2);

        segs = (int)Mathf.Ceil(height / _snapVal) + 2;

        n = 1;
        while (segs > MAX_LINES)
        {
            _snapVal = _snapVal * n;
            n++;
            segs = (int)Mathf.Ceil(height / _snapVal);
        }

        for (int i = -1; i < segs; i++)
        {
            Handles.DrawLine(
                start + (i * (-cam.transform.up * _snapVal)),
                end + (i * (-cam.transform.up * _snapVal)));
        }

#if PRO
        if (drawAngles)
        {
            Vector3 cen = SnapValue(((topRight + bottomLeft) / 2f));

            float half = (width > height) ? width : height;

            float opposite = Mathf.Tan(Mathf.Deg2Rad * angleValue) * half;

            Vector3 up = cam.transform.up * opposite;
            Vector3 right = cam.transform.right * half;

            Vector3 bottomLeftAngle = cen - (up + right);
            Vector3 topRightAngle = cen + (up + right);

            Vector3 bottomRightAngle = cen + (right - up);
            Vector3 topLeftAngle = cen + (up - right);

            // y = 1x+1
            Handles.DrawLine(bottomLeftAngle, topRightAngle);

            // y = -1x-1
            Handles.DrawLine(topLeftAngle, bottomRightAngle);
        }
#endif

        Handles.color = previousColor;
    }
    #endregion

    #region ENUM UTILITY

    public SnapUnit SnapUnitWithString(string str)
    {
        foreach (SnapUnit su in SnapUnit.GetValues(typeof(SnapUnit)))
        {
            if (su.ToString() == str)
                return su;
        }
        return (SnapUnit)0;
    }

    public Axes AxesWithVector(Vector3 val)
    {
        Vector3 v = new Vector3(Mathf.Abs(val.x), Mathf.Abs(val.y), Mathf.Abs(val.z));

        if (v.x > v.y && v.x > v.z)
        {
            if (val.x > 0)
                return Axes.X;
            else
                return Axes.NegX;
        }
        else
            if (v.y > v.x && v.y > v.z)
            {
                if (val.y > 0)
                    return Axes.Y;
                else
                    return Axes.NegY;
            }
            else
            {
                if (val.z > 0)
                    return Axes.Z;
                else
                    return Axes.NegZ;
            }
    }

    public Vector3 VectorWithAxes(Axes axis)
    {
        switch (axis)
        {
            case Axes.X:
                return Vector3.right;
            case Axes.Y:
                return Vector3.up;
            case Axes.Z:
                return Vector3.forward;
            case Axes.NegX:
                return -Vector3.right;
            case Axes.NegY:
                return -Vector3.up;
            case Axes.NegZ:
                return -Vector3.forward;

            default:
                return Vector3.forward;
        }
    }

    public bool IsRounded(Vector3 v)
    {
        return (Mathf.Approximately(v.x, 1f) || Mathf.Approximately(v.y, 1f) || Mathf.Approximately(v.z, 1f)) || v == Vector3.zero;
    }

    public Vector3 RoundAxis(Vector3 v)
    {
        return VectorWithAxes(AxesWithVector(v));
    }
    #endregion

    #region EVENT

    bool oldVal;
    void OnValidateCommand(string command)
    {
        switch (command)
        {
            case "UndoRedoPerformed":

                if (Selection.activeTransform)
                {
                    lastTransform = Selection.activeTransform;
                    lastPosition = Selection.activeTransform.position;
                }

                break;
        }
    }
    #endregion

    #region SNAP

    private void SnapToGrid(Transform[] transforms)
    {
#if UNITY_4_3
		Undo.RecordObjects(transforms as Object[], "Snap to Grid");
#else
        //Undo.RegisterUndo(transforms as Object[], "Snap to Grid");
        Undo.RecordObjects(transforms as Object[], "Snap to Grid");
#endif

        foreach (Transform t in transforms)
        {
            t.position = SnapValue(t.position);
        }
    }

    private Vector3 SnapValue(Vector3 val)
    {
        float _x = val.x, _y = val.y, _z = val.z;
        return new Vector3(
            Snap(_x),
            Snap(_y),
            Snap(_z)
            );
    }

    private Vector3 SnapValue(Vector3 val, Vector3 mask)
    {
        float _x = val.x, _y = val.y, _z = val.z;
        return new Vector3(
            (Mathf.Approximately(mask.x, 0f) ? _x : Snap(_x)),
            (Mathf.Approximately(mask.y, 0f) ? _y : Snap(_y)),
            (Mathf.Approximately(mask.z, 0f) ? _z : Snap(_z))
            );
    }

    private Vector3 SnapToCeil(Vector3 val, Vector3 mask)
    {
        float _x = val.x, _y = val.y, _z = val.z;
        return new Vector3(
            (Mathf.Approximately(mask.x, 0f) ? _x : SnapToCeil(_x)),
            (Mathf.Approximately(mask.y, 0f) ? _y : SnapToCeil(_y)),
            (Mathf.Approximately(mask.z, 0f) ? _z : SnapToCeil(_z))
            );
    }

    private Vector3 SnapToFloor(Vector3 val)
    {
        float _x = val.x, _y = val.y, _z = val.z;
        return new Vector3(
            SnapToFloor(_x),
            SnapToFloor(_y),
            SnapToFloor(_z)
            );
    }

    private Vector3 SnapToFloor(Vector3 val, Vector3 mask)
    {
        float _x = val.x, _y = val.y, _z = val.z;
        return new Vector3(
            (Mathf.Approximately(mask.x, 0f) ? _x : SnapToFloor(_x)),
            (Mathf.Approximately(mask.y, 0f) ? _y : SnapToFloor(_y)),
            (Mathf.Approximately(mask.z, 0f) ? _z : SnapToFloor(_z))
            );
    }

    private float Snap(float val)
    {
        return snapValue * Mathf.Round(val / snapValue);
    }

    private float SnapToFloor(float val)
    {
        return snapValue * Mathf.Floor(val / snapValue);
    }

    private float SnapToCeil(float val)
    {
        return snapValue * Mathf.Ceil(val / snapValue);
    }
    #endregion

    #region MOVING TRANSFORMS

    public void OffsetTransforms(Transform[] trsfrms, Transform ignore, Vector3 offset)
    {
        foreach (Transform t in trsfrms)
        {
            if (t != ignore)
                t.position += offset;
        }
    }
    #endregion

    #region SETTINGS

    /**
	 *	ALL SETTERS ARE RESPONSIBLE FOR UPDATING PROBUILDER
	 */
    public void SetSnapEnabled(bool enable)
    {
        if (Selection.activeTransform)
        {
            lastTransform = Selection.activeTransform;
            lastPosition = Selection.activeTransform.position;
        }

        snapEnabled = enable;
        SceneView.RepaintAll();
        SetSharedSnapValues(snapEnabled, snapValue);
    }

    public void SetSnapValue(SnapUnit su, float val)
    {
#if PRO
        snapValue = SnapUnitValue(su) * val;
        SceneView.RepaintAll();
        SetSharedSnapValues(snapEnabled, snapValue);

        EditorPrefs.SetFloat(k.SnapValueEditorPref, val);
        EditorPrefs.SetString(k.SnapUnitEditorPref, su.ToString());

        // update gui (only necessary when calling with editorpref values)
        t_snapValue = val;
        snapUnit = su;

#else
		Debug.LogWarning("Ye ought not be seein' this ye scurvy pirate.");
#endif
    }

    public void SetGridEnabled(bool enable)
    {
        drawGrid = enable;
        SceneView.RepaintAll();
    }

    public void SetDrawAngles(bool enable)
    {
        drawAngles = enable;
        SceneView.RepaintAll();
    }
    #endregion

    #region GLOBAL SETTING

    public void SetSharedSnapValues(bool enable, float snapVal)
    {
        SixBySeven.Shared.snapEnabled = enable;
        SixBySeven.Shared.snapValue = snapVal;
    }
    #endregion
}