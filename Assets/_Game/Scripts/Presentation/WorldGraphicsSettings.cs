using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IanGame
{
    public static class WorldGraphicsSettings
    {
        const string Prefix = "ian.graphics.";
        public static readonly string[] Names = { "Low", "Medium", "High", "Ultra", "Custom" };
        static readonly int[] FrameLimits = { 30, 45, 60, 120 };
        static readonly float[] Scales = { .75f, .875f, 1, 1 };
        static UniversalRenderPipelineAsset runtime;
        static UniversalRenderPipelineAsset originalOverride;
        static Camera bound;
        static bool originalPost;
        static int preset;
        static int detail;
        static int density;
        static int shadows;
        static int pacing;
        static int scale;
        static bool smooth;
        public static int Preset => preset;
        public static int Detail => detail;
        public static int Crowd => density;
        public static int Shadows => shadows;
        public static int FrameLimit => pacing == 4 ? 0 : FrameLimits[pacing];
        public static float RenderScale => Scales[scale];
        public static bool Smooth => smooth;
        public static bool HasPipeline => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            int fallback = Application.isMobilePlatform
                ? (PlayerPrefs.GetInt("ian.mobile.lowDetail", 0) == 1 ? 0 : 1)
                : 2;
            preset = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "preset", fallback), 0, 4);
            int tier = Mathf.Min(3, preset);
            detail = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "detail", tier), 0, 3);
            shadows = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "shadows", tier), 0, 3);
            density = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "density", tier), 0, 3);
            scale = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "scale", tier), 0, 3);
            pacing = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "pacing", tier), 0, Application.isMobilePlatform ? 3 : 4);
            smooth = PlayerPrefs.GetInt(Prefix + "smooth", 0) == 1;
            Application.quitting -= RestorePipeline;
            Application.quitting += RestorePipeline;
            Apply();
            if (!Application.isEditor && !Application.isMobilePlatform && PlayerPrefs.HasKey(Prefix + "screenWidth"))
            {
                var mode = PlayerPrefs.GetInt(Prefix + "window", 0) == 1
                    ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
                int w = PlayerPrefs.GetInt(Prefix + "screenWidth");
                int h = PlayerPrefs.GetInt(Prefix + "screenHeight");
                if (w >= 640 && h >= 360 && w <= Display.main.systemWidth && h <= Display.main.systemHeight)
                {
                    Screen.SetResolution(w, h, mode);
                }
            }
        }

        public static void ChangeDisplay(bool changeMode)
        {
            bool window = Screen.fullScreenMode == FullScreenMode.Windowed;
            int width = Screen.width;
            int height = Screen.height;
            if (changeMode)
            {
                window = !window;
            }
            else
            {
                var modes = Screen.resolutions;
                if (modes.Length == 0)
                {
                    return;
                }

                var next = System.Array.Find(modes, m => m.width > width || m.width == width && m.height > height);
                if (next.width == 0)
                {
                    next = modes[0];
                }

                width = next.width;
                height = next.height;
            }

            Screen.SetResolution(width, height, window ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow);
            PlayerPrefs.SetInt(Prefix + "screenWidth", width);
            PlayerPrefs.SetInt(Prefix + "screenHeight", height);
            PlayerPrefs.SetInt(Prefix + "window", window ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Bind(Camera camera)
        {
            if (bound != camera)
            {
                bound = camera;
                originalPost = camera && camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)
                    && data.renderPostProcessing;
            }

            ApplyCamera();
        }

        public static void Select(int value)
        {
            preset = Mathf.Clamp(value, 0, 4);
            if (preset < 4)
            {
                detail = preset;
                density = preset;
                pacing = preset;
                scale = preset;
                shadows = preset;
                smooth = false;
            }

            Save();
        }

        public static void NextDetail()
        {
            SetOption(ref detail, 4);
        }

        public static void NextCrowd()
        {
            SetOption(ref density, 4);
        }

        public static void NextFrameLimit()
        {
            SetOption(ref pacing, Application.isMobilePlatform ? 4 : 5);
        }

        public static void NextScale()
        {
            scale = Mathf.Min(2, scale);
            SetOption(ref scale, 3);
        }

        public static void NextShadows()
        {
            SetOption(ref shadows, 4);
        }

        public static void ToggleAA()
        {
            smooth = !smooth;
            preset = 4;
            Save();
        }

        static void SetOption(ref int option, int count)
        {
            option = (option + 1) % count;
            preset = 4;
            Save();
        }

        static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "preset", preset);
            PlayerPrefs.SetInt(Prefix + "detail", detail);
            PlayerPrefs.SetInt(Prefix + "shadows", shadows);
            PlayerPrefs.SetInt(Prefix + "density", density);
            PlayerPrefs.SetInt(Prefix + "pacing", pacing);
            PlayerPrefs.SetInt(Prefix + "scale", scale);
            PlayerPrefs.SetInt(Prefix + "smooth", smooth ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        static void Apply()
        {
            Revision++;
            QualitySettings.vSyncCount = pacing == 4 ? 1 : 0;
            Application.targetFrameRate = pacing == 4 ? -1 : FrameLimit;
            if (!runtime && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset)
            {
                originalOverride = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                runtime = Object.Instantiate(asset);
                runtime.hideFlags = HideFlags.DontSave;
                QualitySettings.renderPipeline = runtime;
            }

            if (runtime)
            {
                runtime.upscalingFilter = UpscalingFilterSelection.Point;
                runtime.renderScale = RenderScale;
            }

            ApplyCamera();
        }

        static void ApplyCamera()
        {
            if (bound && bound.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                data.renderPostProcessing = originalPost || smooth;
                data.antialiasing = smooth ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
            }
        }

        static void RestorePipeline()
        {
            if (runtime)
            {
                QualitySettings.renderPipeline = originalOverride;
                Object.Destroy(runtime);
                runtime = null;
            }
        }
    }
}
