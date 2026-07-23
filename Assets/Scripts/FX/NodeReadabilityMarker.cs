using UnityEngine;

/// <summary>
/// Makes a resource node legible as a minable deposit (Wave C readability).
///
/// The generic art backfill hides a node's authored sphere and fits a
/// Prop_Crate placeholder to it — but for nodes that placeholder collapses to a
/// ~0.14 m speck, so a node ended up rendering as nothing but its dark plinth. A
/// first-person playtest reported exactly that: "can't even tell what's a
/// minable node." Rather than fight the shared fitter (machines and crates fit
/// fine), this adds its own unmistakable geometry: a small cluster of
/// resource-coloured emissive shards on the plinth.
///
/// Emissive on purpose. Machines in this project are lit, not emissive, so a
/// self-glowing cluster reads as "collectible resource" against them, and it
/// stays findable in F8's dark corridors. Colour follows the existing per-type
/// language (scrap amber, energy yellow, circuits blue).
/// </summary>
public class NodeReadabilityMarker : MonoBehaviour
{
    public ResourceTypeId resourceType = ResourceTypeId.ScrapMetal;

    static Material _shardMatBase;

    void Start()
    {
        Build();
    }

    static Color ResourceColour(ResourceTypeId t) => t switch
    {
        ResourceTypeId.EnergyCells       => new Color(0.98f, 0.85f, 0.28f),
        ResourceTypeId.CircuitComponents => new Color(0.32f, 0.62f, 0.92f),
        ResourceTypeId.AdvancedParts     => new Color(0.70f, 0.55f, 0.95f),
        _                                => new Color(0.92f, 0.58f, 0.24f), // scrap / default amber
    };

    void Build()
    {
        if (transform.Find("NodeMarker") != null) return;

        Color c = ResourceColour(resourceType);

        // Bright albedo does most of the reading; emission is kept LOW on
        // purpose. The bloom pass (threshold 1.35, diffusion 6.5) turns any hot
        // small emitter into a formless glow blob — the first attempt at this
        // marker drowned the shards in exactly that. Sub-threshold emission lets
        // the crystals glow enough to find in the dark while still reading as
        // faceted geometry.
        var mat = new Material(Shader.Find("Standard")) { name = "NodeShard" };
        mat.color = c;
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c * 0.55f);

        var root = new GameObject("NodeMarker");
        root.transform.SetParent(transform, false);
        // Sit the cluster base just above the plinth top. Node root sits ~0.5 up,
        // plinth top ~0.55 world, so a small local lift clears it.
        root.transform.localPosition = new Vector3(0f, 0.06f, 0f);

        // A tall faceted cluster: crystals that clearly rise off the plinth so the
        // deposit reads as geometry from across a corridor, not a flat pad. One
        // central spire plus a ring of shorter shards.
        int shards = 6;
        for (int i = 0; i < shards; i++)
        {
            bool centre = i == 0;
            float ang = (i / (float)shards) * Mathf.PI * 2f;
            float rad = centre ? 0f : 0.34f;
            float height = centre ? 1.25f : Random.Range(0.65f, 0.95f);
            float width = centre ? 0.34f : 0.24f;

            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // "Readability" in the name exempts it from RuntimeArtBackfill's
            // HideHostRenderers, which otherwise disables every renderer under a
            // node except the plinth on its 0.75-2.5s rescan — that is what blanked
            // the first version of this cluster.
            shard.name = "ReadabilityShard";
            shard.transform.SetParent(root.transform, false);
            FxSafe.Destroy(shard.GetComponent<Collider>());
            shard.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, height * 0.5f, Mathf.Sin(ang) * rad);
            // Lean the outer shards outward so the cluster splays like ore.
            float lean = centre ? 0f : 16f;
            shard.transform.localRotation = Quaternion.Euler(
                Mathf.Sin(ang) * lean, ang * Mathf.Rad2Deg, -Mathf.Cos(ang) * lean);
            shard.transform.localScale = new Vector3(width, height, width);
            shard.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // A restrained point light so the deposit is findable in the dark — small
        // enough not to wash the plinth into a puddle (the first attempt used
        // 0.9/4.5 and did exactly that). Culls the wall-cap layer (TransparentFX
        // = layer 1) like the other point lights.
        var lightGo = new GameObject("NodeGlow");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = c;
        light.range = 3f;
        light.intensity = 0.35f;
        light.shadows = LightShadows.None;
        light.cullingMask = ~(1 << 1);
    }
}
