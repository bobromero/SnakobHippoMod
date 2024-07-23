using Il2Cpp;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Collections;
using SlapshotModdingUtils;
using Il2CppInterop.Runtime.Injection;

namespace SnakobHippoMod {
    public class HippoMelon : MelonMod {

        public enum HippoSize
        {
            small,
            BIG,
        }

        #region MelonCategories
        public MelonPreferences_Category HippoBottlemus;
        public MelonPreferences_Category HippoGoalExplosion;
        public MelonPreferences_Category GoalExplosionSettings;
        public MelonPreferences_Category HippoHatTrick;
        #endregion

        #region MelonPrefEntries

        public MelonPreferences_Entry<bool> UseGoalExplosion;
        public MelonPreferences_Entry<bool> PersonalGoalExplosions;
        public MelonPreferences_Entry<MelonPreferences_Category> GoalExplosionSettingsCategory;
        public MelonPreferences_Entry<int> NumBigHippos;
        public MelonPreferences_Entry<int> NumSmallHippos;


        public MelonPreferences_Entry<bool> UseHippoBottle;

        public MelonPreferences_Entry<bool> UseHippoHatTrick;
        public MelonPreferences_Entry<HippoSize> HatTrickHippoSize;


        #endregion



        #region modVars
        public List<GameObject> prefabs = new List<GameObject>();
        public static GameObject? HippoBottle;
        public static GameObject? HippoMini;
        public static GameObject? Hippo;

        public static Transform? hippoContainer;
        public static bool spawnedhippos = false;

        #endregion

        #region Patches

        [HarmonyPatch(typeof(NetWaterBottleSpawner))]
        [HarmonyPatch(nameof(NetWaterBottleSpawner.SpawnWaterBottle))] // if possible use nameof() here
        class Patch01
        {
            public static bool hasRan = false;
            public static int counter = 0;

            static void Postfix(NetWaterBottleSpawner __instance)
            {
                if (hasRan)
                {
                    hasRan = false;
                    return;   
                }
                if (!Melon<HippoMelon>.Instance.UseHippoBottle.Value)
                {
                    return;
                }
                if (counter % 2 == 0)
                {
                    var hippoBots = GameObject.FindObjectsOfType<HippoBottle>();
                    foreach (var bottle in hippoBots)
                    {
                        GameObject.Destroy(bottle.gameObject);
                    }
                }
                
                hasRan = true;
                __instance.waterBottleInstance.transform.rotation = Quaternion.EulerAngles(0f, 180f, 0f);
                Transform temp = __instance.waterBottleInstance.transform;
                GameObject.Destroy(__instance.waterBottleInstance);
                var go = GameObject.Instantiate(HippoBottle);
                counter++;
                go.AddComponent<HippoBottle>();
                go.transform.position = temp.position;
                go.transform.rotation = temp.rotation;
                //Melon<HippoMelon>.Logger.Msg(__instance.waterBottleInstance.name);

            }

        }

        [HarmonyPatch(typeof(HatTrickManager))]
        [HarmonyPatch(nameof(HatTrickManager.HatTrick))] // if possible use nameof() here
        class Patch02
        {
            static void Prefix(HatTrickManager __instance, ref Player player)
            {
                if (!Melon<HippoMelon>.Instance.UseHippoHatTrick.Value)
                {
                    return;
                }

                IEnumerator ActivateHipposAffterDelay(GameObject hippoobject)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.RandomRange(0f, 1f));
                    hippoobject.SetActive(true);
                    var rb = hippoobject.GetComponent<Rigidbody>();
                    rb.velocity = Vector3.zero;
                    hippoobject.transform.GetChild(0).GetChild(0).GetComponent<Rigidbody>().velocity = Vector3.zero;
                    Vector3 spawnPos = __instance.spawns[UnityEngine.Random.RandomRange(0, __instance.spawns.Count)].transform.position;
                    hippoobject.transform.position = spawnPos;


