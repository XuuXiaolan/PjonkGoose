﻿using System;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameNetcodeStuff;
using PjonkGooseEnemy.Util.Spawning;
using PjonkGooseEnemy.Util.Extensions;
using PjonkGooseEnemy.EnemyStuff;

namespace PjonkGooseEnemy.Patches;

[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch {
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);

	}

	private static void CreateNetworkManager()
	{
		if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
		{
			if (PjonkGooseEnemyUtils.Instance == null) {
				GameObject utilsInstance = GameObject.Instantiate(Plugin.UtilsPrefab);
				SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
				utilsInstance.GetComponent<NetworkObject>().Spawn();
				Plugin.Logger.LogInfo($"Created PjonkGooseUtils. Scene is: '{utilsInstance.scene.name}'");
			} else {
				Plugin.Logger.LogWarning("PjonkGooseUtils already exists?");
			}
		}
	}
}

[HarmonyPatch(typeof(Landmine))]
static class LandminePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    static void PatchOnTriggerEnter(Landmine __instance, Collider other, ref float ___pressMineDebounceTimer)
    {
		PjonkGooseAI component = other.gameObject.GetComponent<PjonkGooseAI>();
		if (component != null && !component.isEnemyDead)
		{
			___pressMineDebounceTimer = 0.5f;
			__instance.PressMineServerRpc();
		}
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerExit")]
    static void PatchOnTriggerExit(Landmine __instance, Collider other, ref bool ___sendingExplosionRPC)
    {
		PjonkGooseAI component = other.gameObject.GetComponent<PjonkGooseAI>();
		if (component != null && !component.isEnemyDead)
		{
			if (!__instance.hasExploded)
			{
				__instance.SetOffMineAnimation();
				___sendingExplosionRPC = true;
				__instance.ExplodeMineServerRpc();
			}
		}
	}
}

[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(PlayerControllerB.TeleportPlayer))]
	static void PatchTeleportPlayer(PlayerControllerB __instance, Vector3 pos, bool withRotation, float rot, bool allowInteractTrigger, bool enableController)
	{
		foreach (PjonkGooseAI pjonkGooseAI in UnityEngine.Object.FindObjectsOfType<PjonkGooseAI>())
		{
			if (pjonkGooseAI.targetPlayer == __instance) pjonkGooseAI.positionOfPlayerBeforeTeleport = __instance.transform.position;
		}
	}
}

[HarmonyPatch(typeof(MineshaftElevatorController))]
static class MineshaftElevatorControllerPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(nameof(MineshaftElevatorController.Update))]
	static void PatchUpdate(MineshaftElevatorController __instance)
	{
		if (__instance.elevatorFinishedMoving)
        {
            if (__instance.movingDownLastFrame)
            {
                __instance.elevatorIsAtBottom = true;
            }
            else
            {
                __instance.elevatorIsAtBottom = false;
            }
        }
	}
}