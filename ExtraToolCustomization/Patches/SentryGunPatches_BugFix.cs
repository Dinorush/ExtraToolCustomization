using AIGraph;
using Enemies;
using GameData;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Arrays = Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace ExtraToolCustomization.Patches
{
    [HarmonyPatch]
    internal static class SentryGunPatches_AlwaysFix
    {
        [HarmonyPatch(typeof(SentryGunInstance_Detection), nameof(SentryGunInstance_Detection.GetNodesInSight))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_GetNodes(ref Il2Arrays.Il2CppReferenceArray<AIG_CourseNode> __result)
        {
            __result = null!;
            return false;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Detection), nameof(SentryGunInstance_Detection.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_Setup(SentryGunInstance_Detection __instance, AIG_CourseNode spawnNode)
        {
            __instance.m_nodesToCheck = GetNodesInSight(spawnNode, __instance.DetectionSource, __instance.m_archetypeData.Sentry_DetectionMaxRange, __instance.m_archetypeData.Sentry_DetectionMaxAngle <= 90f);
        }

        private static readonly Queue<AIG_CourseNode> s_nodeQueue = new();
        private static AIG_CourseNode[] GetNodesInSight(AIG_CourseNode originNode, Transform detectionSource, float maxRange, bool checkAngle)
        {
            AIG_SearchID.IncrementSearchID();
            ushort searchID = AIG_SearchID.SearchID;
            float maxRangeSqr = maxRange * maxRange;
            Vector3 position = detectionSource.position;
            Vector3 forward = detectionSource.forward;
            s_nodeQueue.Enqueue(originNode);
            List<AIG_CourseNode> nodeList = new();

            while (s_nodeQueue.TryDequeue(out var node))
            {
                node.m_searchID = searchID;
                nodeList.Add(node);

                foreach (AIG_CoursePortal portal in node.m_portals)
                {
                    AIG_CourseNode oppositeNode = portal.GetOppositeNode(node);
                    if (oppositeNode == null || oppositeNode.m_searchID == searchID) continue;

                    if (BoundsInRange(portal.m_cullPortal.Bounds, position, forward, maxRangeSqr, checkAngle))
                        s_nodeQueue.Enqueue(oppositeNode);
                }
            }
            return nodeList.ToArray();
        }

        private static bool BoundsInRange(Bounds bounds, Vector3 origin, Vector3 dir, float sqrRange, bool checkAngle)
        {
            Vector3 diff = bounds.ClosestPoint(origin) - origin;
            if (diff.sqrMagnitude >= sqrRange) return false;

            if (!checkAngle) return true;

            diff = bounds.center - origin;
            float distToBox = Vector3.Dot(dir, diff);
            if (distToBox >= 0) return true;

            float lenInDir = bounds.extents.x * Math.Abs(dir.x) + bounds.extents.y * Math.Abs(dir.y) + bounds.extents.z * Math.Abs(dir.z);
            return distToBox + lenInDir >= 0;
        }

        private readonly static EnemyTarget[] _targetCache = new EnemyTarget[50];
        struct EnemyTarget
        {
            public EnemyAgent EnemyAgent;
            public float DistanceToTargetSqr;
            public float AngleToTarget;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Detection), nameof(SentryGunInstance_Detection.CheckForTarget))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_CheckForTarget(SentryGunInstance_Detection __instance, ArchetypeDataBlock archetypeData, Il2Arrays.Il2CppReferenceArray<AIG_CourseNode> nodesToCheck, Transform detectionSource, ref EnemyAgent? __result)
        {
            if (nodesToCheck == null)
            {
                __result = null;
                return false;
            }

            Vector3 forward = detectionSource.forward;
            Vector3 position = detectionSource.position;
            bool targetBody = archetypeData.Sentry_ForceAimTowardsBody;
            float maxAngle = archetypeData.Sentry_DetectionMaxAngle;
            float maxRangeSqr = archetypeData.Sentry_DetectionMaxRange * archetypeData.Sentry_DetectionMaxRange;
            int blockerMask = LayerManager.MASK_SENTRYGUN_DETECTION_BLOCKERS;

            int numTargets = 0;
            foreach (var node in nodesToCheck)
            {
                foreach (var enemy in node.m_enemiesInNode)
                {
                    Vector3 targetPos = enemy.transform.position;
                    Vector3 diff = targetPos - position;
                    float sqrMagnitude = diff.sqrMagnitude;
                    if (diff.sqrMagnitude > maxRangeSqr) continue;

                    float angle = Vector3.Angle(forward, diff);
                    if (angle > maxAngle) continue;

                    Vector3 aimPos = (targetBody ? enemy.AimTargetBody : enemy.AimTarget).position;
                    if (Physics.Linecast(position, aimPos, blockerMask)) continue;

                    if (enemy.Damage.Health > 0 && ((!enemy.RequireTagForDetection && !archetypeData.Sentry_FireTagOnly) || enemy.IsTagged))
                    {
                        EnemyTarget enemyTarget = new()
                        {
                            EnemyAgent = enemy,
                            DistanceToTargetSqr = sqrMagnitude,
                            AngleToTarget = angle
                        };
                        _targetCache[numTargets++] = enemyTarget;

                        if (numTargets >= _targetCache.Length) break;
                    }
                }

                if (numTargets >= _targetCache.Length) break;
            }

            EnemyTarget bestTarget = default;
            float bestScore = float.MaxValue;
            bool prioritizeTag = archetypeData.Sentry_PrioTag;
            for (int i = 0; i < numTargets; i++)
            {
                var target = _targetCache[i];
                float score = (target.DistanceToTargetSqr + target.AngleToTarget * target.AngleToTarget) / 2f;
                if (prioritizeTag && target.EnemyAgent.IsTagged)
                    score *= 0.5f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }
            __result = bestTarget.EnemyAgent;
            return false;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StartFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StartFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstClipCurr = __instance.m_archetypeData.BurstShotCount;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.StopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StopFiring(SentryGunInstance_Firing_Bullets __instance)
        {
            __instance.m_burstTimer = Clock.Time + __instance.m_archetypeData.BurstDelay;
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.StopFiring))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_StopFiring(SentryGunInstance __instance)
        {
            if (__instance.Ammo < __instance.CostOfBullet)
            {
                var detection = __instance.m_detection.Cast<SentryGunInstance_Detection>();
                detection.Target = null;
                detection.TargetAimTrans = null;
                detection.HasTarget = false;
                detection.TargetIsTagged = false;
            }
        }
    }

    [HarmonyPatch]
    internal static class SentryGunPatches_ShotgunFix
    {
        private static Vector3? _cachedDir = null;
        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.TriggerSingleFireAudio))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Pre_ShotgunFireBullet(SentryGunInstance_Firing_Bullets __instance)
        {
            if (__instance.m_fireMode != eWeaponFireMode.SentryGunShotgunSemi) return;
            if (!__instance.m_archetypeData.Sentry_FireTowardsTargetInsteadOfForward || !__instance.m_core.TryGetTargetAimPos(out var pos)) return;

            _cachedDir = __instance.MuzzleAlign.forward;
            __instance.MuzzleAlign.forward = (pos - __instance.MuzzleAlign.position).normalized;
        }

        [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_ShotgunFireBullet(SentryGunInstance_Firing_Bullets __instance)
        {
            if (_cachedDir != null)
            {
                __instance.MuzzleAlign.forward = (Vector3)_cachedDir;
                _cachedDir = null;
            }
        }
    }
}
