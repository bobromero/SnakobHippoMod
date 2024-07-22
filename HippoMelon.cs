using Il2Cpp;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Collections;
using SlapshotModdingUtils;
using static Il2CppRewired.UI.ControlMapper.ControlMapper;
using System.Reflection.Metadata.Ecma335;
using Il2CppInterop.Runtime;

namespace SnakobHippoMod {
    public class HippoMelon : MelonMod {
        public List<GameObject> prefabs = new List<GameObject>();
        public static GameObject? HippoBottle;
        public static GameObject? HippoMini;
        public static GameObject? Hippo;

        public static Transform? hippoContainer;
        public static bool spawnedhippos = false;

        private static HippoMelon _instance;
        public static HippoMelon Instance => _instance;


        [HarmonyPatch(typeof(NetWaterBottleSpawner))]
        [HarmonyPatch(nameof(NetWaterBottleSpawner.SpawnWaterBottle))] // if possible use nameof() here
        class Patch01
        {
            static bool Prefix(NetWaterBottleSpawner __instance)
            {
                if (HippoBottle && __instance.waterBottlePrefab != HippoBottle)
                {
                    Melon<HippoMelon>.Logger.Msg("Changing prefab");
                    __instance.waterBottlePrefab = HippoBottle;
                }

                return true;
            }

            static void Postfix(NetWaterBottleSpawner __instance)
            {
                Melon<HippoMelon>.Logger.Msg(__instance.waterBottleInstance.name);
                __instance.waterBottleInstance.transform.rotation = Quaternion.EulerAngles(0f, 180f, 0f);
            }

        }

        [HarmonyPatch(typeof(HatTrickManager))]
        [HarmonyPatch(nameof(HatTrickManager.HatTrick))] // if possible use nameof() here
        class Patch02 {
            static bool Prefix(HatTrickManager __instance, ref Player player) {
                IEnumerator SetHipposInactiveAfterDelay(GameObject hippoobject)
                {
                    MelonLogger.Msg("Called");
                    yield return new WaitForSeconds(4);
                    MelonLogger.Msg("Waited");
                    hippoobject.SetActive(false);
                    MelonLogger.Msg("Set Inactive");
                }

                if (HippoMini) 
                {
                    for (int i = 0; i < __instance.hatContainer.childCount; i++)
                    {
                        __instance.hatContainer.transform.GetChild(i).gameObject.SetActive(true);
                        MelonCoroutines.Start(SetHipposInactiveAfterDelay(__instance.hatContainer.transform.GetChild(i).gameObject));
                    }
                }

                return true;
            }

            //static void Postfix(NetWaterBottleSpawner __instance) {
            //    //Melon<HippoMelon>.Logger.Msg(__instance.waterBottleInstance.name);
            //    //__instance.waterBottleInstance.transform.rotation = Quaternion.EulerAngles(0f, 0f, 0f);
            //}


        }


        [HarmonyPatch(typeof(ArenaManager), "Initialize")] // if possible use nameof() here
        class Patch03
        {
            public static bool hasRan = false;

            static void Postfix()
            {
                if (hasRan) { return; }
                HatTrickManager hatTrickManager = UnityEngine.Object.FindObjectOfType<HatTrickManager>();

                if (hatTrickManager != null)
                {
                    var hc = hatTrickManager.hatContainer;
                    for (int i = 0; i < hc.childCount; i++)
                    {
                        GameObject.Destroy(hatTrickManager.hatContainer.transform.GetChild(i).gameObject);
                    }

                    for (int i = 0; i < 13; i++)
                    {
                        var go = GameObject.Instantiate(HippoMini);
                        go.transform.SetParent(hatTrickManager.hatContainer, false);
                        go.SetActive(false);
                    }
                }
                else
                {
                    Melon<HippoMelon>.Logger.Msg("HatTrickManager not found.");
                }
                hasRan = true;
            }
        }

        [HarmonyPatch(typeof(Game))]
        [HarmonyPatch(nameof(Game.OnGoalScored))] // if possible use nameof() here
        class Patch04
        {
            static void Postfix(Game __instance)
            {
                Melon<HippoMelon>.Logger.Warning("Started");
                GameObject puck = GameObject.Find("puck(Clone)");
                if (puck != null)
                {
                    Melon<HippoMelon>.Logger.Warning("Found puck");
                    Vector3 puckPosition = puck.transform.position;

                    System.Random rnd = new System.Random();
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject hippoInstance = GameObject.Instantiate(Hippo, puckPosition, Quaternion.identity);
                        Melon<HippoMelon>.Logger.Warning("Instantiated HippoMini");

                        float x = rnd.Next(-50, 50);
                        float y = rnd.Next(0, 20);
                        float z = rnd.Next(-59, 20);

                        Vector3 forceDirection = new Vector3(x, y, z);
                        MelonLogger.Msg(forceDirection);

                        MelonCoroutines.Start(SimulateForce(hippoInstance, forceDirection));
                    }
                }
                else
                {
                    Melon<HippoMelon>.Logger.Warning("Puck not found!");
                }

                IEnumerator SimulateForce(GameObject hippoInstance, Vector3 forceDirection)
                {
                    float duration = 1.0f; // Duration over which to apply the "force"
                    float elapsedTime = 0f;
                    Vector3 startPosition = hippoInstance.transform.position;
                    Vector3 targetPosition = forceDirection;

                    while (elapsedTime < duration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / duration;

                        // Linearly interpolate position
                        hippoInstance.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                        yield return null;
                    }

                    hippoInstance.transform.position = targetPosition;
                    yield return new WaitForSeconds(5);
                    GameObject.Destroy(hippoInstance.gameObject);

                }
            }   
        }

        [HarmonyPatch(typeof(MainMenuBehaviour))]
        [HarmonyPatch(nameof(MainMenuBehaviour.Awake))]
        class Patch05
        {
            static void Postfix()
            {
                Patch03.hasRan = false;
            }
        }

        public override void OnInitializeMelon() {

        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("Robb&Wallie.SnakobHippoMod");
        harmony.PatchAll();
        Melon<HippoMelon>.Logger.Msg("Initialized");


        prefabs = SlapshotModdingUtils.AssetBundleHelper.LoadAssets(System.Reflection.Assembly.GetExecutingAssembly(),GetType().Namespace);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                MelonLogger.Msg(resourceName);
            }

            Melon<HippoMelon>.Logger.Msg(prefabs.Count);
        foreach (var prefab in prefabs) {
                Melon<HippoMelon>.Logger.Msg(prefab.name);
            if (prefab.name == "HippoBottle") {
                Melon<HippoMelon>.Logger.Msg("Mini Hippo Found!: " + prefab.name);
                HippoBottle = prefab;
            }
            if(prefab.name == "HippoSmallRagdoll") {
                Melon<HippoMelon>.Logger.Msg("Mini ragdoll Hippo Found!: " + prefab.name);
                HippoMini = prefab;
            }
            if(prefab.name == "Hippo") {
                Melon<HippoMelon>.Logger.Msg("Big ragdoll Hippo Found!: " + prefab.name);
                Hippo = prefab;
            }
                
        }

        }

        public override void OnUpdate() {
            
        }
    }
}