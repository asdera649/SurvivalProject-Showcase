using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
#if URP
using UnityEngine.Rendering.Universal;
#endif

namespace SP.Runtime.Core.Systems.GroundColorMapRendering
{
    public static class GroundColorMapRendering
    {
        #region Structs

        private struct TerrainState
        {
            public Terrain Terrain;
            public bool DrawInstanced;
            public float BasemapDistance;
            public bool RenderFoliage;
        }    

        #endregion
    
        private static readonly Dictionary<GameObject, TerrainState> _originalTerrainStates = new ();
    
        private static List<Light> _directionalLights;
        private static readonly Dictionary<Light, bool> _originalLightStates = new ();
        private static AmbientMode _ambientMode;
        private static float _reflectionIntensity;
        private static DefaultReflectionMode _defaultReflectionMode;
        private static bool _fogEnabled;
        private static Color _ambientColor;
    
        private static bool _copyBuffer => Application.isPlaying == false;
    
        private const float CLIP_PADDING = 5f;
        private const float HEIGHT_OFFSET = 1000f;
        private static readonly Color _backgroundColor = Color.red;
    
        private static UnityEngine.Camera _renderCam;
        private static RenderTexture _renderTarget;
        private static Texture2D _bakedTexture;
    
        public static void Render(GroundColorMapRenderer renderer)
        {
            if (!_copyBuffer)
            {
                return;
            }
        
            if (renderer.ColorMapBounds.size == Vector3.zero)
            {
                renderer.ColorMapBounds = GetTerrainBounds(renderer.GroundObjects);
            }
        
            renderer.ColorMapUv = BoundsToUV(renderer.ColorMapBounds);
            
            SetupTerrains(renderer);
            SetupRenderer(renderer);
            SetupLighting(renderer);
            
            RenderToTexture(renderer);
        
            RestoreLighting();
            RestoreRenderer();
            RestoreTerrains(renderer);
        
            Save();
        }
    
        private static void SetupTerrains(GroundColorMapRenderer renderer)
        {
            _originalTerrainStates.Clear();
        
            foreach (var item in renderer.GroundObjects)
            {
                if (item == null)
                {
                    continue;
                }
            
                var terrain = item.GetComponent<Terrain>();
            
                var state = new TerrainState
                {
                    Terrain = terrain
                };
            
                if (terrain)
                {
                    state.DrawInstanced = terrain.drawInstanced;
                    state.BasemapDistance = terrain.basemapDistance;
                    state.RenderFoliage = terrain.drawTreesAndFoliage;
                }
            
                _originalTerrainStates.Add(item, state);
            
                item.transform.position += Vector3.up * HEIGHT_OFFSET;

                if (terrain)
                {
                    terrain.drawInstanced = false;

                    terrain.basemapDistance = 99999;
                
                    terrain.drawTreesAndFoliage = false;
                }
            }
        }
    
        private static void SetupRenderer(GroundColorMapRenderer renderer)
        {
            if (_renderCam)
            {
                Object.DestroyImmediate(_renderCam.gameObject);
            }
        
            _renderCam = new GameObject().AddComponent<UnityEngine.Camera>();
        
            _renderCam.name = "ColorMapRenderCamera(TEMP)";
            _renderCam.enabled = false;
            _renderCam.hideFlags = HideFlags.HideAndDontSave;
        
            _renderCam.orthographic = true;
            _renderCam.orthographicSize = Mathf.Max(
                renderer.ColorMapBounds.extents.x,
                renderer.ColorMapBounds.extents.z);
        
            _renderCam.nearClipPlane = 0.001f;
            _renderCam.farClipPlane = renderer.ColorMapBounds.size.y + CLIP_PADDING;
        
            _renderCam.cullingMask = renderer.CullingMask;
        
            _renderCam.clearFlags = CameraClearFlags.Color;
            _renderCam.backgroundColor = _backgroundColor;
        
            _renderCam.transform.position = renderer.ColorMapBounds.center + Vector3.up *
                (renderer.ColorMapBounds.extents.y + CLIP_PADDING + HEIGHT_OFFSET);
        
            _renderCam.transform.localEulerAngles = Vector3.right * 90f;

#if URP
        var camData = _renderCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderShadows = false;
        camData.renderPostProcessing = false;
        camData.antialiasing = AntialiasingMode.None;
        camData.requiresColorOption = CameraOverrideOption.Off;
        camData.requiresDepthOption = CameraOverrideOption.Off;
        camData.requiresColorTexture = false;
        camData.requiresDepthTexture = false;
#endif
        }
    
        private static void SetupLighting(GroundColorMapRenderer renderer)
        {
            //Setup faux albedo lighting
#if UNITY_2023_2_OR_NEWER
        var lights = (Light[])Object.FindObjectsByType(typeof(Light), FindObjectsSortMode.None);
#else
            var lights = (Light[])Object.FindObjectsOfType(typeof(Light));
#endif
            
            _directionalLights = new List<Light>();
            _originalLightStates.Clear();
            
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    _originalLightStates.Add(l, l.enabled);
                
                    l.enabled = false;
                    
                    _directionalLights.Add(l);
                }
            }

