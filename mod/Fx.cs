// Kill effects (gear confetti, class deaths, crit pop-ups), the TF2-style HUD and the kill feed.
using System.Collections.Generic;
using HarmonyLib;
using Sigf.Kit;
using UnityEngine;

public static class Fx
{
    public static int BluDown, Streak, Alive;
    public static float LastKill = -99f;
    public static string Credit;   // set by the sentry around its kills: shown in the kill feed instead of YOU
    static readonly Color Cream = new Color(0.97f, 0.9f, 0.77f);
    static readonly Color Panel = new Color(0.16f, 0.14f, 0.12f, 0.85f);

    class Pop { public Vector3 pos; public string text; public Color color; public float born, life; public int size; }
    class Feed { public string text; public Color color; public float born; }
    static readonly List<Pop> pops = new List<Pop>();
    static readonly List<Feed> feed = new List<Feed>();
    static GUIStyle big, small;

    public static void Popup(Vector3 pos, string text, Color color, int size = 44, float life = 1.8f) =>
        pops.Add(new Pop { pos = pos, text = text, color = color, born = Time.unscaledTime, life = life, size = size });

    public static void Feeds(string text, Color c)
    {
        feed.Add(new Feed { text = text, color = c, born = Time.unscaledTime });
        if (feed.Count > 5) feed.RemoveAt(0);
    }

    static AudioClip Clip(string n) { try { return Mix.Sound(n + ".wav"); } catch (System.Exception e) { Mix.Warn(e.Message); return null; } }
    public static void Sfx(string n, Vector3? pos = null, float vol = 1f, float pitch = 1f)
    {
        var c = Clip(n); if (c != null) Mix.Play(c, pos, vol, pitch);
    }

    // ---------- kill ----------
    public static void Killed(PejAiController e)
    {
        var cs = e.GetComponent<Costume>();
        string cls = cs != null ? cs.Class : "Merc";
        bool blu = cs == null || cs.Blu;
        var tc = blu ? Costumes.Blu : Costumes.Red;
        var p = e.transform.position + Vector3.up * 1.3f;
        Streak = Time.unscaledTime - LastKill < 8f ? Streak + 1 : 1;
        LastKill = Time.unscaledTime;
        BluDown++;

        Sfx("hit", p, 1f, Random.Range(0.9f, 1.15f));
        // team-colored gibs
        Mix.Burst(p, tc, 14, 6f, 0.08f, 3f);
        // gear flies off as little physics props (they follow the game's slow motion)
        Scatter(e.transform, tc);

        string weapon = "Crystal Shot";
        switch (cls)
        {
            case "Demoman":
                Sfx("boom", p, 1f); Mix.Burst(p, new Color(1f, 0.7f, 0.1f), 30, 11f, 0.12f, 2f); G.Shake(0.8f);
                Popup(p + Vector3.up * 0.8f, "KABOOM!", new Color(1f, 0.6f, 0.1f), 56); weapon = "Sticky Bomb"; Flash(p, new Color(1f, 0.6f, 0.2f), 7f, 0.35f); break;
            case "Pyro":
                Sfx("flame", p, 0.8f); Mix.Burst(p, new Color(1f, 0.45f, 0.05f), 40, 7f, 0.16f, 2.4f); Mix.Burst(p, new Color(1f, 0.9f, 0.2f), 20, 5f, 0.1f, 1.8f);
                Flash(p, new Color(1f, 0.45f, 0.1f), 6f, 0.5f); Popup(p + Vector3.up * 0.8f, "BURNED!", new Color(1f, 0.5f, 0.1f), 48); weapon = "Flame Burst"; break;
            case "Medic":
                Sfx("medic", p, 1f); Popup(p + Vector3.up * 0.8f, "MEDIC!", Color.white, 52); weapon = "Bonesaw Bait"; break;
            case "Heavy":
                Sfx("laugh", p, 0.9f, 0.8f); Mix.Burst(p, new Color(0.95f, 0.75f, 0.2f), 20, 8f, 0.1f, 2.5f); weapon = "Sandvich Special"; break;
            case "Spy": Popup(p + Vector3.up * 0.8f, "BACKSTAB!", new Color(0.8f, 0.1f, 0.1f), 52); weapon = "Butterfly Knife"; break;
            case "Scout": weapon = "Scattergun"; break;
            case "Soldier": weapon = "Rocket Launcher"; break;
            case "Sniper": weapon = "Headshot"; Popup(p + Vector3.up * 0.8f, "HEADSHOT!", Color.white, 52); break;
            case "Engineer": weapon = "Wrench Smash"; break;
        }
        if (cls != "Medic" && cls != "Demoman" && cls != "Pyro" && cls != "Spy" && cls != "Sniper")
        {
            Sfx("crit", p, 1f);
            Popup(p + Vector3.up * 0.8f, Random.value < 0.6f ? "CRITICAL HIT!" : "MINI-CRIT!", Streak > 2 ? new Color(1f, 0.9f, 0.2f) : Cream, 50);
        }
        if (Credit != null) weapon = "Sentry Gun";
        Feeds((Credit ?? "YOU") + "  killed  " + (blu ? "BLU " : "RED ") + cls + "  [" + weapon + "]", tc);
        if (Streak == 3) { Mix.Say("DOMINATION!", 2f, new Color(1f, 0.85f, 0.2f), 0.3f, 80); Sfx("chime"); }
        if (Streak == 6) { Mix.Say("KILLING SPREE!", 2f, new Color(1f, 0.5f, 0.1f), 0.3f, 80); Sfx("chime"); }
    }

    static void Flash(Vector3 p, Color c, float range, float seconds)
    {
        var l = Mix.Glow(p, c, range, 2f);
        Object.Destroy(l.gameObject, seconds);
    }

