using UnityEngine;

/// <summary>
/// Thư viện Material dùng Unity primitives để prototype game không cần asset 3D.
/// Tất cả objects trong game đều lấy material từ đây.
/// </summary>
public static class MaterialLibrary
{
    // ─── Nhân vật ──────────────────────────────────────────────────────────────
    public static Material Player()   => Textured("Textures/kimdong_clothes", Color.white);
    public static Material Enemy()    => Textured("Textures/soldier_uniform", Color.white);
    public static Material Escort()   => Textured("Textures/escort_clothes", Color.white);

    // ─── Môi trường ────────────────────────────────────────────────────────────
    public static Material Ground()   => Textured("Textures/grass_texture", new Color(0.85f, 0.85f, 0.85f), 10f, 10f);
    public static Material Bush()     => Textured("Textures/leaves_texture", new Color(0.6f, 0.9f, 0.6f), 1f, 1f);
    public static Material Water()    => Transparent(new Color(0.15f, 0.45f, 0.75f, 0.65f));
    public static Material Rock()     => Textured("Textures/rock_texture", Color.white, 2f, 2f);
    public static Material Tree()     => Textured("Textures/leaves_texture", Color.white, 2f, 2f);
    public static Material TreeTrunk()=> Textured("Textures/bark_texture", Color.white, 1f, 3f);
    public static Material Wall()     => Textured("Textures/mud_wall_texture", Color.white, 4f, 1f);

    // ─── Props ─────────────────────────────────────────────────────────────────
    public static Material Letter()   => Textured("Textures/letter_texture", Color.white);
    public static Material Checkpoint()=> Textured("Textures/flag_texture", Color.white);
    public static Material Waypoint() => Transparent(new Color(1f, 0.5f, 0f, 0.4f));

    // ─── UI / Đặc biệt ─────────────────────────────────────────────────────────
    public static Material DetectionZoneRed()    => Transparent(new Color(1f, 0f, 0f, 0.15f));
    public static Material DetectionZoneYellow() => Transparent(new Color(1f, 1f, 0f, 0.1f));
    public static Material SoundRing()           => Transparent(new Color(0f, 0.9f, 1f, 0.5f));

    // ─── Nâng cấp UI & Môi trường ──────────────────────────────────────────────
    public static Material WoodPlank()           => Textured("Textures/bark_texture", new Color(0.7f, 0.6f, 0.5f), 2f, 2f);
    public static Material RifleMetal()          => Solid(new Color(0.15f, 0.15f, 0.18f));
    public static Material RifleWood()           => Solid(new Color(0.4f, 0.25f, 0.1f));
    public static Material LanternGlow()         => Emissive(new Color(1f, 0.85f, 0.2f), 1.5f);
    public static Material NonLaStraw()          => Textured("Textures/mud_wall_texture", new Color(1f, 0.92f, 0.7f), 2f, 2f);
    public static Material ThatchRoof()          => Textured("Textures/mud_wall_texture", new Color(0.75f, 0.65f, 0.45f), 3f, 3f);
    public static Material MountainRock()        => Textured("Textures/rock_texture", new Color(0.45f, 0.45f, 0.45f), 5f, 5f);
    public static Material WoodLog()             => Textured("Textures/bark_texture", new Color(0.35f, 0.25f, 0.15f), 1f, 4f);

    // ─── Helper factories ──────────────────────────────────────────────────────
    public static Material Solid(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    public static Material Transparent(Color color)
    {
        Material mat = Solid(color);
        // URP transparent
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.renderQueue = 3000;
        mat.color = color;
        return mat;
    }

    public static Material Emissive(Color color, float intensity)
    {
        Material mat = Solid(color);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
        return mat;
    }

    public static Material Textured(string texturePath, Color color, float tilingX = 1f, float tilingY = 1f)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard"));
        mat.color = color;
        Texture2D tex = Resources.Load<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", new Vector2(tilingX, tilingY));
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                mat.SetTextureScale("_MainTex", new Vector2(tilingX, tilingY));
            }
        }
        else
        {
            Debug.LogWarning($"[MaterialLibrary] Texture not found at Resources/{texturePath}");
        }
        return mat;
    }
}
