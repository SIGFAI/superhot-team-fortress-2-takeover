// TF2 mercenary costumes: primitive-built hats and gear parented to the enemy's bones.
using Sigf.Kit;
using UnityEngine;

public class Costume : MonoBehaviour
{
    public string Class;
    public bool Blu;
    Renderer[] rends;

    // the game recolours its enemies as they spawn and when they are hit: hold the team colour every frame
    void LateUpdate()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        var c = Blu ? Costumes.Blu : Costumes.Red;
        foreach (var r in rends)
        {
            if (r == null || r.name == "TfPart" || r.sharedMaterial == null || !r.sharedMaterial.shader.name.Contains("Crystal")) continue;
            foreach (var m in r.materials)
            {
                if (m == null || m.shader == null || !m.shader.name.Contains("Crystal")) continue;
                if (names == null)
                {
                    var l = new System.Collections.Generic.List<string>();
                    var sh = m.shader;
                    for (int i = 0; i < sh.GetPropertyCount(); i++)
                        if (sh.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color) l.Add(sh.GetPropertyName(i));
                    names = l.ToArray();
                }
                foreach (var n in names)
                {
                    var o = m.GetColor(n);
                    if (n == "_Color" || n == "_EmissionColor" || (o.r > o.g * 1.4f && o.r > o.b * 1.4f && o.r > 0.1f))
                        m.SetColor(n, new Color(c.r * Mathf.Max(o.r, 0.6f), c.g * Mathf.Max(o.r, 0.6f), c.b * Mathf.Max(o.r, 0.6f), o.a));
                }
            }
        }
    }
    static string[] names;
}

public static class Costumes
{
    public static readonly string[] Classes = { "Scout", "Soldier", "Pyro", "Demoman", "Heavy", "Engineer", "Medic", "Sniper", "Spy" };
    public static readonly Color Blu = new Color(0.25f, 0.5f, 0.85f);
    public static readonly Color Red = new Color(0.8f, 0.2f, 0.15f);

    const float GearScale = 1.5f;