                    //super mega workaround going to blow up bad computers maybe
                    var rbr = hippoobject.transform.GetComponentsInChildren<Rigidbody>(true);
                    //MelonLogger.Msg("size of rbr: " + rbr.Count);
                    foreach (var rigidbody in rbr)
                    {
                        rigidbody.velocity = Vector3.zero;
                    }
                    var force = new Vector3(-hippoobject.transform.position.normalized.x, .5f, -hippoobject.transform.position.normalized.z) * 100000f;
                    hippoobject.transform.GetChild(0).GetChild(0).GetComponent<Rigidbody>().AddForce(force);

                    MelonCoroutines.Start(DeactivateHipposAfterDelay(hippoobject));
                }

                IEnumerator DeactivateHipposAfterDelay(GameObject hippoobject)
                {

                    //MelonLogger.Msg("Called");
                    yield return new WaitForSeconds(8);
                    //MelonLogger.Msg("Waited");
                    hippoobject.transform.position = Vector3.zero;
                    hippoobject.transform.GetChild(0).GetChild(0).position = Vector3.zero;
                    hippoobject.SetActive(false);
                    //MelonLogger.Msg("Set Inactive");
                }

                if (HippoMini && Hippo)
                {
                    var hippoContainer = __instance.hatContainer.transform;

                    for (int i = 0; i < hippoContainer.childCount; i++)
                    {
                        var hip = hippoContainer.GetChild(i).gameObject;
                        MelonCoroutines.Start(ActivateHipposAffterDelay(hip));
                    }
                }
            }

        }


        [HarmonyPatch(typeof(ArenaManager), "Initialize")] // if possible use nameof() here
        class Patch03
        {
            public static bool hasRan = false;


            static void Postfix()
            {
                var melinst = Melon<HippoMelon>.Instance;
                if (!melinst.UseHippoHatTrick.Value)
                {
                    return;
                }
                if (hasRan) { 
                    hasRan = false;
                    return; }
                HatTrickManager hatTrickManager = UnityEngine.Object.FindObjectOfType<HatTrickManager>();

                if (hatTrickManager != null)
                {
                    var hc = hatTrickManager.hatContainer;
                    for (int i = 0; i < hc.childCount; i++)
                    {
                        GameObject.Destroy(hatTrickManager.hatContainer.transform.GetChild(i).gameObject);
                    }
                    var hippoSize = melinst.HatTrickHippoSize.Value == HippoSize.BIG ? Hippo : HippoMini;
                    for (int i = 0; i < 13; i++)
                    {
                        var go = GameObject.Instantiate(hippoSize);
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
            public static bool hasRan = false;
            public static bool UserScored = false;


            static void Prefix(Game __instance, ref GoalScoredPacket goalScored)
            {
                UserScored = goalScored.ScorerID == __instance.GetLocalPlayerId();

            }

            static void Postfix(Game __instance)
            {
                var melinst = Melon<HippoMelon>.Instance;

                if (!melinst.UseGoalExplosion.Value)
                {
                    return;
                }
                bool didIScore = melinst.PersonalGoalExplosions.Value && !UserScored;
                if (didIScore)
                {
                    return;
                }

                if (hasRan)
                {
                    //MelonLogger.Error("not spawning more hippos");
                    hasRan = false;
                    return;
                }
                //Melon<HippoMelon>.Logger.Warning("Started");
                GameObject puck = AppManager.Instance.game.Pucks[0].gameObject;
                if (puck != null)
                {
                    //Melon<HippoMelon>.Logger.Warning("Found puck");
                    Vector3 puckPosition = puck.transform.position;

                    System.Random rnd = new System.Random();
                    for (int i = 0; i < melinst.NumBigHippos.Value; i++)
                    {
                        GameObject hippoInstance = GameObject.Instantiate(Hippo, puckPosition, Quaternion.identity);

                        float x = rnd.Next(-50, 50);
                        float y = rnd.Next(0, 20);
                        float z = rnd.Next(-59, 20);

                        Vector3 forceDirection = new Vector3(x, y, z);
                        //MelonLogger.Msg(forceDirection);

                        MelonCoroutines.Start(SimulateForce(hippoInstance, forceDirection));
                    }


                    for (int i = 0; i < melinst.NumSmallHippos.Value; i++)
                    {
                        GameObject hippoInstance = GameObject.Instantiate(HippoMini, puckPosition, Quaternion.identity);

                        float x = rnd.Next(-50, 50);
                        float y = rnd.Next(0, 20);
                        float z = rnd.Next(-59, 20);

                        Vector3 forceDirection = new Vector3(x, y, z);
                        //MelonLogger.Msg(forceDirection);

                        MelonCoroutines.Start(SimulateForce(hippoInstance, forceDirection));
                    }
                    hasRan = true;
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



        #endregion



        public override void OnInitializeMelon() {

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("Robb&Wallie.SnakobHippoMod");
            harmony.PatchAll();
            Melon<HippoMelon>.Logger.Msg("Initialized");

            #region MelonPrefs

            HippoBottlemus = MelonPreferences.CreateCategory("HippoBottlemus");
            UseHippoBottle = HippoBottlemus.CreateEntry<bool>("Use Hippos as Bottles", true);

            HippoGoalExplosion = MelonPreferences.CreateCategory("Hippo Goal Explosions");
            UseGoalExplosion = HippoGoalExplosion.CreateEntry<bool>("Use Hippo Goal Explosions", true);
            PersonalGoalExplosions = HippoGoalExplosion.CreateEntry<bool>("Only show Hippos on your goals", false);
            GoalExplosionSettingsCategory = HippoGoalExplosion.CreateEntry("Settings for your goal explosions", GoalExplosionSettings);
            GoalExplosionSettings = MelonPreferences.CreateCategory("Goal Explosion Settings");
            NumBigHippos = GoalExplosionSettings.CreateEntry("Number of big Hippos", 2);
            NumSmallHippos = GoalExplosionSettings.CreateEntry("Number of small Hippos", 3);


            HippoHatTrick = MelonPreferences.CreateCategory("Hippo Hattrick");
            UseHippoHatTrick = HippoHatTrick.CreateEntry<bool>("Use Hippos as hattricks", true);
            HatTrickHippoSize = HippoHatTrick.CreateEntry<HippoSize>("Hattrick Hippo Size", HippoSize.BIG);

            #endregion




            #region GettingAssets

            prefabs = AssetBundleHelper.LoadAssets(System.Reflection.Assembly.GetExecutingAssembly(), GetType().Namespace);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            

            Melon<HippoMelon>.Logger.Msg("Got Hippos: "+prefabs.Count);
            foreach (var prefab in prefabs)
            {
                //Melon<HippoMelon>.Logger.Msg(prefab.name);
                
                if (prefab.name == "HippoBottle")
                {
                    //Melon<HippoMelon>.Logger.Msg("Mini Hippo Found!: " + prefab.name);
                    HippoBottle = prefab;
                }
                if (prefab.name == "HippoSmallRagdoll")
                {
                    //Melon<HippoMelon>.Logger.Msg("Mini ragdoll Hippo Found!: " + prefab.name);
                    HippoMini = prefab;
                }
                if (prefab.name == "Hippo")
                {
                    //Melon<HippoMelon>.Logger.Msg("Big ragdoll Hippo Found!: " + prefab.name);
                    Hippo = prefab;
                }


            }

            #endregion



        }

        //public override void OnUpdate() {
        //    if (Input.GetKeyDown(KeyCode.P)) {
        //        try
        //        {
        //            var htm = GameObject.FindObjectOfType<HatTrickManager>();

        //            htm.HatTrick(AppManager.Instance.game.localPlayer, 3);
        //        }
        //        catch
        //        {
        //            Melon<HippoMelon>.Logger.Warning("ho Hat TrickManager found");
        //        }
        //    }
        //}
    }


    [RegisterTypeInIl2Cpp]
    public class HippoBottle : MonoBehaviour
    {

        public HippoBottle(IntPtr ptr) : base(ptr) { }

        // Optional, only used in case you want to instantiate this class in the mono-side
        // Don't use this on MonoBehaviours / Components!
        public HippoBottle() : base(ClassInjector.DerivedConstructorPointer<HippoBottle>()) => ClassInjector.DerivedConstructorBody(this);
    }
}