using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    public interface ICombatTarget
    {
        string TargetName { get; }
        int Hp { get; }
        int MaxHp { get; }
        bool IsDead { get; }
        bool IsBoss { get; }
        Transform AimPoint { get; }
        bool IsHeadshot(Vector3 point);
        bool IsAirborne { get; }
        void TakeDamage(int damage, bool headshot, bool melee, bool airborne, Vector3 hitPoint);
    }

    public static class CombatRegistry
    {
        public static readonly List<CombatTarget> Targets = new List<CombatTarget>();

        public static void Register(CombatTarget target)
        {
            if (target != null && !Targets.Contains(target)) Targets.Add(target);
        }

        public static void Unregister(CombatTarget target)
        {
            Targets.Remove(target);
        }

        public static CombatTarget Nearest(Vector3 from, float maxDistance, Vector3 forward)
        {
            CombatTarget best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < Targets.Count; i++)
            {
                var t = Targets[i];
                if (t == null || t.IsDead) continue;
                if (Mathf.Abs(t.transform.position.y - from.y) > 8f) continue;
                Vector3 to = t.transform.position - from;
                float d = to.magnitude;
                if (d > maxDistance) continue;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                float dot = flat.sqrMagnitude < 0.001f ? 1f : Vector3.Dot(forward, flat.normalized);
                if (dot < 0.1f) continue;
                float score = d - dot * 3f;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            return best;
        }
    }

    public abstract class CombatTarget : MonoBehaviour, ICombatTarget
    {
        public abstract string TargetName { get; }
        public abstract int Hp { get; }
        public abstract int MaxHp { get; }
        public abstract bool IsDead { get; }
        public virtual bool IsBoss { get { return false; } }
        public virtual Transform AimPoint { get { return transform; } }
        public virtual bool IsAirborne { get { return false; } }
        public virtual bool IsHeadshot(Vector3 point) { return point.y > transform.position.y + 0.35f; }
        public abstract void TakeDamage(int damage, bool headshot, bool melee, bool airborne, Vector3 hitPoint);

        protected virtual void OnEnable() { CombatRegistry.Register(this); }
        protected virtual void OnDisable() { CombatRegistry.Unregister(this); }
    }
}