    static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
        return null;
    }

    // A holder at the bone, upright and facing the same way as the body, so offsets are x right, y up, z forward (metres).
    static Transform Holder(Transform bone, Transform body)
    {
        var h = new GameObject("TfGear").transform;
        h.position = bone.position;
        h.rotation = Quaternion.Euler(0f, body.eulerAngles.y, 0f);
        h.SetParent(bone, true);
        h.localScale = Vector3.one * (GearScale / Mathf.Max(0.01f, bone.lossyScale.x));
        return h;
    }

    static GameObject Part(Transform h, PrimitiveType t, Vector3 pos, Vector3 size, Color c, Vector3? euler = null)
    {
        var g = Mix.Shape(t, h.position, size, c, false, "TfPart");
        Mix.Paint(g, c, 0.45f);
        g.transform.SetParent(h, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
        g.transform.localScale = size;
        return g;
    }

    // a clone of a dressed enemy carries its gear: remove it before dressing again
    static void Strip(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var c = t.GetChild(i);
            if (c.name == "TfGear") Object.DestroyImmediate(c.gameObject); else Strip(c);
        }
    }

    public static void Dress(PejAiController e, string cls, bool blu)
    {
        var old = e.GetComponent<Costume>();
        if (old != null)
        {
            if (old.Class == cls && old.Blu == blu) return;
            Strip(e.transform);
            Object.DestroyImmediate(old);
        }
        var m = e.gameObject.AddComponent<Costume>(); m.Class = cls; m.Blu = blu;
        var body = e.transform;
        var head = Find(body, "Head"); var chest = Find(body, "Chest");
        var hips = Find(body, "Hips");
        if (head == null || chest == null) return;
        var eb = Find(body, "EnemyBody");
        if (eb != null) eb.localScale *= 1.2f;
        var team = blu ? Blu : Red;
        var hh = Holder(head, body); var ch = Holder(chest, body);
        var dark = new Color(0.12f, 0.1f, 0.1f); var white = new Color(0.95f, 0.95f, 0.95f);
        var glass = new Color(0.6f, 0.9f, 1f); var gold = new Color(0.95f, 0.75f, 0.2f);
        var S = PrimitiveType.Sphere; var C = PrimitiveType.Cube; var Cy = PrimitiveType.Cylinder;
        var skin = new Color(0.93f, 0.72f, 0.58f);
        Part(hh, S, new Vector3(0, 0.02f, 0.08f), new Vector3(0.22f, 0.25f, 0.2f), skin);
        Part(ch, Cy, new Vector3(0.22f, 0.0f, 0f), new Vector3(0.1f, 0.12f, 0.1f), team);
        switch (cls)
        {
            case "Scout":
                Part(hh, S, new Vector3(0, 0.12f, 0.05f), new Vector3(0.27f, 0.17f, 0.28f), team);
                Part(hh, C, new Vector3(0, 0.09f, 0.22f), new Vector3(0.2f, 0.02f, 0.14f), team);
                Part(hh, Cy, new Vector3(-0.14f, 0.0f, 0.04f), new Vector3(0.1f, 0.02f, 0.1f), dark, new Vector3(0, 0, 90));
                Part(hh, Cy, new Vector3(0.14f, 0.0f, 0.04f), new Vector3(0.1f, 0.02f, 0.1f), dark, new Vector3(0, 0, 90));
                Part(ch, Cy, new Vector3(0.05f, 0.1f, -0.22f), new Vector3(0.06f, 0.6f, 0.06f), new Color(0.75f, 0.55f, 0.3f), new Vector3(0, 0, 35));
                break;
            case "Soldier":
                Part(hh, S, new Vector3(0, 0.1f, 0.05f), new Vector3(0.32f, 0.26f, 0.32f), new Color(0.35f, 0.4f, 0.22f));
                Part(hh, C, new Vector3(0, 0.12f, 0.2f), new Vector3(0.05f, 0.04f, 0.14f), white);
                Part(hh, Cy, new Vector3(0, 0.0f, 0.2f), new Vector3(0.2f, 0.02f, 0.07f), new Color(0.3f, 0.35f, 0.2f), new Vector3(0, 0, 90));
                Part(ch, C, new Vector3(0, 0.05f, -0.17f), new Vector3(0.3f, 0.35f, 0.12f), new Color(0.3f, 0.3f, 0.25f));
                Part(ch, Cy, new Vector3(0.0f, 0.2f, -0.22f), new Vector3(0.1f, 0.5f, 0.1f), new Color(0.25f, 0.28f, 0.2f), new Vector3(60, 0, 0));
                break;
            case "Pyro":
                Part(hh, S, new Vector3(0, 0.03f, 0.06f), new Vector3(0.3f, 0.32f, 0.3f), new Color(0.75f, 0.35f, 0.1f));
                Part(hh, S, new Vector3(-0.07f, 0.04f, 0.2f), new Vector3(0.1f, 0.1f, 0.04f), glass);
                Part(hh, S, new Vector3(0.07f, 0.04f, 0.2f), new Vector3(0.1f, 0.1f, 0.04f), glass);
                Part(hh, Cy, new Vector3(0, -0.06f, 0.24f), new Vector3(0.08f, 0.05f, 0.08f), dark, new Vector3(90, 0, 0));
                Part(ch, Cy, new Vector3(0, 0.0f, -0.2f), new Vector3(0.18f, 0.28f, 0.18f), new Color(0.65f, 0.15f, 0.1f));
                Part(ch, Cy, new Vector3(0.1f, 0.0f, -0.2f), new Vector3(0.14f, 0.26f, 0.14f), new Color(0.65f, 0.65f, 0.7f));
                break;
            case "Demoman":
                Part(hh, S, new Vector3(0, 0.11f, 0.04f), new Vector3(0.27f, 0.14f, 0.27f), dark);
                Part(hh, S, new Vector3(0.07f, 0.04f, 0.2f), new Vector3(0.09f, 0.09f, 0.03f), dark);
                Part(hh, C, new Vector3(0, 0.07f, 0.1f), new Vector3(0.3f, 0.01f, 0.01f), dark, new Vector3(0, 0, -15));
                if (hips != null) { var r = Holder(hips, body); Part(r, Cy, new Vector3(0.28f, 0.0f, 0.0f), new Vector3(0.12f, 0.2f, 0.12f), new Color(0.2f, 0.55f, 0.2f)); Part(r, Cy, new Vector3(0.28f, 0.17f, 0.0f), new Vector3(0.05f, 0.1f, 0.05f), new Color(0.2f, 0.55f, 0.2f)); }
                break;
            case "Heavy":
                Part(ch, Cy, new Vector3(0, 0.1f, 0.16f), new Vector3(0.3f, 0.03f, 0.1f), new Color(0.45f, 0.3f, 0.15f), new Vector3(0, 0, 40));
                for (int i = 0; i < 7; i++)
                    Part(ch, Cy, new Vector3(-0.15f + i * 0.05f, 0.14f - i * 0.05f, 0.2f), new Vector3(0.04f, 0.05f, 0.04f), gold);
                Part(hh, S, new Vector3(0, -0.04f, 0.14f), new Vector3(0.2f, 0.12f, 0.12f), new Color(0.15f, 0.1f, 0.05f));
                Part(hh, C, new Vector3(0, 0.05f, 0.2f), new Vector3(0.22f, 0.03f, 0.02f), new Color(0.15f, 0.1f, 0.05f));
                Part(ch, S, new Vector3(0, 0.0f, 0), new Vector3(0.5f, 0.45f, 0.4f), team * 0.9f);
                break;
            case "Engineer":
                Part(hh, S, new Vector3(0, 0.11f, 0.05f), new Vector3(0.3f, 0.2f, 0.3f), new Color(0.95f, 0.8f, 0.1f));
                Part(hh, Cy, new Vector3(0, 0.14f, 0.05f), new Vector3(0.06f, 0.1f, 0.1f), new Color(0.95f, 0.8f, 0.1f));
                Part(hh, S, new Vector3(-0.07f, 0.1f, 0.2f), new Vector3(0.1f, 0.1f, 0.03f), glass);
                Part(hh, S, new Vector3(0.07f, 0.1f, 0.2f), new Vector3(0.1f, 0.1f, 0.03f), glass);
                if (hips != null) { var r = Holder(hips, body); Part(r, Cy, new Vector3(-0.3f, 0.0f, 0.0f), new Vector3(0.05f, 0.4f, 0.05f), new Color(0.6f, 0.6f, 0.65f)); Part(r, Cy, new Vector3(-0.3f, 0.24f, 0.0f), new Vector3(0.16f, 0.05f, 0.1f), new Color(0.75f, 0.2f, 0.15f)); }
                break;
            case "Medic":
                Part(hh, S, new Vector3(-0.07f, 0.03f, 0.2f), new Vector3(0.1f, 0.1f, 0.03f), glass);
                Part(hh, S, new Vector3(0.07f, 0.03f, 0.2f), new Vector3(0.1f, 0.1f, 0.03f), glass);
                Part(hh, S, new Vector3(0, 0.1f, 0.0f), new Vector3(0.24f, 0.12f, 0.2f), new Color(0.75f, 0.75f, 0.75f));
                Part(ch, C, new Vector3(0, -0.02f, 0), new Vector3(0.4f, 0.5f, 0.28f), white);
                Part(ch, C, new Vector3(0, 0.05f, -0.2f), new Vector3(0.28f, 0.34f, 0.14f), new Color(0.7f, 0.7f, 0.72f));
                Part(ch, C, new Vector3(0.0f, 0.1f, 0.2f), new Vector3(0.08f, 0.02f, 0.02f), team);
                break;
            case "Sniper":
                Part(hh, Cy, new Vector3(0, 0.1f, 0.05f), new Vector3(0.5f, 0.012f, 0.5f), new Color(0.5f, 0.38f, 0.2f));
                Part(hh, S, new Vector3(0, 0.14f, 0.05f), new Vector3(0.28f, 0.2f, 0.28f), new Color(0.5f, 0.38f, 0.2f));
                Part(hh, C, new Vector3(0, 0.03f, 0.2f), new Vector3(0.22f, 0.05f, 0.03f), dark);
                Part(ch, Cy, new Vector3(0, 0.0f, -0.22f), new Vector3(0.05f, 0.9f, 0.05f), new Color(0.25f, 0.2f, 0.12f), new Vector3(0, 0, 25));
                break;
            case "Spy":
                Part(hh, S, new Vector3(0, 0.03f, 0.06f), new Vector3(0.27f, 0.3f, 0.27f), dark);
                Part(hh, S, new Vector3(0, 0.04f, 0.2f), new Vector3(0.14f, 0.06f, 0.04f), team);
                Part(hh, Cy, new Vector3(0, 0.12f, 0.05f), new Vector3(0.28f, 0.012f, 0.28f), new Color(0.1f, 0.1f, 0.12f));
                Part(hh, Cy, new Vector3(0, 0.17f, 0.05f), new Vector3(0.18f, 0.06f, 0.18f), new Color(0.1f, 0.1f, 0.12f));
                Part(ch, C, new Vector3(0, 0.15f, 0.2f), new Vector3(0.05f, 0.16f, 0.02f), new Color(0.7f, 0.1f, 0.1f));
                break;
        }
    }
}
