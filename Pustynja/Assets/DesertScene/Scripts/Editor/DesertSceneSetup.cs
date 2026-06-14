using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DesertScene.EditorTools
{
    public static class DesertSceneSetup
    {
        private const string RootFolder = "Assets/DesertScene";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string TexturesFolder = RootFolder + "/Textures";
        private const string ScenePath = "Assets/Scenes/DesertDemo.unity";

        [MenuItem("Pustynja/Setup Desert Demo Scene")]
        public static void SetupDesertDemoScene()
        {
            EnsureFolders();

            string heightMaskPath = CreateHeightMaskTexture();
            string sandTexturePath = CreateSandTexture();

            Texture2D heightMask = AssetDatabase.LoadAssetAtPath<Texture2D>(heightMaskPath);
            Texture2D sandTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sandTexturePath);

            Material sandMaterial = CreateSandMaterial(sandTexture);
            Material heatMaterial = CreateHeatMaterial();
            Material dustMaterial = CreateDustMaterial();
            Material sandWindMaterial = CreateSandWindMaterial();
            Material footprintMaterial = CreateFootprintMaterial();
            Material skyboxMaterial = CreateSkyboxMaterial();
            Material sunGlareMaterial = CreateSunGlareMaterial();
            VolumeProfile volumeProfile = CreateVolumeProfile();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.75f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.52f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.22f, 0.11f);
            RenderSettings.ambientIntensity = 1.1f;

            Camera playerCamera = CreatePlayer(null, heatMaterial, footprintMaterial);
            Light sun = CreateSun(null);
            CreateSunGlare(null, playerCamera, sun, sunGlareMaterial);
            CreateVolume(null, volumeProfile);
            CreateDust(null, dustMaterial, playerCamera.transform.parent);
            CreateSandWind(null, sandWindMaterial, playerCamera.transform.parent, playerCamera);
            CreateTerrainGenerator(null, heightMask, sandTexture, sandMaterial, playerCamera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = playerCamera.gameObject;
            Debug.Log("Desert demo scene created at " + ScenePath);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "DesertScene");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Textures");
            EnsureFolder(RootFolder, "Shaders");
            EnsureFolder(RootFolder, "Docs");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static string CreateHeightMaskTexture()
        {
            string path = TexturesFolder + "/DefaultHeightMask.png";
            if (File.Exists(path))
                return path;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);

                    float duneWave = Mathf.Sin((u * 6.5f + v * 2.3f) * Mathf.PI);
                    float broadNoise = Mathf.PerlinNoise(u * 3.2f + 4.1f, v * 3.2f + 1.7f);
                    float smallNoise = Mathf.PerlinNoise(u * 13.5f + 8.3f, v * 13.5f + 2.4f);
                    float centerSoftness = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 0.55f);

                    float value = 0.28f + duneWave * 0.18f + broadNoise * 0.38f + smallNoise * 0.08f;
                    value *= centerSoftness;
                    value = Mathf.Clamp01(value);

                    texture.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            ConfigureTexture(path, true, false, TextureWrapMode.Clamp);
            return path;
        }

        private static string CreateSandTexture()
        {
            string path = TexturesFolder + "/ProceduralSand.png";
            if (File.Exists(path))
                return path;

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);

            Color darkSand = new Color(0.63f, 0.43f, 0.2f);
            Color lightSand = new Color(0.96f, 0.74f, 0.38f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float fineNoise = Mathf.PerlinNoise(u * 42f, v * 42f);
                    float softNoise = Mathf.PerlinNoise(u * 8f + 12f, v * 8f + 3f);
                    float ripple = Mathf.Sin((u * 24f + softNoise * 1.5f) * Mathf.PI) * 0.04f;
                    float value = Mathf.Clamp01(0.48f + fineNoise * 0.2f + softNoise * 0.18f + ripple);

                    texture.SetPixel(x, y, Color.Lerp(darkSand, lightSand, value));
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            ConfigureTexture(path, false, true, TextureWrapMode.Repeat);
            return path;
        }

        private static string CreateDustParticleTexture()
        {
            string path = TexturesFolder + "/SandDustDot.png";
            if (File.Exists(path))
                return path;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                    float distance = Vector2.Distance(uv, new Vector2(0.5f, 0.5f));
                    float softDot = Mathf.SmoothStep(0.5f, 0.08f, distance);
                    float grain = Mathf.PerlinNoise(x * 0.34f, y * 0.34f);
                    float alpha = softDot * Mathf.Lerp(0.45f, 1f, grain);
                    Color color = new Color(0.88f, 0.63f, 0.32f, alpha);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            ConfigureTexture(path, false, true, TextureWrapMode.Clamp);
            return path;
        }

        private static void ConfigureTexture(string path, bool readable, bool srgb, TextureWrapMode wrapMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.isReadable = readable;
            importer.sRGBTexture = srgb;
            importer.wrapMode = wrapMode;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Material CreateSandMaterial(Texture2D sandTexture)
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/DesertSand.mat", "Pustynja/WorldSpaceSand");
            material.SetColor("_BaseColor", new Color(0.86f, 0.58f, 0.27f, 1f));
            material.SetTexture("_BaseMap", sandTexture);
            material.SetTextureScale("_BaseMap", new Vector2(12f, 12f));
            material.SetFloat("_WorldTextureScale", 0.2f);
            material.SetFloat("_NormalStrength", 0.45f);
            material.SetFloat("_AmbientStrength", 0.42f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateHeatMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/HeatHazeOverlay.mat", "Pustynja/HeatHazeOverlay");
            material.SetFloat("_Strength", 0.0014f);
            material.SetFloat("_Speed", 0.75f);
            material.SetFloat("_Scale", 22f);
            material.SetFloat("_Opacity", 0.24f);
            material.SetColor("_WarmTint", new Color(1f, 0.72f, 0.42f, 0.25f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateDustMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/DustParticle.mat", "Universal Render Pipeline/Particles/Unlit");
            Texture2D dustTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CreateDustParticleTexture());
            material.SetTexture("_BaseMap", dustTexture);
            material.SetColor("_BaseColor", new Color(0.9f, 0.68f, 0.4f, 0.48f));
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSandWindMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/SandWindParticle.mat", "Universal Render Pipeline/Particles/Unlit");
            Texture2D dustTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CreateDustParticleTexture());
            material.SetTexture("_BaseMap", dustTexture);
            material.SetColor("_BaseColor", new Color(1f, 0.82f, 0.52f, 0.18f));
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateFootprintMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/FootprintMark.mat", "Pustynja/FootprintMark");
            material.SetColor("_Color", new Color(0.12f, 0.065f, 0.025f, 0.62f));
            material.SetFloat("_SoftEdge", 0.24f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSkyboxMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/DesertSkybox.mat", "Skybox/Procedural");
            material.SetColor("_SkyTint", new Color(0.98f, 0.64f, 0.34f, 1f));
            material.SetColor("_GroundColor", new Color(0.55f, 0.33f, 0.14f, 1f));
            material.SetFloat("_AtmosphereThickness", 1.25f);
            material.SetFloat("_Exposure", 1.25f);
            material.SetFloat("_SunSize", 0.08f);
            material.SetFloat("_SunSizeConvergence", 4f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSunGlareMaterial()
        {
            Material material = LoadOrCreateMaterial(MaterialsFolder + "/SunGlare.mat", "Pustynja/SunGlare");
            material.SetColor("_Color", new Color(1f, 0.73f, 0.3f, 0.75f));
            material.SetColor("_RingColor", new Color(1f, 0.43f, 0.15f, 0.35f));
            material.SetFloat("_Intensity", 1.45f);
            material.SetFloat("_Radius", 0.2f);
            material.SetFloat("_Softness", 0.42f);
            material.SetFloat("_RingRadius", 0.32f);
            material.SetFloat("_RingWidth", 0.035f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            return material;
        }

        private static VolumeProfile CreateVolumeProfile()
        {
            string path = RootFolder + "/HotDesertVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0.22f);
            color.contrast.Override(18f);
            color.saturation.Override(8f);
            color.colorFilter.Override(new Color(1f, 0.78f, 0.55f, 1f));

            WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
            whiteBalance.temperature.Override(38f);
            whiteBalance.tint.Override(8f);

            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.42f);
            bloom.scatter.Override(0.58f);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.42f, 0.22f, 0.08f));

            ChromaticAberration chromatic = GetOrAdd<ChromaticAberration>(profile);
            chromatic.intensity.Override(0.04f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
                component = profile.Add<T>(true);

            component.active = true;
            return component;
        }

        private static Light CreateSun(Transform parent)
        {
            GameObject sunObject = new GameObject("Desert Sun");
            if (parent != null)
                sunObject.transform.SetParent(parent);

            Vector3 visibleSunDirection = new Vector3(0.25f, 0.45f, 0.86f).normalized;
            sunObject.transform.rotation = Quaternion.LookRotation(-visibleSunDirection, Vector3.up);

            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.78f, 0.47f);
            sun.intensity = 4.2f;
            sun.shadows = LightShadows.Soft;
            sun.useColorTemperature = true;
            sun.colorTemperature = 4300f;
            RenderSettings.sun = sun;

            return sun;
        }

        private static Camera CreatePlayer(Transform parent, Material heatMaterial, Material footprintMaterial)
        {
            GameObject playerObject = new GameObject("Desert FPS Player");
            if (parent != null)
                playerObject.transform.SetParent(parent);
            playerObject.transform.position = new Vector3(0f, 11f, -13f);

            CharacterController characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 50f;

            SimpleDesertFpsController fpsController = playerObject.AddComponent<SimpleDesertFpsController>();
            fpsController.walkSpeed = 5.4f;
            fpsController.jumpHeight = 1.45f;
            fpsController.gravity = -18f;

            DesertFootprintTrail footprintTrail = playerObject.AddComponent<DesertFootprintTrail>();
            footprintTrail.footprintMaterial = footprintMaterial;
            footprintTrail.stepDistance = 0.85f;
            footprintTrail.minMoveSpeed = 0.15f;
            footprintTrail.footprintSize = 0.5f;
            footprintTrail.landingFootprintSize = 0.68f;
            footprintTrail.yOffset = 0.07f;
            footprintTrail.maxFootprints = 90;

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 500f;
            camera.clearFlags = CameraClearFlags.Skybox;

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.requiresColorOption = CameraOverrideOption.On;
            cameraData.requiresDepthOption = CameraOverrideOption.On;

            cameraObject.AddComponent<AudioListener>();

            DesertHeatHazeOverlay heatOverlay = cameraObject.AddComponent<DesertHeatHazeOverlay>();
            heatOverlay.heatMaterial = heatMaterial;
            heatOverlay.distortionStrength = 0.0014f;
            heatOverlay.shimmerSpeed = 0.75f;
            heatOverlay.shimmerScale = 22f;
            heatOverlay.opacity = 0.24f;

            fpsController.playerCamera = camera;
            return camera;
        }

        private static void CreateTerrainGenerator(
            Transform parent,
            Texture2D heightMask,
            Texture2D sandTexture,
            Material sandMaterial,
            Camera playerCamera)
        {
            GameObject generatorObject = new GameObject("TerrainGenerator");
            if (parent != null)
                generatorObject.transform.SetParent(parent);

            DesertTerrainGenerator generator = generatorObject.AddComponent<DesertTerrainGenerator>();
            generator.grayscaleSourceImage = heightMask;
            generator.terrainSize = new Vector2(60f, 60f);
            generator.heightMultiplier = 9f;
            generator.resolution = 42;
            generator.isoLevel = 0f;
            generator.makeEdgesTileable = true;
            generator.desertMaterial = sandMaterial;
            generator.sandTexture = sandTexture;
            generator.sandTextureTiling = new Vector2(12f, 12f);
            generator.generateCollider = true;
            generator.clearOldGeneratedTerrain = true;
            generator.GenerateTerrain();

            InfiniteDesertTerrainStreamer streamer = generatorObject.AddComponent<InfiniteDesertTerrainStreamer>();
            streamer.target = playerCamera.transform.parent;
            streamer.targetCamera = playerCamera;
            streamer.sourceTile = generatorObject.GetComponentInChildren<GeneratedDesertTerrain>(true);
            streamer.tileSize = generator.terrainSize;
            streamer.safetyRadius = 2;
            streamer.forwardTiles = 8;
            streamer.sideTiles = 6;
            streamer.generateColliders = true;
            streamer.RefreshNow();
        }

        private static void CreateVolume(Transform parent, VolumeProfile profile)
        {
            GameObject volumeObject = new GameObject("Hot Desert Global Volume");
            if (parent != null)
                volumeObject.transform.SetParent(parent);

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static void CreateDust(Transform parent, Material dustMaterial, Transform target)
        {
            GameObject dustObject = new GameObject("Sand Dust Atmosphere");
            if (parent != null)
                dustObject.transform.SetParent(parent);
            dustObject.transform.position = new Vector3(0f, 4f, 0f);

            ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.085f);
            main.startColor = new Color(0.9f, 0.68f, 0.4f, 0.48f);
            main.maxParticles = 4200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            DesertDustFollower follower = dustObject.AddComponent<DesertDustFollower>();
            follower.target = target;
            follower.fixedWorldHeight = 4f;
            follower.smoothTime = 0.02f;
            follower.SnapToTarget();

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 620f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(96f, 6f, 96f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0.55f, 2.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.22f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.42f;
            noise.scrollSpeed = 0.8f;

            ParticleSystemRenderer renderer = dustObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = dustMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0.0005f;
            renderer.maxParticleSize = 0.035f;
            renderer.sortingFudge = 4f;

            particles.Play();
        }

        private static void CreateSandWind(Transform parent, Material windMaterial, Transform target, Camera targetCamera)
        {
            GameObject windObject = new GameObject("Sand Wind Trails");
            if (parent != null)
                windObject.transform.SetParent(parent);

            ParticleSystem particles = windObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = windObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = windMaterial;

            DesertSandWindEffect wind = windObject.AddComponent<DesertSandWindEffect>();
            wind.target = target;
            wind.targetCamera = targetCamera;
            wind.windMaterial = windMaterial;
            wind.windDirection = new Vector3(1f, 0f, 0.35f);
            wind.aheadDistance = 18f;
            wind.heightOffset = 1.4f;
            wind.emitterSize = new Vector3(34f, 2.8f, 18f);
            wind.ApplySettings();
            wind.SnapToTarget();

            particles.Play();
        }

        private static void CreateSunGlare(Transform parent, Camera targetCamera, Light sun, Material material)
        {
            GameObject glareObject = new GameObject("Layered Lens Flare");
            if (parent != null)
                glareObject.transform.SetParent(parent);

            Vector3 sunDirection = -sun.transform.forward;
            glareObject.transform.position = sunDirection.normalized * 140f;

            DesertLensFlare flare = glareObject.AddComponent<DesertLensFlare>();
            flare.targetCamera = targetCamera;
            flare.sunTransform = sun.transform;
            flare.flareMaterial = material;
            flare.BuildFlare();
        }
    }
}
