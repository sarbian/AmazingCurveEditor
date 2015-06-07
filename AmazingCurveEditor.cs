using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace AmazingCurveEditor
{
    public class FloatString4 : IComparable<FloatString4>
    {
        public Vector4 floats;
        public string[] strings;

        public int CompareTo(FloatString4 other)
        {
            if (other == null)
            {
                return 1;
            }
            return floats.x.CompareTo(other.floats.x);
        }

        public FloatString4()
        {
            floats = new Vector4();
            strings = new string[] {"0", "0", "0", "0"};
        }

        public FloatString4(float x, float y, float z, float w)
        {
            floats = new Vector4(x, y, z, w);
            UpdateStrings();
        }

        public void UpdateFloats()
        {
            float x, y, z, w;
            float.TryParse(strings[0], out x);
            float.TryParse(strings[1], out y);
            float.TryParse(strings[2], out z);
            float.TryParse(strings[3], out w);
            floats = new Vector4(x, y, z, w);
        }

        public void UpdateStrings()
        {
            strings = new string[] {floats.x.ToString(), floats.y.ToString(), floats.z.ToString(), floats.w.ToString()};
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class AmazingCurveEditor : MonoBehaviour
    {
        private int texWidth = 512;
        private int texHeight = 128;

        private List<FloatString4> points = new List<FloatString4>();
        private FloatCurve curve = new FloatCurve();
        private bool curveNeedsUpdate = false;
        private Rect winPos = new Rect();
        private Texture2D graph;
        private Vector2 scrollPos = new Vector2();
        private string textVersion;
        private bool showUI;

        public void Start()
        {
            winPos = new Rect(Screen.width / 2, Screen.height / 2, 512, 400);
            graph = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, true);
            points.Add(new FloatString4(0, 0, 0, 0.02f));
            points.Add(new FloatString4(100, 1, 0.02f, 0));
            UpdateCurve();
        }

        private void OnGUI()
        {
            if (graph != null && showUI)
            {
                GUI.skin = AssetBase.GetGUISkin("KSP window 2");
                winPos = GUILayout.Window(9384, winPos, WindowGUI, "MuMech CurveEd");
            }
        }

        private void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(graph, GUILayout.Width(512), GUILayout.Height(128));
            GUILayout.EndVertical();

            FloatString4 excludePoint = null;

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.Height(200));
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical();
            GUILayout.Label("X");
            foreach (FloatString4 p in points)
            {
                string ns = GUILayout.TextField(p.strings[0]);
                if (ns != p.strings[0])
                {
                    p.strings[0] = ns;
                    p.UpdateFloats();
                    curveNeedsUpdate = true;
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Y");
            foreach (FloatString4 p in points)
            {
                string ns = GUILayout.TextField(p.strings[1]);
                if (ns != p.strings[1])
                {
                    p.strings[1] = ns;
                    p.UpdateFloats();
                    curveNeedsUpdate = true;
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("In Tangent");
            foreach (FloatString4 p in points)
            {
                string ns = GUILayout.TextField(p.strings[2]);
                if (ns != p.strings[2])
                {
                    p.strings[2] = ns;
                    p.UpdateFloats();
                    curveNeedsUpdate = true;
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Out Tangent");
            foreach (FloatString4 p in points)
            {
                string ns = GUILayout.TextField(p.strings[3]);
                if (ns != p.strings[3])
                {
                    p.strings[3] = ns;
                    p.UpdateFloats();
                    curveNeedsUpdate = true;
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Remove");
            foreach (FloatString4 p in points)
            {
                if (GUILayout.Button("X"))
                {
                    excludePoint = p;
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (excludePoint != null)
            {
                points.Remove(excludePoint);
                curveNeedsUpdate = true;
            }

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Add Node"))
            {
                points.Add(new FloatString4(points.Last().floats.x + 1, points.Last().floats.y, points.Last().floats.z,
                    points.Last().floats.w));
                curveNeedsUpdate = true;
            }
            GUILayout.EndHorizontal();

            string newT = GUILayout.TextArea(textVersion, GUILayout.ExpandWidth(true), GUILayout.Height(100));
            if (newT != textVersion)
            {
                StringToCurve(newT);
            }

            GUI.DragWindow();
        }

        private void UpdateCurve()
        {
            points.Sort();

            curve = new FloatCurve();

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (FloatString4 v in points)
            {
                curve.Add(v.floats.x, v.floats.y, v.floats.z, v.floats.w);
            }

            textVersion = CurveToString();

            for (int x = 0; x < texWidth; x++)
            {
                for (int y = 0; y < texHeight; y++)
                {
                    graph.SetPixel(x, y, Color.black);
                }
                float fY = curve.Evaluate(curve.minTime + curve.maxTime * (float) x / (float) (texWidth - 1));
                minY = Mathf.Min(minY, fY);
                maxY = Mathf.Max(maxY, fY);
            }
            for (int x = 0; x < texWidth; x++)
            {
                float fY = curve.Evaluate(curve.minTime + curve.maxTime * (float) x / (float) (texWidth - 1));
                graph.SetPixel(x, Mathf.RoundToInt((fY - minY) / (maxY - minY) * (texHeight - 1)), Color.green);
            }
            graph.Apply();
            curveNeedsUpdate = false;
        }

        private void Update()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F5))
            {
                showUI = !showUI;
            }

            if (curveNeedsUpdate)
            {
                UpdateCurve();
            }
        }

        private string CurveToString()
        {
            string buff = "";
            foreach (FloatString4 p in points)
            {
                buff += "key = " + p.floats.x + " " + p.floats.y + " " + p.floats.z + " " + p.floats.w + "\n";
            }
            return buff;
        }

        private void StringToCurve(string data)
        {
            points = new List<FloatString4>();

            string[] lines = data.Split('\n');
            foreach (string line in lines)
            {
                string[] pcs = line.Split(new char[] {'=', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if ((pcs.Length >= 5) && (pcs[0] == "key"))
                {
                    FloatString4 nv = new FloatString4();
                    nv.strings = new string[] {pcs[1], pcs[2], pcs[3], pcs[4]};
                    nv.UpdateFloats();
                    points.Add(nv);
                }
            }

            curveNeedsUpdate = true;
        }
    }
}
