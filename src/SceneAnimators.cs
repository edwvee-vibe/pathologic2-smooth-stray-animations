using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Pathologic2SmoothStrayAnimations
{
    // Which animators are switched: those in AnimatorUpdateMode.AnimatePhysics, except
    // - animators on an object with a NavMeshAgent: these are NPCs, and the game's NPC state code sets their update
    //   mode itself (walking NPCs are put into AnimatePhysics by NpcStateMove; see the Smooth NPC Walking mod);
    // - animators whose controller is listed in SkipControllers (frozen poses).
    //
    // Where they come from, and how the mod finds them without per-frame work:
    // - objects placed in scenes (Nata, soldier dummies): one pass over all loaded animators on each sceneLoaded.
    //   Scene activation is already a loading frame, and the pass only looks at animators in AnimatePhysics;
    // - objects created from prefabs at run time (entity models through DynamicModelComponent, blueprints including
    //   cutscenes): the game creates both through UnityFactory.Instantiate(GameObject, string). A prefix looks the
    //   prefab up in a dictionary. Only the first time a prefab is seen are its animators examined, and if needed the
    //   prefab asset itself is switched in memory, so all its copies are created already switched. For every other
    //   spawn (passers-by included) the cost is one dictionary lookup.
    // Nothing runs per frame. When the mod is off, the scene and spawn handlers return immediately.
    internal static class SceneAnimators
    {
        private struct Entry
        {
            public Animator Animator;
            public Rigidbody Body; // set only when the mod changed its interpolation
        }

        internal sealed class PrefabInfo
        {
            public int[] Indices;    // positions of switched animators in GetComponentsInChildren<Animator>(true)
            public bool[] HasBody;
        }

        // Live objects switched by the mod (scene objects and copies of switched prefabs).
        private static readonly List<Entry> Live = new List<Entry>();
        // Prefab assets switched by the mod. Kept while the mod is off so they can be switched again.
        private static readonly List<Entry> PrefabAssets = new List<Entry>();
        // Prefab instance id -> what was switched in it (null: nothing to do).
        private static readonly Dictionary<int, PrefabInfo> Prefabs = new Dictionary<int, PrefabInfo>();
        // Animators in AnimatePhysics that were examined and left alone (NPCs, skipped controllers).
        private static readonly HashSet<int> Rejected = new HashSet<int>();
        private static readonly List<Animator> Buffer = new List<Animator>();
        private static HashSet<string> skip;

        private static long hookCalls, hookTicks, hookMaxTicks;
        private static float nextHookLog;

        internal static void Init()
        {
            skip = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in Plugin.SkipControllers.Value.Split(','))
            {
                string trimmed = name.Trim();
                if (trimmed.Length > 0)
                {
                    skip.Add(trimmed);
                }
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Plugin.Enabled.Value)
            {
                ScanLoaded(scene.name);
            }
        }

        /// <summary>Switches all eligible animators of loaded scenes. Returns the number switched now.</summary>
        internal static int ScanLoaded(string reason)
        {
            long start = Stopwatch.GetTimestamp();
            Prune();
            Animator[] all = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int switched = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Animator animator = all[i];
                if (animator.updateMode != AnimatorUpdateMode.AnimatePhysics)
                {
                    continue;
                }
                int id = animator.GetInstanceID();
                if (Rejected.Contains(id))
                {
                    continue;
                }
                if (!IsEligible(animator))
                {
                    Rejected.Add(id);
                    continue;
                }
                Live.Add(Switch(animator));
                switched++;
                if (Plugin.LogDetails.Value)
                {
                    Plugin.Log.LogInfo($"Per-frame: {Describe(animator)}");
                }
            }
            if (Plugin.LogDetails.Value)
            {
                Plugin.Log.LogInfo($"Scene pass ({reason}): {all.Length} animators, {switched} switched, " +
                                   $"{Micro(Stopwatch.GetTimestamp() - start)} us");
            }
            return switched;
        }

        private static bool IsEligible(Animator animator)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller != null && skip.Contains(controller.name))
            {
                return false;
            }
            return !animator.TryGetComponent(out NavMeshAgent _);
        }

        private static Entry Switch(Animator animator)
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            var entry = new Entry { Animator = animator };
            if (Plugin.InterpolateBodies.Value && animator.TryGetComponent(out Rigidbody body) &&
                !body.isKinematic && body.interpolation == RigidbodyInterpolation.None)
            {
                body.interpolation = RigidbodyInterpolation.Interpolate;
                entry.Body = body;
            }
            return entry;
        }

        private static void Revert(Entry entry)
        {
            if (entry.Animator != null && entry.Animator.updateMode == AnimatorUpdateMode.Normal)
            {
                entry.Animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            }
            if (entry.Body != null)
            {
                entry.Body.interpolation = RigidbodyInterpolation.None;
            }
        }

        private static void Prune()
        {
            Live.RemoveAll(e => e.Animator == null);
        }

        /// <summary>Applies the toggle. Returns the number of live animators switched by the mod.</summary>
        internal static int SetEnabled(bool on)
        {
            if (on)
            {
                for (int i = 0; i < PrefabAssets.Count; i++)
                {
                    Entry e = PrefabAssets[i];
                    if (e.Animator != null)
                    {
                        e.Animator.updateMode = AnimatorUpdateMode.Normal;
                        if (e.Body != null)
                        {
                            e.Body.interpolation = RigidbodyInterpolation.Interpolate;
                        }
                    }
                }
                ScanLoaded("toggle");
            }
            else
            {
                foreach (Entry e in PrefabAssets)
                {
                    Revert(e);
                }
                foreach (Entry e in Live)
                {
                    Revert(e);
                }
                Live.Clear();
            }
            Prune();
            return Live.Count;
        }

        internal static string Stats()
        {
            return $"live switched {Live.Count}, prefabs seen {Prefabs.Count}, prefab animators switched {PrefabAssets.Count}, " +
                   $"spawn hook calls {hookCalls}";
        }

        internal static void LogHookStats()
        {
            if (Time.unscaledTime < nextHookLog)
            {
                return;
            }
            nextHookLog = Time.unscaledTime + 60f;
            if (hookCalls == 0)
            {
                return;
            }
            Plugin.Log.LogInfo($"Spawn hook, last minute: {hookCalls} calls, total {Micro(hookTicks)} us, " +
                               $"longest {Micro(hookMaxTicks)} us; {Stats()}");
            hookCalls = 0;
            hookTicks = 0;
            hookMaxTicks = 0;
        }

        // Called before the game instantiates a prefab. Returns what has to be tracked in the new copy.
        internal static PrefabInfo BeforeInstantiate(GameObject prefab)
        {
            long start = Stopwatch.GetTimestamp();
            int id = prefab.GetInstanceID();
            if (!Prefabs.TryGetValue(id, out PrefabInfo info))
            {
                info = Analyze(prefab);
                Prefabs[id] = info;
                if (Plugin.LogDetails.Value && info != null)
                {
                    Plugin.Log.LogInfo($"Prefab {prefab.name}: {info.Indices.Length} animators switched, " +
                                       $"{Micro(Stopwatch.GetTimestamp() - start)} us");
                }
            }
            long ticks = Stopwatch.GetTimestamp() - start;
            hookCalls++;
            hookTicks += ticks;
            if (ticks > hookMaxTicks)
            {
                hookMaxTicks = ticks;
            }
            return info;
        }

        private static PrefabInfo Analyze(GameObject prefab)
        {
            prefab.GetComponentsInChildren(true, Buffer);
            List<int> indices = null;
            List<bool> bodies = null;
            for (int i = 0; i < Buffer.Count; i++)
            {
                Animator animator = Buffer[i];
                if (animator.updateMode != AnimatorUpdateMode.AnimatePhysics || !IsEligible(animator))
                {
                    continue;
                }
                Entry entry = Switch(animator);
                PrefabAssets.Add(entry);
                (indices ??= new List<int>()).Add(i);
                (bodies ??= new List<bool>()).Add(entry.Body != null);
                if (Plugin.LogDetails.Value)
                {
                    Plugin.Log.LogInfo($"Per-frame (prefab): {Describe(animator)}");
                }
            }
            Buffer.Clear();
            if (indices == null)
            {
                return null;
            }
            return new PrefabInfo { Indices = indices.ToArray(), HasBody = bodies.ToArray() };
        }

        // Called after the game instantiated a prefab that has switched animators: remember them for the toggle.
        internal static void AfterInstantiate(GameObject instance, PrefabInfo info)
        {
            instance.GetComponentsInChildren(true, Buffer);
            for (int k = 0; k < info.Indices.Length; k++)
            {
                int i = info.Indices[k];
                if (i >= Buffer.Count)
                {
                    break;
                }
                var entry = new Entry { Animator = Buffer[i] };
                if (info.HasBody[k])
                {
                    Buffer[i].TryGetComponent(out entry.Body);
                }
                Live.Add(entry);
            }
            Buffer.Clear();
        }

        private static string Describe(Animator animator)
        {
            var sb = new StringBuilder();
            Transform t = animator.transform;
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                path = p.name + "/" + path;
            }
            sb.Append(animator.gameObject.scene.IsValid() ? animator.gameObject.scene.name : "<asset>").Append(": ").Append(path);
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            sb.Append(" (").Append(controller != null ? controller.name : "no controller").Append(')');
            return sb.ToString();
        }

        private static long Micro(long ticks)
        {
            return ticks * 1000000L / Stopwatch.Frequency;
        }
    }

    [HarmonyPatch]
    internal static class UnityFactoryInstantiatePatch
    {
        // UnityFactory also has a generic Instantiate<T>(GameObject, string); patch the non-generic one.
        private static System.Reflection.MethodBase TargetMethod()
        {
            foreach (var method in typeof(UnityFactory).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var parameters = method.GetParameters();
                if (method.Name == nameof(UnityFactory.Instantiate) && !method.IsGenericMethodDefinition &&
                    parameters.Length == 2 && parameters[0].ParameterType == typeof(GameObject) &&
                    parameters[1].ParameterType == typeof(string))
                {
                    return method;
                }
            }
            throw new MissingMethodException("UnityFactory.Instantiate(GameObject, string) not found");
        }

        private static void Prefix(GameObject prefab, out SceneAnimators.PrefabInfo __state)
        {
            __state = null;
            if (!Plugin.Enabled.Value || prefab == null)
            {
                return;
            }
            __state = SceneAnimators.BeforeInstantiate(prefab);
        }

        private static void Postfix(GameObject __result, SceneAnimators.PrefabInfo __state)
        {
            if (__state != null && __result != null)
            {
                SceneAnimators.AfterInstantiate(__result, __state);
            }
        }
    }
}
