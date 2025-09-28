using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Terrain : Node3D
{
    [ExportGroup("Dimensions")]
    [Export] public int TerrainWidth = 100;
    [Export] public int TerrainDepth = 100;
    [Export(PropertyHint.Range, "2, 512")] public int Resolution = 128;

    [ExportGroup("Generation")]
    [Export] public TerrainLayer[] Layers { get; set; } = [];

    [ExportGroup("Materials")]
    [Export] public ShaderMaterial SplatmapMaterial { get; set; }
    [Export] public TerrainMaterial[] Materials { get; set; } = new TerrainMaterial[MAX_TEXTURES];

    private bool _generate;
    [Export]
    public bool Generate
    {
        get => _generate;
        set
        {
            _generate = value;
            if (_generate)
            {
                GenerateTerrain();
            }
        }
    }

    private MeshInstance3D _meshInstance;
    private const int MAX_TEXTURES = 4;

    public void GenerateTerrain()
    {
        if (Layers == null || Layers.Length == 0)
        {
            GD.PrintErr("No terrain layers assigned. Cannot generate terrain.");
            return;
        }

        _meshInstance = GetNodeOrNull<MeshInstance3D>("TerrainMesh");
        if (_meshInstance == null)
        {
            _meshInstance = new MeshInstance3D();
            _meshInstance.Name = "TerrainMesh";
            AddChild(_meshInstance);
        }

        var terrainData = new TerrainData(Resolution, MAX_TEXTURES);
        foreach (var layer in Layers)
        {
            if (layer != null)
            {
                layer.Apply(terrainData, Resolution);
            }
        }

        NormalizeSplatmap(terrainData);
        BuildMesh(terrainData);

        GD.Print("Terrain generated successfully.");
    }

    private void NormalizeSplatmap(TerrainData data)
    {
        for (int z = 0; z < Resolution; z++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float totalStrength = 0;
                for (int i = 0; i < MAX_TEXTURES; i++)
                {
                    totalStrength += data.Splatmap[x, z, i];
                }

                if (totalStrength > 0)
                {
                    for (int i = 0; i < MAX_TEXTURES; i++)
                    {
                        data.Splatmap[x, z, i] /= totalStrength;
                    }
                }
                else
                {
                    data.Splatmap[x, z, 0] = 1.0f;
                }
            }
        }
    }

    private void BuildMesh(TerrainData data)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        st.SetColor(new Color(1, 1, 1));
        st.SetUV(new Vector2(0, 0));

        for (int z = 0; z < Resolution; z++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                float xPos = (float)x / (Resolution - 1) * TerrainWidth;
                float zPos = (float)z / (Resolution - 1) * TerrainDepth;
                float yPos = data.Heights[x, z];

                Vector2 uv = new Vector2((float)x / (Resolution - 1), (float)z / (Resolution - 1));
                Color splatColor = new Color(
                    data.Splatmap[x, z, 0],
                    data.Splatmap[x, z, 1],
                    data.Splatmap[x, z, 2],
                    data.Splatmap[x, z, 3]
                );

                st.SetUV(uv);
                st.SetColor(splatColor);
                st.AddVertex(new Vector3(xPos, yPos, zPos));
            }
        }

        for (int z = 0; z < Resolution - 1; z++)
        {
            for (int x = 0; x < Resolution - 1; x++)
            {
                int topLeft = z * Resolution + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * Resolution + x;
                int bottomRight = bottomLeft + 1;

                st.AddIndex(topLeft);
                st.AddIndex(topRight);
                st.AddIndex(bottomLeft);

                st.AddIndex(topRight);
                st.AddIndex(bottomRight);
                st.AddIndex(bottomLeft);
            }
        }

        st.GenerateNormals();
        st.GenerateTangents();

        var arrayMesh = st.Commit();
        _meshInstance.Mesh = arrayMesh;

        _meshInstance.MaterialOverride = SplatmapMaterial;

        if (SplatmapMaterial != null)
        {
            var albedoTextures = new Godot.Collections.Array<Texture2D>();
            var normalTextures = new Godot.Collections.Array<Texture2D>();
            var roughnessTextures = new Godot.Collections.Array<Texture2D>();

            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                if (Materials[i] != null)
                {
                    albedoTextures.Add(Materials[i].AlbedoTexture);
                    normalTextures.Add(Materials[i].NormalTexture);
                    roughnessTextures.Add(Materials[i].RoughnessTexture);
                }
                else
                {
                    albedoTextures.Add(null);
                    normalTextures.Add(null);
                    roughnessTextures.Add(null);
                }
            }

            SplatmapMaterial.SetShaderParameter("albedo_textures", albedoTextures);
            SplatmapMaterial.SetShaderParameter("normal_textures", normalTextures);
            SplatmapMaterial.SetShaderParameter("roughness_textures", roughnessTextures);
        }
    }
}