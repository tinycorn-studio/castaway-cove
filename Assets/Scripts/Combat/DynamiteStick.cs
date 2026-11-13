using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    public class DynamiteStick : MonoBehaviour
    {
        public float Fuse = 1.6f;
        public float Radius = 6f;
        public int Damage = 60;

        private GameBoot game;
        private bool exploded;

        public void Init(GameBoot g, float fuse)
        {
            game = g;
            Fuse = fuse;
        }

        private void Update()
        {
            Fuse -= Time.deltaTime;
            if (Fuse <= 0f && !exploded) Explode();
        }

        private void Explode()
        {
            exploded = true;
            if (game != null)
            {
                var hits = Physics.OverlapSphere(transform.position, Radius, ~0, QueryTriggerInteraction.Collide);
                var damaged = new HashSet<CombatTarget>();
                for (int i = 0; i < hits.Length; i++)
                {
                    var target = hits[i].GetComponentInParent<CombatTarget>();
                    if (target == null || target.IsDead || damaged.Contains(target)) continue;
                    damaged.Add(target);
                    target.TakeDamage(Damage, false, false, false, transform.position);
                }
                if (game.Player != null && Vector3.Distance(game.Player.transform.position, transform.position) < 4f && game.Combat != null)
                    game.Combat.TakeDamage(15);
                if (game.Audio != null) game.Audio.Play(game.Audio.Snap, 0.9f, 0.7f);
                if (game.UI != null) game.UI.Toast("Boom!");
            }
            Destroy(gameObject);
        }
    }
}
