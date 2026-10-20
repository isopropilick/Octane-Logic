using System;
using System.Collections.Generic;
using System.IO;
using ShapezShifter.Kit;
using UnityEngine;
using ILogger = Core.Logging.ILogger;
using Object = UnityEngine.Object;

namespace OctaneLogic
{
    /// <summary>Loads optional art from beside the mod DLL; never changes building IDs or simulation.</summary>
    internal sealed class GateVisualAssets : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _resourcesDirectory;
        private readonly List<Object> _owned = new List<Object>();
        private bool _disposed;

        public GateVisualAssets(ILogger logger)
        {
            _logger = logger;
            _resourcesDirectory = Path.Combine(ModDirectoryLocator.GetDirectoryLocation<Main>(), "Resources");
        }

        public LOD6Mesh LoadModel(string id, Func<Mesh> createPlaceholder, out Mesh baseMesh)
        {
            string directory = Path.Combine(_resourcesDirectory, "Models", id);
            Mesh? loaded = TryLoadMesh(Path.Combine(directory, "LOD0.fbx"));
            if (loaded == null)
            {
                baseMesh = createPlaceholder();
                _owned.Add(baseMesh);
                _logger.Info?.Log($"Octane Logic: {id} uses placeholder art; add {Path.Combine(directory, "LOD0.fbx")} and restart the game.");
                return MeshLod.Create().AddLod0Mesh(baseMesh).BuildLod6Mesh();
            }

            baseMesh = loaded;
            var meshes = new Mesh[6];
            meshes[0] = baseMesh;
            for (int i = 1; i < meshes.Length; i++)
            {
                // An absent/invalid optional LOD inherits the previous, more detailed mesh.
                meshes[i] = TryLoadMesh(Path.Combine(directory, $"LOD{i}.fbx")) ?? meshes[i - 1];
            }

            // Shifter's builder currently maps slots 3/4/5 to Minimal/Overview/Reduced.
            // Export filenames follow the official art guide: 3=Reduced, 4=Minimal, 5=Overview.
            LOD6Mesh lod = MeshLod.Create().AddLod0Mesh(meshes[0])
                .AddLod1Mesh(meshes[1]).AddLod2Mesh(meshes[2])
                .AddLod3Mesh(meshes[4]).AddLod4Mesh(meshes[5]).AddLod5Mesh(meshes[3])
                .BuildLod6Mesh();
            _logger.Info?.Log($"Octane Logic: loaded {id} model from {directory}.");
            return lod;
        }

        private Mesh? TryLoadMesh(string path)
        {
            if (!File.Exists(path)) return null;
            Mesh? mesh = null;
            try
            {
                // Official sample pipeline: exactly one static mesh per FBX, with its UV channels preserved.
                mesh = FileMeshLoader.LoadSingleMeshFromFile(path);
                ValidateMesh(mesh);
                mesh.name = "OctaneLogic_" + Path.GetFileName(Path.GetDirectoryName(path)) + "_" + Path.GetFileNameWithoutExtension(path);
                _owned.Add(mesh);
                return mesh;
            }
            catch (Exception exception)
            {
                if (mesh != null) Object.Destroy(mesh);
                _logger.Warning?.Log($"Octane Logic: could not load {path}; using fallback. {exception.GetType().Name}: {exception.Message}");
                return null;
            }
        }

        private static void ValidateMesh(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount < 3 || mesh.subMeshCount != 1 || mesh.triangles.Length < 3)
                throw new InvalidDataException("Export exactly one triangulated mesh with one material.");

            foreach (Vector3 vertex in mesh.vertices)
            {
                if (!IsFinite(vertex.x) || !IsFinite(vertex.y) || !IsFinite(vertex.z))
                    throw new InvalidDataException("The model contains non-finite coordinates.");
            }

            ValidateUvs(mesh.uv, mesh.vertexCount, "UV0 (MaterialLUT color)");
            ValidateUvs(mesh.uv2, mesh.vertexCount, "UV1 (tileable details)");
            if (mesh.normals.Length != mesh.vertexCount) mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Bounds bounds = mesh.bounds;
            const float tolerance = 0.02f;
            if (bounds.min.x < -0.5f - tolerance || bounds.max.x > 0.5f + tolerance ||
                bounds.min.z < -0.5f - tolerance || bounds.max.z > 0.5f + tolerance ||
                bounds.min.y < -tolerance)
                throw new InvalidDataException("The model must fit a centered 1 m tile, with its base at y=0. Bake transforms and check export units.");
        }

        private static void ValidateUvs(Vector2[] uvs, int vertexCount, string channel)
        {
            if (uvs.Length != vertexCount)
                throw new InvalidDataException($"Missing {channel}; building models require two UV maps in export order.");
            foreach (Vector2 uv in uvs)
            {
                if (!IsFinite(uv.x) || !IsFinite(uv.y))
                    throw new InvalidDataException($"The model contains non-finite {channel} coordinates.");
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public Sprite LoadIcon(string id, Color placeholderColor)
        {
            string path = Path.Combine(_resourcesDirectory, "Icons", id + ".png");
            var texture = new Texture2D(2, 2) { name = "OctaneLogic_" + id + "_Icon" };
            bool loaded = false;
            if (File.Exists(path))
            {
                try
                {
                    loaded = ImageConversion.LoadImage(texture, File.ReadAllBytes(path));
                    if (!loaded) throw new InvalidDataException("The icon could not be decoded as an image.");
                }
                catch (Exception exception)
                {
                    _logger.Warning?.Log($"Octane Logic: could not load icon {path}; using placeholder. {exception.Message}");
                    Object.Destroy(texture);
                    texture = new Texture2D(2, 2);
                }
            }
            if (!loaded)
            {
                texture.SetPixels(new[] { placeholderColor, placeholderColor, placeholderColor, placeholderColor });
                texture.Apply();
            }
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _owned.Add(texture);
            _owned.Add(sprite);
            return sprite;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Each mesh is owned once even when several LOD slots reuse it.
            for (int i = _owned.Count - 1; i >= 0; i--) Object.Destroy(_owned[i]);
            _owned.Clear();
        }
    }
}
