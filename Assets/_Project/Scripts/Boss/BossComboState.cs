using BossFight.Combat;
using BossFight.Core;
using UnityEngine;

namespace BossFight.Boss
{
    internal sealed class BossComboState : IState
    {
        private readonly BossController boss;
        private AttackData strike;
        private float elapsed;
        private bool opened;
        private bool sampledEnd;

        public int Index { get; private set; }
        public float WindupRemaining => strike != null ? Mathf.Max(0f, strike.Windup - elapsed) : 0f;

        public BossComboState(BossController boss) => this.boss = boss;

        public void Enter() => StartStrike(0);

        public void Tick(float dt)
        {
            float activeEnd = strike.Windup + strike.Active;
            if (elapsed < strike.Windup - boss.Config.AimLockLead)
            {
                boss.Motor.Face(boss.DirectionToTarget(), boss.Config.TurnSpeed, dt);
            }

            float moveTime = Mathf.Clamp(activeEnd - elapsed, 0f, dt);
            float travel = strike.LungeDistance * moveTime / Mathf.Max(0.01f, activeEnd);
            boss.Motor.Move(boss.transform.forward * (travel / dt), dt);
            elapsed += dt;

            if (elapsed < strike.Windup)
            {
                boss.Weapons.Windup(Index, elapsed / strike.Windup);
                return;
            }

            if (!opened)
            {
                opened = true;
                boss.Weapons.Swing(Index, 0f);
                boss.Weapons.Hitbox.Begin(boss.gameObject, strike.Damage, strike.GuardDamage,
                    strike.Active, targetFilter: boss.Target);
            }

            if (!sampledEnd)
            {
                float fraction = Mathf.Clamp01((elapsed - strike.Windup) / Mathf.Max(0.01f, strike.Active));
                boss.Weapons.Swing(Index, fraction);
                sampledEnd = elapsed >= activeEnd;
                // 留给 LateUpdate 一次扫掠，即使低帧率跳过了整个判定段。
                return;
            }

            if (boss.Weapons.Hitbox.IsActive)
            {
                boss.Weapons.Hitbox.End();
                boss.Weapons.Rest();
            }
            if (elapsed >= strike.Duration)
            {
                if (Index + 1 < boss.Config.ComboLength)
                {
                    StartStrike(Index + 1);
                }
                else
                {
                    boss.ToApproach();
                }
            }
        }

        public void Exit()
        {
            boss.Weapons.Hitbox.End();
            boss.Brain.FinishCombo(boss.ActionTime);
        }

        private void StartStrike(int index)
        {
            Index = index;
            strike = boss.Config.GetStrike(index);
            elapsed = 0f;
            opened = false;
            sampledEnd = false;
            boss.Weapons.Windup(Index, 0f);
        }
    }
}
