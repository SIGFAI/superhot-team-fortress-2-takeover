// Team Fortress 2 Takeover: every SUPERHOT enemy becomes a BLU mercenary (Scout, Soldier, Pyro, Demoman, Heavy,
// Engineer, Medic, Sniper, Spy) with its own hat and gear. Kills are TF2-flavoured: crits, gear confetti, class deaths,
// kill feed, scoreboard and the TF2 HUD.
using System.Collections;
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class SigfMod : MixMod
{
    public override void OnReady()
    {
        Mix.Say("TEAM FORTRESS 2 TAKEOVER", 4f, new Color(1f, 0.55f, 0.15f), 0.2f, 72);
        Mix.After(0.5f, () => Fx.Sfx("chime"));
        Mix.After(1.2f, () => G.Words("BLU;TEAM;INCOMING"));
        Mix.Every(0.1f, DressAll, "dress");
        Mix.After(3f, () => { if (!Mix.DemoStarted) BuildSentry(); });
        Mix.Every(5f, () =>
        {
            if (G.Cam != null && G.Enemies().Count < 5)
            {
                var e = G.SpawnEnemy(G.Ahead(11f) + Random.insideUnitSphere * 3f);
                if (e != null) DressRandom(e);
            }
        }, "spawner");
    }

    static Sentry BuildSentry()
    {
        if (G.Cam == null) return null;
        var pos = G.Ahead(4.2f) + G.Cam.transform.right * 1.0f;
        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var nh, 3f, UnityEngine.AI.NavMesh.AllAreas)) pos = nh.position;
        Fx.Sfx("chime");
        Fx.Feeds("Engineer built a Sentry Gun", Costumes.Red);
        Mix.Say("SENTRY GUN BUILT!", 3f, new Color(1f, 0.85f, 0.3f), 0.25f, 60);
        Fx.Popup(pos + Vector3.up * 1.8f, "SENTRY GUN  LV1", Color.white, 34, 8f);
        Mix.Burst(pos + Vector3.up * 0.3f, new Color(1f, 0.8f, 0.3f), 20, 5f, 0.06f, 1f);
        return Sentry.Build(pos, G.Ahead(10f) - pos);
    }

    static void DressRandom(PejAiController e)
    {
        Costumes.Dress(e, Costumes.Classes[Random.Range(0, Costumes.Classes.Length)], true);
    }

    static void DressAll()
    {
        int n = 0, un = 0;
        foreach (var e in G.Enemies())
        {
            n++;
            if (e.GetComponent<Costume>() == null) { un++; DressRandom(e); }
        }
        Fx.Alive = n;
    }

    public override void OnGui() => Fx.DrawHud();

    public override IEnumerator Demo()
    {
        for (int w = 0; w < 20 && G.Cam == null; w++) yield return Mix.Wait(0.5f);
        G.ForceTime(0.04f);
        G.Place(new Vector3(7.5f, -1.4f, -2.5f), new Vector3(7.5f, -0.6f, -12f));
        yield return Mix.Wait(0.6f);
        BuildSentry();   // the Engineer's turret pops up beside the player in the first seconds
        yield return Mix.Wait(1.5f);
        // the lineup: all nine classes close in front of the player, frozen in SUPERHOT time
        var line = new Dictionary<string, PejAiController>();
        int i = 0;
        foreach (var cls in Costumes.Classes)
        {
            var pos = G.Ahead(2.6f + i * 0.9f) + G.Cam.transform.right * (i % 2 == 0 ? -0.55f : 0.45f);
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var nh, 2.5f, UnityEngine.AI.NavMesh.AllAreas)) pos = nh.position;
            var e = G.SpawnEnemy(pos);
            if (e == null) { i++; continue; }
            Costumes.Dress(e, cls, true);
            line[cls] = e;
            i++;
        }
        Mix.Say("NINE MERCS. ONE PLAYER.", 4f, Color.white, 0.3f, 56);
        yield return Mix.Wait(4f);
        foreach (var cls in new[] { "Demoman", "Pyro", "Medic", "Heavy" })
        {
            if (!line.TryGetValue(cls, out var e) || e == null || e.IsDead) continue;
            G.Shatter(e);
            yield return Mix.Wait(2.2f);
        }
        // real time again: SUPERHOT time pulses between slow and fast, the sentry and the bot pick mercs off one by one
        for (int round = 0; round < 40; round++)
        {
            G.ForceTime(round % 4 < 2 ? 0.1f : 0.6f);
            yield return Mix.Wait(1.8f);
            if (round % 3 != 2) continue;
            var t = G.Closest(G.Pos);
            if (t != null && !t.IsDead) G.Shatter(t);
        }
        G.Release();
    }
}
