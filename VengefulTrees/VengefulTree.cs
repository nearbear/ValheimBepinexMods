using BepInEx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VengefulTrees
{
    public class VengefulTree : MonoBehaviour
    {
        public Player player;

        private static Vector3 XZPlane(Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public class TreeHatredLevel
        {
            public float force;
            public float duration;
            public float chances;
            public string description;
            // TODO: sound??
        }

        public static float forceMultiplier = 1f;

        public static readonly TreeHatredLevel KINDA_MAD = new TreeHatredLevel() {          force = .9f,    duration = 15f, chances = 30,   description = "kinda mad" };
        public static readonly TreeHatredLevel SOMEWHAT_FURIOUS = new TreeHatredLevel() {   force = 2f,     duration = 10f, chances = 20,   description = "somewhat furious" };
        public static readonly TreeHatredLevel FUCKING_PISSED = new TreeHatredLevel() {     force = 4f,     duration = 15f, chances = 5,    description = "fucking pissed" };

        public static readonly List<TreeHatredLevel> treeHatredLevels = new List<TreeHatredLevel>()
        {
            KINDA_MAD, SOMEWHAT_FURIOUS, FUCKING_PISSED
        };

        public static void CheckTreeHatred(GameObject treeLogObject)
        {
            TreeLog treeLog = treeLogObject.GetComponent<TreeLog>();
            Player player = Player.GetClosestPlayer(treeLog.transform.position, 20f);

            if (!player)
            {
                return;
            }

            TreeHatredLevel level = GetTreeHatredLevel(player);
            if (level != null)
            {
                Debug.Log(string.Format("A{1} {0} tree is seeking vengence!", level.description, "aeiouAEIOU".IndexOf(level.description) >= 0 ? "n" : ""));
                
                VengefulTree vengefulTree = treeLogObject.AddComponent<VengefulTree>();
                vengefulTree.player = player;

                // Give it some fat 5x loot
                treeLog.m_dropWhenDestroyed.m_dropMin *= 5;
                treeLog.m_dropWhenDestroyed.m_dropMax *= 5;

                // Don't let it get stuck in the stump, move it up a bit
                treeLog.transform.position = treeLog.transform.position + .5f * Vector3.up;

                // Drop the velocity applied by the axe hit
                Rigidbody rigidbody = treeLog.GetComponent<Rigidbody>();
                rigidbody.ResetInertiaTensor();
                
                vengefulTree.StartCoroutine(vengefulTree.SeekVengence(forceMultiplier * level.force, level.duration));
            }
        }

        public static TreeHatredLevel GetTreeHatredLevel(Player player)
        {
            float chanceOfVengence = .005f;
            if (Main.debugMode.Value)
            {
                chanceOfVengence = 99999f;
            }
            if (player.GetCurrentBiome() == Heightmap.Biome.BlackForest)
            {
                // Very spoopy envrionment, is only fitting that trees come alive more often there
                chanceOfVengence *= 2f;
            }
            if (Utility.TryParseAssemblyName("EasyTreesy", out AssemblyName asdfdsafads))
            {
                // Marks you as an enemy of treekind everywhere
                chanceOfVengence *= 5f;
            }
            string characterName = Game.instance.GetPlayerProfile().GetName().ToLower();
            if (characterName.Contains("steve") || characterName.StartsWith("ste"))
            {
                // Immortal legend that can destroy trees with his bare fists. Trees hate him.
                chanceOfVengence *= 5f;
            }

            if (Random.Range(0f, 1f) > chanceOfVengence)
            {
                return null;
            }

            float totalChances = treeHatredLevels.Sum(item => item.chances);
            float chance = Random.Range(0, totalChances);

            TreeHatredLevel selectedLevel = null;
            foreach (TreeHatredLevel level in treeHatredLevels)
            {
                selectedLevel = level;
                chance -= level.chances;

                if(chance < 0)
                {
                    return selectedLevel;
                }
            }
            return selectedLevel;
        }

        // TODO: Add more than one behavior? Maybe a "helicopter" one or an "ICBM" one
        public IEnumerator SeekVengence(float force, float duration)
        {
            float startTime = Time.time;
            float endTime = Time.time + duration;
            int orientation = 1;

            while (Time.time < endTime && !player.IsDead())
            {
                float elapsed = Time.time - startTime;

                Vector3 playerPosition = player.transform.position;

                Vector3 top = transform.position + 4f * orientation * transform.up;
                Vector3 bottom = transform.position - 4f * orientation * transform.up;
                Vector3 axis = top - bottom;
                /*if (top.y < bottom.y)
                {
                    Vector3 swap = top;
                    top = bottom;
                    bottom = swap;

                    rigidbody.AddForceAtPosition(rigidbody.mass * Vector3.up, top, ForceMode.Impulse);
                    orientation *= -1;
                }*/
                // TODO: Add flipping effect, like a slinky

                Vector3 forwardDirection = (playerPosition - top + Vector3.ProjectOnPlane(top - playerPosition, axis)).normalized;

                // TODO: Add interpolation so higher velocities can be used?
                Vector3 tangentDirection = XZPlane(Vector3.Cross(axis, Vector3.up)).normalized;
                Plane playerToTree = new Plane(bottom - playerPosition, playerPosition);
                Ray ray = new Ray(top, tangentDirection);
                if (!playerToTree.Raycast(ray, out float whoCAres))
                {
                    tangentDirection *= -1f;
                }

                Vector3 forcePosition = top;
                Vector3 forceDirection = (tangentDirection + forwardDirection).normalized;

                float angleFrom = Vector3.Angle(XZPlane(playerPosition), XZPlane(top));

                transform.GetComponent<Rigidbody>().AddForceAtPosition(force * forceDirection, forcePosition, ForceMode.VelocityChange);
                // TODO: Add heavy impulse up if player tries to jump over log
                // TODO: Add "clanging" effect because that wo  uld be very scary

                yield return new WaitForFixedUpdate();
            }
        }

        private void OnDestroy()
        {
            // There's no XP overflow so just use a big number
            player.RaiseSkill(Skills.SkillType.WoodCutting, 999999999999f);
        }
    }
}
