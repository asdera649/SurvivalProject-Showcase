using SP.Runtime.Core.Environment;
using UnityEditor;
using UnityEngine;

namespace SP.Editor.LevelDesign
{
    /// <summary>
    /// Editor tool для гармоничной покраски кустов на сцене.
    /// </summary>
    public class BushMaterialPainter : EditorWindow
    {
        private Material _materialNormal;
        private Material _materialDark;
        private Material _materialLight;
        
        [Tooltip("Масштаб шума. Больше = крупнее кластеры.")]
        private float _noiseScale = 0.035f;

        [Tooltip("Смещение seed по X. Меняй для разных результатов.")]
        private float _noiseSeedX = 137.4f;

        [Tooltip("Смещение seed по Y (Z в world space).")]
        private float _noiseSeedY = 289.7f;
        
        private float _thresholdLight = 0.90f;
        private float _thresholdDark  = 0.45f;
        
        private Vector2 _scrollPos;
        //private int _lastCount = -1;
        private string _statusMsg = "";
        private Color _statusColor = Color.white;
        
        [MenuItem("Tools/Level Design/Bush Material Painter")]
        public static void ShowWindow()
        {
            var w = GetWindow<BushMaterialPainter>("Bush Painter");
            w.minSize = new Vector2(360, 480);
        }
        
        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            GUILayout.Space(8);

            DrawMaterialsSection();
            GUILayout.Space(8);

            DrawNoiseSection();
            GUILayout.Space(8);

            DrawDistributionPreview();
            GUILayout.Space(12);

            DrawActions();
            GUILayout.Space(8);

            DrawStatus();

            EditorGUILayout.EndScrollView();
        }
        
