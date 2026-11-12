using UnityEngine;

namespace Cove
{
    public class Boat : MonoBehaviour
    {
        public Transform hull;
        public Interactable interact;
        public float Yaw;
        public float Speed = 7f;
        public bool Occupied;

        private float bobT;
        private float rollT;
        private float currentCooldown;
        private float blockCooldown;

        private void Start()
        {
            Yaw = transform.eulerAngles.y;
        }

        public Vector3 SeatPosition
        {
            get { return transform.position + Vector3.up * 0.62f + transform.forward * 0.35f; }
        }

        private void Update()
        {
            bobT += Time.deltaTime;
            rollT += Time.deltaTime;

            float bob = Mathf.Sin(bobT * 1.7f) * 0.045f + Mathf.Sin(bobT * 0.9f + 1f) * 0.02f;
            Vector3 p = transform.position;
            p.y = 0.32f + bob;
            transform.position = p;
            if (hull != null)
            {
                hull.localRotation = Quaternion.Euler(Mathf.Sin(rollT * 0.8f) * 1.4f, 0f, Mathf.Sin(rollT * 1.1f + 0.5f) * 2.2f);
            }

            var game = GameBoot.I;
            if (game == null) return;
            if (interact != null) interact.prompt = Occupied ? "E   Leave the rowboat" : "E   Board the rowboat";
            if (!Occupied || !game.Playing || game.UI == null || game.UI.AnyPanelOpen) return;

            float throttle = 0f;
            float turn = 0f;
            if (game.Player != null && game.Player.ExternalInput)
            {
                throttle = game.Player.ExternalMove.y;
                turn = game.Player.ExternalMove.x;
            }
            else
            {
                throttle = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
                turn = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            }

            if (Mathf.Abs(turn) > 0.01f)
            {
                float delta = turn * 55f * Time.deltaTime;
                Yaw += delta;
                if (game.Player != null) game.Player.Yaw += delta;
            }
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);

            float move = throttle >= 0f ? throttle * Speed : throttle * 2.6f;
            Vector3 next = transform.position + transform.forward * move * Time.deltaTime;
            next.y = transform.position.y;

            if (game.World != null && game.World.Islands != null)
            {
                for (int i = 0; i < game.World.Islands.Count; i++)
                {
                    var isl = game.World.Islands[i];
                    Vector2 d = new Vector2(next.x - isl.center.x, next.z - isl.center.z);
                    if (d.magnitude < isl.radius * 0.98f)
                    {
                        Vector2 away = d.sqrMagnitude < 0.01f ? new Vector2(0f, 1f) : d.normalized;
                        next = new Vector3(isl.center.x + away.x * isl.radius * 0.98f, next.y, isl.center.z + away.y * isl.radius * 0.98f);
                        blockCooldown -= Time.deltaTime;
                        if (blockCooldown <= 0f)
                        {
                            blockCooldown = 3f;
                            game.UI.Toast("The shore is too shallow for the boat here.");
                        }
                        break;
                    }
                }
            }

            float limit = game.State.island2 ? (game.State.island3 ? 260f : 99f) : 46f;
            Vector2 fromHome = new Vector2(next.x, next.z);
            if (fromHome.magnitude > limit)
            {
                fromHome = fromHome.normalized * limit;
                next = new Vector3(fromHome.x, next.y, fromHome.y);
                currentCooldown -= Time.deltaTime;
                if (currentCooldown <= 0f)
                {
                    currentCooldown = 4f;
                    if (!game.State.island2) game.UI.Toast("A strong current blocks the way. Earn $100 to unlock new waters.");
                    else game.UI.Toast("The far current pushes back. Earn $500 to unlock the last island.");
                }
            }

            transform.position = next;
            if (game.Player != null) game.Player.transform.position = SeatPosition;
        }
    }
}
