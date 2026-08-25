using System.Collections.Generic;
using System.Linq;
using SP.Runtime.Core.Entities.Mined.Tree;
using UnityEditor;
using UnityEngine;

namespace SP.Editor.LevelDesign
{
    /// <summary>
    /// Tree Naturalizer — Editor window for adding organic, level-design-quality
    /// finishing touches to Gaia-placed trees (TreeEntity components).
    /// </summary>
    public class TreeNaturalizer : EditorWindow
    {
        // State
        private List<TreeEntity> _trees = new();
        private int _seed = 42;

        // Position settings
        [Header("Position")]
        private float _positionRadius = 0.8f; // max XZ drift in world units
        private bool _snapToTerrain = true;
        private float _terrainRayOffset = 10f;

        // Scale settings
        private float _scaleVariation = 0.12f; // ± relative scale change
        private bool _uniformScale = true;

        // Rotation settings
        private float _yawVariation = 25f; // ± degrees around Y
        private float _tiltVariation = 2.5f; // ± degrees tilt (X/Z)

        // Clustering
        private bool _useGroupBias = true;
        private float _groupRadius = 4f; // neighbourhood for group bias
        private float _groupStrength = 0.35f; // 0 = no pull, 1 = full pull
        private float _minDistance = 1.5f;
        private int _separationIterations = 5;

        // Size-by-neighbour
        private bool _shrinkNearby = true;
        private float _shrinkRadius = 3f;
        private float _shrinkAmount = 0.08f; // max scale reduction

        // UI state
        private Vector2 _scroll;
        private bool _foldPos = true;
        private bool _foldScale = true;
        private bool _foldRot = true;
        private bool _foldGroup = true;

        // Colours
        private static readonly Color HeaderCol = new(0.15f, 0.55f, 0.35f);
        private static readonly Color AccentCol = new(0.25f, 0.80f, 0.50f);
        private static readonly Color DangerCol = new(0.85f, 0.30f, 0.25f);
        
        [MenuItem("Tools/Level Design/Tree Naturalizer")]
        public static void OpenWindow()
        {
            var win = GetWindow<TreeNaturalizer>("Tree Naturalizer");
            win.minSize = new Vector2(340, 520);
            win.Refresh();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTreeCount();
            DrawSeedRow();
            EditorGUILayout.Space(6);

            _foldPos = DrawSection("Position Drift", _foldPos, DrawPositionSettings);
            _foldScale = DrawSection("Scale Variation", _foldScale, DrawScaleSettings);
            _foldRot = DrawSection("Rotation Variation", _foldRot, DrawRotationSettings);
            _foldGroup = DrawSection("Organic Clustering", _foldGroup, DrawGroupSettings);

            EditorGUILayout.Space(8);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 44);
            EditorGUI.DrawRect(rect, HeaderCol);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            GUI.Label(rect, "Tree Naturalizer", style);
        }

        private void DrawTreeCount()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            var label = _trees.Count == 0
                ? "No TreeEntity objects found in scene"
                : $"{_trees.Count} TreeEntity objects ready";
            
