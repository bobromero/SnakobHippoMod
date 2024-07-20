using Il2Cpp;
using MelonLoader;
using UnityEngine;
using HarmonyLib;

namespace SnakobHippoMod {
    public class HippoMelon : MelonMod {
        public List<GameObject> prefabs = new List<GameObject>();
        public static GameObject? HippoBottle;

        [HarmonyPatch(typeof(NetWaterBottleSpawner))]
        [HarmonyPatch(nameof(NetWaterBottleSpawner.SpawnWaterBottle))] // if possible use nameof() here
        class Patch01 {
            static bool Prefix(NetWaterBottleSpawner __instance) {
                if (HippoBottle && __instance.waterBottlePrefab != HippoBottle) {
                    Melon<HippoMelon>.Logger.Msg("Changing prefab");
                    __instance.waterBottlePrefab = HippoBottle;
                }

                return true;
            }

            static void Postfix(NetWaterBottleSpawner __instance) {
                //Melon<HippoMelon>.Logger.Msg(__instance.waterBottleInstance.name);
                __instance.waterBottleInstance.transform.rotation = Quaternion.EulerAngles(0f, 0f, 0f);
            }


        }
        


        public override void OnInitializeMelon() {

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("Robb&Wallie.SnakobHippoMod");
            harmony.PatchAll();
            Melon<HippoMelon>.Logger.Msg("Initialized");


            prefabs = SlapshotModdingUtils.AssetBundleHelper.LoadAssets(System.Reflection.Assembly.GetExecutingAssembly(),GetType().Namespace);

            foreach (var prefab in prefabs) {
                if (prefab.name == "HippoBottle") {
                    Melon<HippoMelon>.Logger.Msg("Mini Hippo Found!: " + prefab.name);
                    HippoBottle = prefab;
                }
                if(prefab.name == "HippoSmallRagdoll") {
                    Melon<HippoMelon>.Logger.Msg("Mini ragdoll Hippo Found!: " + prefab.name);
                }
                if(prefab.name == "Hippo") {
                    Melon<HippoMelon>.Logger.Msg("Big ragdoll Hippo Found!: " + prefab.name);
                }
                
            }

        }
        public override void OnUpdate() {
            
        }
    }
}