        private static void DrawHeader()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Bush Material Painter", style, GUILayout.Height(28));
            EditorGUILayout.HelpBox(
                "Назначает материалы кустам (Bush) через Perlin Noise — " +
                "соседние кусты склонны иметь похожий цвет (эффект кластеров).",
                MessageType.None);
        }

        private void DrawMaterialsSection()
        {
            EditorGUILayout.LabelField("Материалы", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _materialNormal = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Обычный", "Базовый цвет куста"),
                    _materialNormal, typeof(Material), false);

                _materialDark = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Тёмный", "Более тёмный вариант"),
                    _materialDark, typeof(Material), false);

                _materialLight = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Светлый", "Акцентный светлый куст"),
                    _materialLight, typeof(Material), false);
            }
        }

        private void DrawNoiseSection()
        {
            EditorGUILayout.LabelField("Настройки шума (гармоничность)", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _noiseScale = EditorGUILayout.Slider(
                    new GUIContent("Noise Scale",
                        "Чем меньше — тем крупнее кластеры одного цвета.\n" +
                        "Рекомендуется 0.02 – 0.06"),
                    _noiseScale, 0.005f, 0.2f);

                _noiseSeedX = EditorGUILayout.FloatField(
                    new GUIContent("Seed X", "Смести для нового паттерна"), _noiseSeedX);
                
                _noiseSeedY = EditorGUILayout.FloatField(
                    new GUIContent("Seed Z", "Смести для нового паттерна"), _noiseSeedY);

                if (GUILayout.Button("Рандомизировать Seed", GUILayout.Height(22)))
                {
                    _noiseSeedX = Random.Range(0f, 1000f);
                    _noiseSeedY = Random.Range(0f, 1000f);
                }

                EditorGUILayout.Space(4);
                
                _thresholdLight = EditorGUILayout.Slider(
                    new GUIContent("Порог Светлый (верхний %)",
                        "Всё выше этого порога → светлый материал.\n" +
                        "По умолчанию 0.90 = ~10% кустов"),
                    _thresholdLight, 0.7f, 0.99f);

                _thresholdDark = EditorGUILayout.Slider(
                    new GUIContent("Порог Тёмный (нижний %)",
                        "Выше этого, но ниже Light → тёмный.\n" +
                        "По умолчанию 0.45 = ~45% тёмных, ~45% обычных"),
                    _thresholdDark, 0.1f, 0.89f);
            }
        }

        private void DrawDistributionPreview()
        {
            EditorGUILayout.LabelField("Предполагаемое распределение", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var pLight  = (1f - _thresholdLight) * 100f;
                var pDark   = (_thresholdLight - _thresholdDark) * 100f;
                var pNormal = _thresholdDark * 100f;

                DrawColorBar(Color.green,        $"Обычный\n{pNormal:F0}%", pNormal / 100f);
                GUILayout.Space(4);
                DrawColorBar(new Color(0.3f, 0.5f, 0.1f), $"Тёмный\n{pDark:F0}%", pDark / 100f);
                GUILayout.Space(4);
                DrawColorBar(new Color(0.7f, 1f, 0.4f),   $"Светлый\n{pLight:F0}%", pLight / 100f);
            }
        }

        private static void DrawColorBar(Color c, string label, float fraction)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = c;
                GUILayout.Box("", GUILayout.Height(Mathf.Max(8, fraction * 60)), GUILayout.ExpandWidth(true));
                GUI.backgroundColor = old;
                EditorGUILayout.LabelField(label,
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true },
                    GUILayout.Height(30));
            }
        }

        private void DrawActions()
        {
            var ready = _materialNormal && _materialDark && _materialLight;
            
            if (!ready)
            {
                EditorGUILayout.HelpBox("Назначь все 3 материала, чтобы продолжить.", MessageType.Warning);
            }

            GUI.enabled = ready;
            
            var count = CountBushes();
            var countLabel = count >= 0 ? $"Найдено кустов на сцене: {count}" : "";

            if (count >= 0)
            {
                EditorGUILayout.LabelField(countLabel, EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Применить материалы ко всем кустам", GUILayout.Height(36)))
            {
                Apply();
            }

            GUILayout.Space(4);

            GUI.enabled = true;
            
            if (GUILayout.Button("Отменить (Undo)", GUILayout.Height(24)))
            {
                Undo.PerformUndo();
                SetStatus("Отменено.", Color.yellow);
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMsg))
            {
                return;
            }
            
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = _statusColor },
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            
            EditorGUILayout.LabelField(_statusMsg, style);
        }
        
        //  Основная логика

        private void Apply()
        {
            var bushes = FindObjectsOfType<Bush>();

            if (bushes.Length == 0)
            {
                SetStatus("Кусты (Bush) не найдены на сцене.", Color.red);
                return;
            }

            Undo.SetCurrentGroupName("Bush Material Paint");
            
            var groupId = Undo.GetCurrentGroup();

            int cNormal = 0, cDark = 0, cLight = 0;

            foreach (var bush in bushes)
            {
                var rend = bush.GetComponentInChildren<Renderer>();

                if (rend == null)
                {
                    continue;
                }

                var mat = SampleMaterial(bush.transform.position);

                Undo.RecordObject(rend, "Set Bush Material");
                
                rend.sharedMaterial = mat;

                if (mat == _materialLight)
                {
                    cLight++;
                }
                else if (mat == _materialDark)
                {
                    cDark++;
                }
                else
                {
                    cNormal++;
                }
            }

            Undo.CollapseUndoOperations(groupId);

            var msg = $"Обработано {bushes.Length} кустов: " +
                      $"обычных {cNormal}, тёмных {cDark}, светлых {cLight}.";
            
            SetStatus(msg, new Color(0.2f, 0.9f, 0.3f));
            
            Debug.Log("[BushMaterialPainter] " + msg);
        }
        
        /// <summary>
        /// Сэмплирует Perlin Noise и возвращает материал.
        /// Шум даёт значение [0..1]. Близкие объекты дают близкие значения
        /// соседние кусты образуют цветовые кластеры.
        /// </summary>
        private Material SampleMaterial(Vector3 worldPos)
        {
            var nx = (worldPos.x + _noiseSeedX) * _noiseScale;
            var ny = (worldPos.z + _noiseSeedY) * _noiseScale; // Z — горизонтальная ось

            var noise = Mathf.PerlinNoise(nx, ny); // [0..1]

            if (noise >= _thresholdLight)
            {
                return _materialLight;
            }
            
            return noise >= _thresholdDark ? _materialDark : _materialNormal;
        }

        // Утилиты

        private static int CountBushes()
        {
            var arr = FindObjectsOfType<Bush>();
            return arr?.Length ?? 0;
        }

        private void SetStatus(string msg, Color col)
        {
            _statusMsg   = msg;
            _statusColor = col;
            Repaint();
        }
    }
}