            var col = _trees.Count == 0 ? new Color(1f, 0.7f, 0.2f) : AccentCol;
            var s = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = col }
            };
            
            GUILayout.Label(label, s);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", GUILayout.Width(76)))
            {
                Refresh();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeedRow()
        {
            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Random Seed", _seed);

            if (GUILayout.Button("🎲", GUILayout.Width(28)))
            {
                _seed = Random.Range(0, 99999);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private static bool DrawSection(string title, bool open, System.Action body)
        {
            EditorGUILayout.Space(4);
            var r = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(r, new Color(0.22f, 0.22f, 0.22f));
            
            var ts = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.85f, 0.95f, 0.88f) }
            };
            
            open = EditorGUI.Foldout(r, open, "  " + title, true, ts);
            
            if (open)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);
                body();
                EditorGUI.indentLevel--;
            }
            
            return open;
        }

        private void DrawPositionSettings()
        {
            _positionRadius = EditorGUILayout.Slider(
                new GUIContent("Max Drift (m)",
                    "Maximum XZ displacement from the original position (metres)."),
                _positionRadius, 0f, 3f);

            _snapToTerrain = EditorGUILayout.Toggle(
                new GUIContent("Snap Y to Terrain",
                    "Raycast downward and place the tree exactly on the terrain surface."),
                _snapToTerrain);

            if (_snapToTerrain)
            {
                EditorGUI.indentLevel++;
                _terrainRayOffset = EditorGUILayout.FloatField(
                    new GUIContent("Ray Start Offset",
                        "How high above the tree to start the raycast."),
                    _terrainRayOffset);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.HelpBox(
                "Trees will nudge within a circle of this radius.\n" +
                "0.5 – 1.0 m gives subtle, natural-looking drift.",
                MessageType.None);
        }

        private void DrawScaleSettings()
        {
            _scaleVariation = EditorGUILayout.Slider(
                new GUIContent("Scale Variation ±",
                    "Relative scale change. 0.12 = ±12 % of the original scale."),
                _scaleVariation, 0f, 0.5f);

            _uniformScale = EditorGUILayout.Toggle(
                new GUIContent("Uniform Scale",
                    "When enabled all axes scale together; disable for slight stretch."),
                _uniformScale);
        }

        private void DrawRotationSettings()
        {
            _yawVariation = EditorGUILayout.Slider(
                new GUIContent("Yaw Variation °",
                    "Random rotation around the Y-axis (up). 15–30° looks natural."),
                _yawVariation, 0f, 180f);

            _tiltVariation = EditorGUILayout.Slider(
                new GUIContent("Tilt Variation °",
                    "Slight lean on X/Z axes — mimics wind-shaped growth. Keep < 5°."),
                _tiltVariation, 0f, 10f);
        }

        private void DrawGroupSettings()
        {
            _useGroupBias = EditorGUILayout.Toggle(
                new GUIContent("Enable Group Bias",
                    "Nudge isolated trees slightly toward their nearest neighbours, " +
                    "reinforcing natural clustering."),
                _useGroupBias);

            if (_useGroupBias)
            {
                EditorGUI.indentLevel++;
                _groupRadius = EditorGUILayout.Slider(
                    new GUIContent("Group Radius (m)",
                        "Search radius for neighbours to bias toward."),
                    _groupRadius, 1f, 15f);
                _groupStrength = EditorGUILayout.Slider(
                    new GUIContent("Group Strength",
                        "0 = no pull toward neighbours; 1 = fully snaps to centroid."),
                    _groupStrength, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            _shrinkNearby = EditorGUILayout.Toggle(
                new GUIContent("Shrink Crowded Trees",
                    "Trees with many close neighbours get a slight scale reduction, " +
                    "simulating competition for light — a classic level-design trick."),
                _shrinkNearby);

            if (_shrinkNearby)
            {
                EditorGUI.indentLevel++;
                _shrinkRadius = EditorGUILayout.Slider(
                    new GUIContent("Shrink Radius (m)",
                        "Neighbour search radius for the shrink calculation."),
                    _shrinkRadius, 1f, 10f);
                _shrinkAmount = EditorGUILayout.Slider(
                    new GUIContent("Max Shrink",
                        "Maximum scale reduction for densely packed trees."),
                    _shrinkAmount, 0f, 0.4f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.HelpBox(
                "Group Bias + Shrink Crowded Trees together are the biggest " +
                "contributors to a hand-crafted, level-design feel.",
                MessageType.None);
            
            EditorGUILayout.Space(4);

            _minDistance = EditorGUILayout.Slider(
                new GUIContent("Min Distance (m)",
                    "Minimum allowed distance between any two trees. Prevents overlapping."),
                _minDistance, 0.5f, 5f);

            _separationIterations = EditorGUILayout.IntSlider(
                new GUIContent("Separation Passes",
                    "How many times to run the separation solver. More = more accurate but slower."),
                _separationIterations, 1, 10);
        }

        private void DrawActions()
        {
            if (_trees.Count == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("No trees found — press Refresh", GUILayout.Height(38));
                EditorGUI.EndDisabledGroup();
                return;
            }

            // Apply
            var applyStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = 
                { 
                    textColor = Color.white,
                    background = MakeTex(2, 2, HeaderCol) 
                },
                hover = 
                { 
                    textColor = Color.white,
                    background = MakeTex(2, 2, AccentCol) 
                }
            };

            if (GUILayout.Button("Apply Naturalization", applyStyle, GUILayout.Height(42)))
            {
                ApplyNaturalization();
            }

            EditorGUILayout.Space(4);

            // Reset
            var resetStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = DangerCol }
            };

            if (GUILayout.Button("Undo Last Apply  (Ctrl+Z also works)", resetStyle))
            {
                Undo.PerformUndo();
            }
        }
        
        // Core logic
        private void Refresh()
        {
            _trees = FindObjectsByType<TreeEntity>(FindObjectsSortMode.None).ToList();
            Repaint();
        }

        private void ApplyNaturalization()
        {
            if (_trees.Count == 0)
            {
                Debug.LogWarning("[TreeNaturalizer] No trees found."); 
                return;
            }

            // Register full undo so Ctrl+Z restores everything
            Undo.SetCurrentGroupName("Tree Naturalization");
            var undoGroup = Undo.GetCurrentGroup();

            foreach (var t in _trees)
            {
                Undo.RecordObject(t.transform, "Tree Naturalization");
            }

            Random.InitState(_seed);

            // Cache original positions for group-bias calculation BEFORE we move anything
            var origPos = _trees.Select(t => t.transform.position).ToArray();

            for (var i = 0; i < _trees.Count; i++)
            {
                var tr = _trees[i].transform;

                // 1. Compute group bias offset
                var groupOffset = Vector3.zero;
                
                if (_useGroupBias)
                {
                    var neighbours = new List<Vector3>();
                    
                    for (var j = 0; j < _trees.Count; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }
                        
                        var d = Vector2.Distance(
                            new Vector2(origPos[i].x, origPos[i].z),
                            new Vector2(origPos[j].x, origPos[j].z));

                        if (d < _groupRadius)
                        {
                            neighbours.Add(origPos[j]);
                        }
                    }
                    
                    if (neighbours.Count > 0)
                    {
                        var centroid = neighbours.Aggregate(Vector3.zero, (a, b) => a + b)
                                           / neighbours.Count;
                        var toward = centroid - origPos[i];
                        toward.y = 0;
                        // Pull is stronger when the tree is very isolated
                        var isolation = 1f - Mathf.Clamp01(neighbours.Count / 5f);
                        groupOffset = toward * (_groupStrength * isolation);
                    }
                }

                // 2. Random XZ drift + group bias
                var randCircle = Random.insideUnitCircle * _positionRadius;
                var newPos = origPos[i] + new Vector3(randCircle.x, 0, randCircle.y) + groupOffset;

                // 3. Snap Y to terrain
                if (_snapToTerrain)
                {
                    var rayOrigin = newPos + Vector3.up * _terrainRayOffset;

                    newPos.y = 
                        Physics.Raycast(rayOrigin, Vector3.down, out var hit, _terrainRayOffset * 2f) ? 
                            hit.point.y :
                            origPos[i].y; // fallback: keep original Y
                }

                tr.position = newPos;

                // 4. Neighbour-count shrink
                var shrinkFactor = 0f;
                
                if (_shrinkNearby)
                {
                    var nearby = 0;
                    
                    for (var j = 0; j < _trees.Count; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }
                        
                        var d = Vector2.Distance(
                            new Vector2(origPos[i].x, origPos[i].z),
                            new Vector2(origPos[j].x, origPos[j].z));

                        if (d < _shrinkRadius)
                        {
                            nearby++;
                        }
                    }
                    // More neighbours → more shrink, capped
                    shrinkFactor = Mathf.Clamp01(nearby / 4f) * _shrinkAmount;
                }

                // 5. Scale variation
                var baseVariation = Random.Range(-_scaleVariation, _scaleVariation);
                var scaleMult = 1f + baseVariation - shrinkFactor;

                if (_uniformScale)
                {
                    tr.localScale = tr.localScale * scaleMult;
                }
                else
                {
                    // Independent per-axis variation for slightly more organic look
                    var sx = 1f + Random.Range(-_scaleVariation, _scaleVariation) - shrinkFactor;
                    var sy = 1f + Random.Range(-_scaleVariation, _scaleVariation) - shrinkFactor;
                    var sz = 1f + Random.Range(-_scaleVariation, _scaleVariation) - shrinkFactor;
                    tr.localScale = Vector3.Scale(tr.localScale, new Vector3(sx, sy, sz));
                }

                // 6. Rotation
                var yaw = Random.Range(-_yawVariation, _yawVariation);
                var tiltX = Random.Range(-_tiltVariation, _tiltVariation);
                var tiltZ = Random.Range(-_tiltVariation, _tiltVariation);
                tr.rotation = Quaternion.Euler(tiltX, tr.eulerAngles.y + yaw, tiltZ);
            }
            
            // 7. Separation pass
            var newPositions = _trees.Select(t => t.transform.position).ToArray();

            for (var iter = 0; iter < _separationIterations; iter++)
            {
                var moved = false;

                for (var i = 0; i < _trees.Count; i++)
                {
                    for (var j = i + 1; j < _trees.Count; j++)
                    {
                        var delta = newPositions[i] - newPositions[j];
                        delta.y = 0f;
                        var dist = delta.magnitude;

                        if (dist < _minDistance && dist > 0.001f)
                        {
                            var push = delta.normalized * ((_minDistance - dist) * 0.5f);
                            newPositions[i] += push;
                            newPositions[j] -= push;
                            moved = true;
                        }
                    }
                }

                if (!moved)
                {
                    break;
                }
            }
            
            for (var i = 0; i < _trees.Count; i++)
            {
                var pos = newPositions[i];

                if (_snapToTerrain)
                {
                    var rayOrigin = pos + Vector3.up * _terrainRayOffset;
                    
                    if (Physics.Raycast(rayOrigin, Vector3.down, out var hit2, _terrainRayOffset * 2f))
                    {
                        pos.y = hit2.point.y;
                    }
                }

                _trees[i].transform.position = pos;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[TreeNaturalizer] Naturalized {_trees.Count} trees. Seed={_seed}");
            SceneView.RepaintAll();
        }

        // Utility
        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h);
            var pixels = Enumerable.Repeat(col, w * h).ToArray();
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