            _ambientMode = RenderSettings.ambientMode;
            _ambientColor = RenderSettings.ambientLight;
            _reflectionIntensity = RenderSettings.reflectionIntensity;
            _defaultReflectionMode = RenderSettings.defaultReflectionMode;
            _fogEnabled = RenderSettings.fog;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.fog = false;
        }
    
        private static void RenderToTexture(GroundColorMapRenderer renderer)
        {
            if (!_renderCam)
            {
                Debug.LogError("Renderer does not have a render cam set up");
                return;
            }

            if (_renderTarget)
            {
                RenderTexture.ReleaseTemporary(_renderTarget);
            }
        
            _renderTarget = RenderTexture.GetTemporary(
                renderer.ColorMapResolution,
                renderer.ColorMapResolution,
                8,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
        
            _renderCam.targetTexture = _renderTarget;
            RenderTexture.active = _renderTarget;
        
            _renderCam.Render();

            Graphics.SetRenderTarget(_renderTarget);

            _bakedTexture = new Texture2D(
                renderer.ColorMapResolution,
                renderer.ColorMapResolution, 
                TextureFormat.ARGB32, 
                false, 
                true);

            _bakedTexture.ReadPixels(new Rect(
                0,
                0,
                renderer.ColorMapResolution, 
                renderer.ColorMapResolution), 0, 0);
            
            _bakedTexture.Apply();
            
            //Cleanup
            _renderCam.targetTexture = null;
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(_renderTarget);
        }
    
        private static void RestoreLighting()
        {
            foreach (var light in _directionalLights)
            {
                if (_originalLightStates.TryGetValue(light, out var state))
                {
                    light.enabled = state;
                }
            }
        
            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientLight = _ambientColor;
            RenderSettings.defaultReflectionMode = _defaultReflectionMode;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
            RenderSettings.fog = _fogEnabled;
        }
    
        private static void RestoreRenderer()
        {
            Object.DestroyImmediate(_renderCam.gameObject);
            _renderCam = null;
        }
    
        private static void RestoreTerrains(GroundColorMapRenderer renderer)
        {
            foreach (var item in renderer.GroundObjects)
            {
                if (item == null)
                {
                    continue;
                }
            
                _originalTerrainStates.TryGetValue(item, out var state);

                item.transform.position += Vector3.down * HEIGHT_OFFSET;

                if (state.Terrain)
                {
                    state.Terrain.drawInstanced = state.DrawInstanced;
                    state.Terrain.drawTreesAndFoliage = state.RenderFoliage;

                    state.Terrain.basemapDistance = state.BasemapDistance;
                }
            }
        }

        private static void Save()
        {
#if UNITY_EDITOR
            if (_bakedTexture == null)
            {
                Debug.LogError("Backed texture is null!");
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            var fileName = $"GroundColorMap_{timestamp}.png";
        
            var relativePath = "Assets/" + fileName;
            var absolutePath = Path.Combine(Application.dataPath, fileName);
        
            var textureBytes = _bakedTexture.EncodeToPNG();
        
            File.WriteAllBytes(absolutePath, textureBytes);
        
            Object.DestroyImmediate(_bakedTexture);
        
            AssetDatabase.Refresh();
        
            var tImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        
            if (tImporter != null)
            {
                tImporter.wrapMode = TextureWrapMode.Clamp;
                tImporter.sRGBTexture = false;
                tImporter.maxTextureSize = 4096;
            
                tImporter.SaveAndReimport();
            }

            Debug.Log($"Texture is preserved: {relativePath}");
#endif
        }

        #region Utilities

        public static Bounds GetTerrainBounds(IEnumerable<GameObject> objects)
        {
            var minSum = Vector3.one * Mathf.Infinity;
            var maxSum = Vector3.one * Mathf.NegativeInfinity;
            var min = Vector3.zero;
            var max = Vector3.zero;
        
            foreach (var item in objects)
            {
                if (item == null)
                {
                    continue;
                }

                var t = item.GetComponent<Terrain>();
                var r = t ? null : item.GetComponent<MeshRenderer>();

                if (t)
                {
                    //Min/max bounds corners in world-space
                    min = t.GetPosition(); //Doesn't exactly represent the minimum bounds value, but doesn't have to be
                    max = t.GetPosition() + t.terrainData.size; //Note, size is slightly more correct in height than bounds
                }

                if (r)
                {
                    //World-space bounds corners
                    min = r.bounds.min;
                    max = r.bounds.max;
                }
            
                minSum = Vector3.Min(minSum, min);
            
                //Must handle each axis separately, terrain may be further away, but not necessarily higher
                maxSum.x = Mathf.Max(maxSum.x, max.x);
                maxSum.y = Mathf.Max(maxSum.y, max.y);
                maxSum.z = Mathf.Max(maxSum.z, max.z);
            }

            var b = new Bounds(Vector3.zero, Vector3.zero);

            b.SetMinMax(minSum, maxSum);

            //Increase bounds height for flat terrains by 1 unit up and down
            if (b.size.y < 2f)
            {
                b.Encapsulate(b.center + Vector3.up);
                b.Encapsulate(b.center + Vector3.down);
            }

            //Ensure bounds is always square
            b.size = new Vector3(Mathf.Max(b.size.x, b.size.z), b.size.y, Mathf.Max(b.size.x, b.size.z));
            b.center = Vector3.Lerp(b.min, b.max, 0.5f);

            return b;
        }
     
        public static Vector4 BoundsToUV(Bounds b)
        {
            var uv = new Vector4
            {
                //Origin position
                x = b.min.x,
                y = b.min.z,
                //Scale factor
                z = 1f / b.size.x,
                w = 0f
            };

            return uv;
        }

        #endregion
    }
}
