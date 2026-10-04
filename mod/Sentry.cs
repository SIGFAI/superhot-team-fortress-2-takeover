// The Engineer's Sentry Gun: a RED turret built from primitives that tracks and shoots the BLU mercs.
// It runs on game time (Time.deltaTime), so SUPERHOT's freeze stops it mid-swing and its tracers hang in the air.
using Sigf.Kit;
using UnityEngine;

public class Sentry : MonoBehaviour
{
    Transform head;
    Transform muzzle;
    float cooldown = 1.2f, build, flash;
    Light light;
    public int Kills;

    public static Sentry Build(Vector3 pos, Vector3 facing)
    {
        var root = new GameObject("TfSentry");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z));
        var red = Costumes.Red; var steel = new Color(0.55f, 0.55f, 0.6f); var dark = new Color(0.15f, 0.15f, 0.17f);
        Piece(root.transform, PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(0.9f, 0.12f, 0.9f), steel);
        Piece(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.12f, 0.35f, 0.12f), steel);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var leg = Piece(root.transform, PrimitiveType.Cylinder, new Vector3(Mathf.Sin(a) * 0.3f, 0.2f, Mathf.Cos(a) * 0.3f), new Vector3(0.05f, 0.3f, 0.05f), dark);
            leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(a) * 25f, 0, -Mathf.Sin(a) * 25f);
        }
        var head = new GameObject("Head").transform;
        head.SetParent(root.transform, false);
        head.localPosition = new Vector3(0, 0.95f, 0);
        Piece(head, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.55f, 0.4f, 0.6f), red);
        Piece(head, PrimitiveType.Cube, new Vector3(0, 0.25f, -0.1f), new Vector3(0.3f, 0.1f, 0.3f), steel);
        Piece(head, PrimitiveType.Cylinder, new Vector3(-0.12f, 0, 0.45f), new Vector3(0.07f, 0.28f, 0.07f), dark).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Piece(head, PrimitiveType.Cylinder, new Vector3(0.12f, 0, 0.45f), new Vector3(0.07f, 0.28f, 0.07f), dark).transform.localRotation = Quaternion.Euler(90, 0, 0);
        Piece(head, PrimitiveType.Cube, new Vector3(0, 0.05f, 0.31f), new Vector3(0.45f, 0.08f, 0.04f), new Color(1f, 0.85f, 0.2f));
        var m = new GameObject("Muzzle").transform;
        m.SetParent(head, false); m.localPosition = new Vector3(0, 0, 0.8f);
        var s = root.AddComponent<Sentry>();
        s.head = head; s.muzzle = m;
        root.transform.localScale = Vector3.zero;
        return s;
    }

    static GameObject Piece(Transform parent, PrimitiveType t, Vector3 pos, Vector3 size, Color c)
    {
        var g = Mix.Shape(t, parent.position, size, c, false, "TfSentryPart");
        Mix.Paint(g, c, 0.35f);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = size;
        return g;
    }

    PejAiController Target()
    {
        PejAiController best = null; float bd = 28f;
        foreach (var e in G.Enemies())
        {
            float d = Vector3.Distance(e.transform.position, transform.position);
            if (d >= bd) continue;
            var from = muzzle.position; var to = e.transform.position + Vector3.up * 1.2f;
            if (Physics.Linecast(from, to, out var hit) && hit.collider.GetComponentInParent<PejAiController>() == null) continue;
            bd = d; best = e;
        }
        return best;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (build < 1f)
        {
            // pop-up build animation
            build = Mathf.Min(1f, build + dt * 1.6f);
            float k = 1f + Mathf.Sin(build * Mathf.PI) * 0.15f;
            transform.localScale = Vector3.one * build * k * 0.7f;
            return;
        }
        if (light != null) { flash -= dt; if (flash <= 0f) light.enabled = false; }
        var t = Target();
        if (t == null) { head.Rotate(0, 40f * dt, 0, Space.Self); return; }
        var aim = t.transform.position + Vector3.up * 1.2f - head.position;
        head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.LookRotation(aim), 360f * dt);
        cooldown -= dt;
        if (cooldown <= 0f && Vector3.Angle(head.forward, aim) < 8f)
        {
            cooldown = 0.55f;
            Fire(t, aim);
        }
    }

    void Fire(PejAiController t, Vector3 aim)
    {
        var from = muzzle.position;
        var to = t.transform.position + Vector3.up * 1.2f;
        Tracer(from, to);
        if (light == null) light = Mix.Glow(from, new Color(1f, 0.8f, 0.3f), 5f, 2.5f, muzzle);
        light.enabled = true; flash = 0.06f;
        Mix.Burst(from + head.forward * 0.1f, new Color(1f, 0.9f, 0.4f), 4, 4f, 0.05f, 0.5f);
        Fx.Sfx("hit", from, 0.5f, 1.6f);
        // every third burst is the killing one
        if (++shots % 3 == 0)
        {
            Fx.Credit = "SENTRY"; Kills++;
            G.Kill(t);
            Fx.Credit = null;
        }
        else Mix.Burst(to, new Color(1f, 0.9f, 0.4f), 5, 3f, 0.05f, 0.6f);
    }
    int shots;

    static void Tracer(Vector3 a, Vector3 b)
    {
        var g = Mix.Shape(PrimitiveType.Cube, (a + b) / 2f, new Vector3(0.04f, 0.04f, Vector3.Distance(a, b)), new Color(1f, 0.85f, 0.3f), false, "TfTracer");
        Mix.Paint(g, new Color(1f, 0.85f, 0.3f), 2f);
        g.transform.rotation = Quaternion.LookRotation(b - a);
        Object.Destroy(g, 0.25f);
    }
}