    static void Scatter(Transform body, Color team)
    {
        var holders = new List<Transform>();
        Collect(body, holders);
        foreach (var h in holders)
        {
            var parts = new List<Transform>();
            foreach (Transform c in h) parts.Add(c);
            foreach (var part in parts)
            {
                part.SetParent(null, true);
                var col = part.gameObject.AddComponent<BoxCollider>();
                var rb = part.gameObject.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.velocity = (Random.onUnitSphere + Vector3.up * 1.2f) * Random.Range(3f, 7f);
                rb.angularVelocity = Random.insideUnitSphere * 12f;
                part.gameObject.layer = 2;
                Object.Destroy(part.gameObject, Random.Range(4f, 7f));
            }
            Object.Destroy(h.gameObject);
        }
    }

    static void Collect(Transform t, List<Transform> l)
    {
        foreach (Transform c in t) { if (c.name == "TfGear") l.Add(c); else Collect(c, l); }
    }

    // ---------- HUD ----------
    static void Box(Rect r, Color c)
    {
        var prev = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = prev;
    }

    static void Label(Rect r, string t, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter)
    {
        if (big == null) big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        big.fontSize = size; big.alignment = a;
        var prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.8f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, big);
        GUI.color = c; GUI.Label(r, t, big); GUI.color = prev;
    }

    public static void DrawHud()
    {
        float k = Screen.height / 1080f; float W = Screen.width, H = Screen.height;
        float now = Time.unscaledTime;
        // health (bottom left) and ammo (bottom right)
        Box(new Rect(30 * k, H - 150 * k, 230 * k, 110 * k), Panel);
        Box(new Rect(30 * k, H - 150 * k, 230 * k, 6 * k), Costumes.Red);
        Label(new Rect(40 * k, H - 140 * k, 100 * k, 90 * k), "+", (int)(70 * k), Cream);
        Label(new Rect(110 * k, H - 150 * k, 150 * k, 110 * k), "125", (int)(80 * k), Cream);
        Label(new Rect(30 * k, H - 188 * k, 230 * k, 36 * k), "HEALTH", (int)(22 * k), Cream);
        Box(new Rect(W - 290 * k, H - 150 * k, 260 * k, 110 * k), Panel);
        Box(new Rect(W - 290 * k, H - 150 * k, 260 * k, 6 * k), Costumes.Red);
        Label(new Rect(W - 290 * k, H - 150 * k, 150 * k, 110 * k), "6", (int)(80 * k), Cream);
        Label(new Rect(W - 150 * k, H - 130 * k, 110 * k, 70 * k), "/ 32", (int)(40 * k), Cream);
        Label(new Rect(W - 290 * k, H - 188 * k, 260 * k, 36 * k), "AMMO", (int)(22 * k), Cream);
        // scoreboard (top centre): RED = you, BLU = the mercs you shattered
        Box(new Rect(W / 2 - 170 * k, 14 * k, 150 * k, 56 * k), new Color(Costumes.Red.r, Costumes.Red.g, Costumes.Red.b, 0.9f));
        Box(new Rect(W / 2 + 20 * k, 14 * k, 150 * k, 56 * k), new Color(Costumes.Blu.r, Costumes.Blu.g, Costumes.Blu.b, 0.9f));
        Label(new Rect(W / 2 - 170 * k, 14 * k, 150 * k, 56 * k), "YOU  " + BluDown, (int)(34 * k), Color.white);
        Label(new Rect(W / 2 + 20 * k, 14 * k, 150 * k, 56 * k), "BLU  " + Alive, (int)(34 * k), Color.white);
        Label(new Rect(W / 2 - 300 * k, 76 * k, 600 * k, 30 * k), "YOU = mercs shattered   |   BLU = mercs still standing", (int)(18 * k), Cream);
        // kill feed (top right)
        for (int i = feed.Count - 1; i >= 0; i--) if (now - feed[i].born > 7f) feed.RemoveAt(i);
        for (int i = 0; i < feed.Count; i++)
        {
            var r = new Rect(W - 520 * k, (20 + i * 40) * k, 490 * k, 34 * k);
            Box(r, new Color(0.1f, 0.08f, 0.06f, 0.75f));
            Box(new Rect(r.x, r.y, 5 * k, r.height), feed[i].color);
            Label(r, feed[i].text, (int)(20 * k), Cream);
        }
        // pop-ups in the world
        var cam = G.Cam;
        if (cam != null)
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i]; float a = (now - p.born) / p.life;
                if (a >= 1f) { pops.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.pos + Vector3.up * a * 0.8f);
                if (sp.z <= 0f) continue;
                var c = p.color; c.a = 1f - a * a;
                Label(new Rect(sp.x - 300 * k, H - sp.y - 40 * k, 600 * k, 80 * k), p.text, (int)(p.size * k), c);
            }
    }
}

[HarmonyPatch(typeof(PejAiController), nameof(PejAiController.Kill))]
static class EnemyKilledPatch
{
    static void Prefix(PejAiController __instance)
    {
        if (__instance == null || __instance.IsDead) return;
        Fx.Killed(__instance);
    }
}

// the game's endless mode throws its kill counter across the screen as giant numerals: they hide the TF2 HUD.
// DisplayQuick is inlined into its callers, so block the method every overload ends in.
[HarmonyPatch(typeof(TextManager), nameof(TextManager.Display))]
static class NoNumbersPatch
{
    static bool Prefix(OverlayWord[] words)
    {
        if (words == null || words.Length == 0) return true;
        foreach (var w in words)
        {
            if (w == null || string.IsNullOrEmpty(w.txt)) return true;
            foreach (var ch in w.txt) if (char.IsLetter(ch)) return true;
            Mix.Log("blocked numeral '" + w.txt + "'");
        }
        return false;
    }
}
