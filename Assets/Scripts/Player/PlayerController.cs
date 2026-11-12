using UnityEngine;

namespace Cove
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Camera Cam;
        public Transform CameraPivot;
        public float WalkSpeed = 3.6f;
        public float SprintSpeed = 6.4f;
        public float SwimSpeed = 2.0f;
        public float Acceleration = 16f;
        public float AirControl = 0.4f;
        public float Gravity = 20f;
        public float JumpVelocity = 6.6f;
        public float MouseSensitivity = 2.2f;
        public float WaterLevel = 0.15f;

        public float Yaw;
        public float Pitch;
        public bool InBoat;
        public bool inputEnabled = true;

        public bool ExternalInput;
        public Vector2 ExternalMove;
        public bool ExternalSprint;
        public bool ExternalJump;

        public CharacterController Controller { get; private set; }
        public bool Swimming { get; private set; }
        public bool Grounded { get; private set; }
        public bool IsMoving { get; private set; }

        private Vector3 horizontalVel;
        private float verticalVel;
        private float stepTimer;
        private float bobPhase;
        private float splashTimer;
        private bool wasSwimming;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Controller.height = 1.8f;
            Controller.radius = 0.36f;
            Controller.center = new Vector3(0f, 0.9f, 0f);
            Controller.slopeLimit = 52f;
            Controller.stepOffset = 0.4f;
            Controller.skinWidth = 0.03f;
            Controller.minMoveDistance = 0f;
            Yaw = transform.eulerAngles.y;
            Pitch = 0f;
        }

        public void ForceLook(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
            if (CameraPivot != null) CameraPivot.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
        }

        public void Teleport(Vector3 pos, float? yaw = null)
        {
            Controller.enabled = false;
            transform.position = pos;
            Controller.enabled = true;
            verticalVel = 0f;
            horizontalVel = Vector3.zero;
            if (yaw.HasValue) ForceLook(yaw.Value, Pitch);
        }

        private void Update()
        {
            if (Cam == null) return;
            if (!inputEnabled) return;

            bool blocked = GameBoot.I != null && GameBoot.I.UI != null && GameBoot.I.UI.AnyPanelOpen;
            if (!blocked && Cursor.lockState == CursorLockMode.Locked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * MouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
                Yaw += mx;
                Pitch = Mathf.Clamp(Pitch - my, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
            if (CameraPivot != null) CameraPivot.localRotation = Quaternion.Euler(Pitch, 0f, 0f);

            if (InBoat)
            {
                horizontalVel = Vector3.zero;
                verticalVel = 0f;
                IsMoving = false;
                return;
            }

            Vector2 move = ExternalInput ? ExternalMove : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool sprint = ExternalInput ? ExternalSprint : Input.GetKey(KeyCode.LeftShift);
            bool jump = ExternalInput ? ExternalJump : Input.GetKeyDown(KeyCode.Space);
            if (blocked)
            {
                move = Vector2.zero;
                sprint = false;
                jump = false;
            }
            if (move.sqrMagnitude > 1f) move.Normalize();

            Grounded = Controller.isGrounded;
            Swimming = !Grounded && transform.position.y < WaterLevel + 0.55f;

            if (Swimming && !wasSwimming)
            {
                if (GameBoot.I != null && GameBoot.I.Audio != null) GameBoot.I.Audio.Play(GameBoot.I.Audio.Splash, 0.5f, 1f);
            }
            wasSwimming = Swimming;

            float speed = Swimming ? SwimSpeed : (sprint && move.y > 0f ? SprintSpeed : WalkSpeed);
            Vector3 desired = (transform.right * move.x + transform.forward * move.y) * speed;
            float accel = Grounded || Swimming ? Acceleration : Acceleration * AirControl;
            horizontalVel = Vector3.MoveTowards(horizontalVel, desired, accel * Time.deltaTime);
            IsMoving = horizontalVel.magnitude > 0.4f;

            if (Swimming)
            {
                float surface = WaterLevel - 0.18f;
                float buoy = (surface - transform.position.y) * 22f;
                verticalVel = Mathf.Lerp(verticalVel, Mathf.Clamp(buoy, -3f, 3f), Time.deltaTime * 5f);
                if (jump) verticalVel = 2.2f;
            }
            else
            {
                verticalVel -= Gravity * Time.deltaTime;
                if (Grounded && verticalVel < 0f) verticalVel = -2.5f;
                if (Grounded && jump && !blocked)
                {
                    verticalVel = JumpVelocity;
                    GameBoot.I.Audio.Play(GameBoot.I.Audio.Splash, 0.18f, 1.6f);
                }
            }

            Vector3 motion = horizontalVel;
            motion.y = verticalVel;
            Controller.Move(motion * Time.deltaTime);

            if (transform.position.y < -4f)
            {
                Teleport(GameBoot.I.World.StartSpawn.position + Vector3.up * 0.5f);
            }

            if (Grounded && CameraPivot != null)
            {
                if (IsMoving)
                {
                    bobPhase += Time.deltaTime * (sprint ? 13f : 8.5f);
                    stepTimer -= Time.deltaTime;
                    if (stepTimer <= 0f)
                    {
                        stepTimer = sprint ? 0.31f : 0.46f;
                        GameBoot.I.Audio.PlayFootstep(false);
                    }
                }
                else
                {
                    stepTimer = 0f;
                }
                float bob = IsMoving ? Mathf.Sin(bobPhase) * (sprint ? 0.055f : 0.035f) : 0f;
                Vector3 lp = CameraPivot.localPosition;
                lp.y = Mathf.Lerp(lp.y, 1.62f + bob, Time.deltaTime * 10f);
                CameraPivot.localPosition = lp;
            }
            else if (Swimming)
            {
                splashTimer -= Time.deltaTime;
                if (IsMoving && splashTimer <= 0f)
                {
                    splashTimer = 0.85f;
                    GameBoot.I.Audio.PlayFootstep(true);
                }
            }
        }
    }

    public class PlayerInteraction : MonoBehaviour
    {
        public PlayerController player;
        public float Range = 3.6f;
        private Interactable current;

        private void Update()
        {
            var game = GameBoot.I;
            if (game == null || game.UI == null) return;
            if (!game.Playing || player == null || !player.inputEnabled) return;

            bool fishingBusy = game.Fishing != null && game.Fishing.State != FishingState.Idle;
            if (player.InBoat)
            {
                if (!fishingBusy) game.UI.SetPrompt("E   Leave the boat");
                if (!game.UI.AnyPanelOpen && Input.GetKeyDown(KeyCode.E)) game.ToggleBoat();
                current = null;
                return;
            }

            Interactable found = null;
            if (player.Cam != null)
            {
                Ray ray = new Ray(player.Cam.transform.position, player.Cam.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Range, ~0, QueryTriggerInteraction.Collide))
                {
                    found = hit.collider.GetComponentInParent<Interactable>();
                }
            }

            current = found;
            if (fishingBusy)
            {
                if (found != null && !game.UI.AnyPanelOpen && Input.GetKeyDown(KeyCode.E)) found.Interact();
                return;
            }
            if (game.UI.AnyPanelOpen) game.UI.SetPrompt("");
            else game.UI.SetPrompt(found != null ? found.prompt : "");

            if (found != null && !game.UI.AnyPanelOpen && Input.GetKeyDown(KeyCode.E))
            {
                found.Interact();
            }
        }
    }
}
