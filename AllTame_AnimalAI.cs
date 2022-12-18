using HarmonyLib;
using System;
//using CreatureLevelControl;
using System.Collections.Generic;
using UnityEngine;

namespace AllTameable
{
    public class AllTame_AnimalAI : MonsterAI
    {

        public float m_safetime = 8f;
        //private new readonly float m_updateTargetFarRange = 32f;
       // private new readonly float m_updateTargetIntervalFar = 10f;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
        //private static class Prefix_MonsterAI_UpdateAI
        //{

            private static bool Prefix(AllTame_AnimalAI __instance, float dt)
            {
                if (typeof(AllTame_AnimalAI) == __instance.GetType())
                {
                    __instance.UpdateAI_AllTameable(dt);
                    //DBG.blogDebug("AllTame");
                    return false;
                }
                //DBG.blogDebug("Not AllTame");
                return true;
            }
        //}

        private void UpdateAI_AllTameable(float dt)
        {

            if (m_nview.IsOwner())
            {

                UpdateTakeoffLanding(dt);
                if (m_jumpInterval > 0f)
                {
                    m_jumpTimer += dt;
                }
                if (m_randomMoveUpdateTimer > 0f)
                {
                    m_randomMoveUpdateTimer -= dt;
                }
                UpdateRegeneration(dt);
                m_timeSinceHurt += dt;

                if ((m_afraidOfFire && AvoidFire(dt, null, true)))
                {
                    return;
                }
                m_updateTargetTimer -= dt;
                if (m_updateTargetTimer <= 0f)
                {
                    m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 32f) ? 2f : 10f);
                    Character character = FindEnemy();
                    if ((bool)character)
                    {
                        m_targetCreature = character;
                    }
                }
                Humanoid humanoid = m_character as Humanoid;
                if (m_targetCreature != null)
                {
                    if (m_targetCreature.IsDead()) { m_targetCreature = null; }
                }

                if ((bool)m_targetCreature)
                {
                    bool targetinrange = CanSenseTarget(m_targetCreature);
                    SetTargetInfo(m_targetCreature.GetZDOID());
                    if (targetinrange)
                    {
                        base.SetAlerted(true);
                    }
                }
                else
                {
                    SetTargetInfo(ZDOID.None);
                }
                if ((bool)m_tamable && (bool)m_tamable.m_saddle && m_tamable.m_saddle.UpdateRiding(dt))
                {
                    return;
                }
                if (IsAlerted())
                {
                    m_timeSinceSensedTargetCreature += dt;
                    if (m_timeSinceSensedTargetCreature > m_safetime)
                    {
                        m_targetCreature = null;
                        base.SetAlerted(false);
                        //SetTameAlerted(alert: false);
                    }
                }

                if ((bool)m_targetCreature)
                {
                    Flee(dt, m_targetCreature.transform.position);
                    m_targetCreature.OnTargeted(sensed: false, alerted: false);
                }
                else if ((!IsAlerted() || (m_targetStatic == null && m_targetCreature == null)) && UpdateConsumeItem(humanoid, dt))
                {
                    m_aiStatus = "Consume item";
                    return;
                }
                else
                {
                    if ((bool)m_follow)
                    {
                        Follow(m_follow, dt);
                        m_aiStatus = "Follow";
                        return;
                    }
                    //DBG.blogDebug("Idle");
                    IdleMovement(dt);
                }
            }
            else
            {
                m_alerted = m_nview.GetZDO().GetBool("alert");
            }
        }

    }
}
