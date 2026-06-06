using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Rigidbody-based 3D movement on the X/Z plane with facing = movement direction.
    /// Constant speed (no sprint in the MVP). Drives an Animator's speed parameter so the
    /// Idle/Walk blend works once a real character + Animator are wired in.
    /// Named ArenaPlayerController to avoid a name clash with the Input System sample's
    /// "PlayerController" type, which hides same-named components in the Add Component menu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputProvider))]
    public class ArenaPlayerController : MonoBehaviour
    {
        #region Public Fields
        [Header("Movement")]
        [Tooltip("Constant move speed in units/second.")]
        public float moveSpeed = 6f;

        [Tooltip("How quickly the character turns to face its movement direction.")]
        public float turnSpeed = 15f;

        [Header("Dash")]
        [Tooltip("Speed during a dash (units/second).")]
        public float dashSpeed = 16f;

        [Tooltip("How long a dash lasts (seconds).")]
        public float dashDuration = 0.18f;

        [Tooltip("Cooldown between dashes (seconds).")]
        public float dashCooldown = 1f;

        [Header("Animation (optional)")]
        [Tooltip("Animator to drive. If empty, the first Animator in children is used.")]
        public Animator animator;

        [Tooltip("Float parameter set to the current planar speed.")]
        public string speedParameter = "Speed";
        #endregion

        #region Private Fields
        private Rigidbody rb;
        private PlayerInputProvider input;
        private Vector2 moveInput;
        private int speedHash;
        private bool isDashing;
        private float dashEndTime;
        private float lastDashTime = -999f;
        private Vector3 dashDir = Vector3.forward;
        #endregion

        private void Awake()
        {
            this.rb = GetComponent<Rigidbody>();
            this.input = GetComponent<PlayerInputProvider>();

            // Lock vertical position (gameplay is on the X/Z plane) and freeze tilt so collisions
            // can't pop the body up or tip it over. Y rotation stays free for facing via MoveRotation;
            // collision-induced spin is killed by zeroing angular velocity each FixedUpdate.
            this.rb.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            if (this.animator == null) this.animator = GetComponentInChildren<Animator>();
            this.speedHash = Animator.StringToHash(this.speedParameter);
        }

        private void Update()
        {
            this.moveInput = this.input.Move != null ? this.input.Move.ReadValue<Vector2>() : Vector2.zero;
            this.TryStartDash();
            this.UpdateAnimator();
        }

        private void TryStartDash()
        {
            if (this.input.Dash == null || !this.input.Dash.WasPerformedThisFrame()) return;
            if (this.isDashing || Time.time - this.lastDashTime < this.dashCooldown) return;

            // Dash toward current input direction, or straight ahead (facing) when standing still.
            Vector3 d = new Vector3(this.moveInput.x, 0f, this.moveInput.y);
            this.dashDir = d.sqrMagnitude > 0.01f ? d.normalized : this.transform.forward;
            this.isDashing = true;
            this.dashEndTime = Time.time + this.dashDuration;
            this.lastDashTime = Time.time;
        }

        private void FixedUpdate()
        {
            // Dash overrides normal movement for its duration.
            if (this.isDashing)
            {
                if (Time.time >= this.dashEndTime)
                {
                    this.isDashing = false;
                }
                else
                {
                    this.rb.linearVelocity = this.dashDir * this.dashSpeed;
                    this.rb.angularVelocity = Vector3.zero;
                    this.rb.MoveRotation(Quaternion.LookRotation(this.dashDir, Vector3.up));
                    return;
                }
            }

            // 2D input -> X/Z plane (input.y maps to world Z).
            Vector3 dir = new Vector3(this.moveInput.x, 0f, this.moveInput.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // Horizontal velocity from input only; Y is locked by the constraint.
            this.rb.linearVelocity = dir * this.moveSpeed;

            // Kill any spin a collision may have imparted, so facing stays fully script-controlled.
            this.rb.angularVelocity = Vector3.zero;

            // Face the movement direction.
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                this.rb.MoveRotation(Quaternion.Slerp(this.rb.rotation, target, this.turnSpeed * Time.fixedDeltaTime));
            }
        }

        private void UpdateAnimator()
        {
            if (this.animator == null) return;
            Vector3 v = this.rb.linearVelocity;
            float planarSpeed = new Vector2(v.x, v.z).magnitude;
            this.animator.SetFloat(this.speedHash, planarSpeed);
        }
    }
}
