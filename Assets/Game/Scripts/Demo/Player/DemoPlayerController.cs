using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Build;
using UnityEngine;
using UnityEngine.Serialization;

namespace Shanhai.Demo.Player
{
    /// <summary>2D 玩家移动、瞄准和边界约束；只消费语义化输入。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DemoPlayerController : MonoBehaviour
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [FormerlySerializedAs("inputSource")]
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoPlayerCombatController attackLockBehaviour;
        [SerializeField] private DemoMarkLoadout markLoadout;
        [SerializeField] private DemoBuffLoadout buffLoadout;
        [SerializeField] private DemoPlayerHealthController healthController;
        [SerializeField] private float fallbackMoveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 1_440f;
        [SerializeField] private float minX = -18f;
        [SerializeField] private float maxX = 18f;
        [FormerlySerializedAs("minZ")]
        [SerializeField] private float minY = -4f;
        [FormerlySerializedAs("maxZ")]
        [SerializeField] private float maxY = 56f;
        [SerializeField] private LayerMask movementCollisionMask = ~0;
        [SerializeField, Min(0f)] private float movementCollisionSkin = 0.02f;

        private Rigidbody2D _body;
        private IDemoInputSource _inputSource;
        private float _nextDodgeAt;
        private float _invulnerableUntil;
        private int _lastDodgeFrame = -1;
        private Vector2 _dodgeDirection;
        private float _dodgeRemaining;
        private float _dodgeSpeed;
        private readonly RaycastHit2D[] _movementHits = new RaycastHit2D[16];
        private ContactFilter2D _movementFilter;

        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public bool DodgedThisFrame => _lastDodgeFrame == Time.frameCount;
        public bool IsDodging => _dodgeRemaining > 0f;

        public void ResetForRetry()
        {
            _nextDodgeAt = 0f;
            _invulnerableUntil = 0f;
            _lastDodgeFrame = -1;
            _dodgeRemaining = 0f;
            if (_body != null)
            {
                _body.velocity = Vector2.zero;
                _body.angularVelocity = 0f;
            }
            transform.rotation = Quaternion.identity;
        }

        public void BindInput(MonoBehaviour input, Camera camera)
        {
            inputSourceBehaviour = input; _inputSource = input as IDemoInputSource; aimCamera = camera;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _inputSource = inputSourceBehaviour as IDemoInputSource;
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _movementFilter.SetLayerMask(movementCollisionMask);
            _movementFilter.useTriggers = false;
        }

        private void Update()
        {
            if (GameplayBlocked) return;
            if (_inputSource == null) return;

            DemoInputState input = _inputSource.ReadState();
            if (healthController != null && (healthController.IsHitStunned || healthController.IsRetryRequired))
            {
                _dodgeRemaining = 0f;
                return;
            }
            TryDodge(input);
            if (IsDodging)
            {
                float step = Mathf.Min(Time.deltaTime, _dodgeRemaining);
                float expectedDistance = _dodgeSpeed * step;
                float movedDistance = MoveWithCollision(_dodgeDirection * expectedDistance);
                _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - step);
                if (movedDistance + 0.0001f < expectedDistance) _dodgeRemaining = 0f;
                return;
            }
            float moveSpeed = gameplayConfig != null && gameplayConfig.Player != null
                ? gameplayConfig.Player.moveSpeed : fallbackMoveSpeed;
            if (markLoadout != null) moveSpeed *= markLoadout.AttributeMoveSpeedMultiplier * markLoadout.TemporaryMoveSpeedMultiplier;
            if (buffLoadout != null) moveSpeed *= buffLoadout.MoveSpeedMultiplier;
            Vector2 target = _body.position + input.Move * (moveSpeed * Time.deltaTime);
            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
            MoveWithCollision(target - _body.position);

            if (input.AimDirection.sqrMagnitude > 0.001f) RotateTowards(input.AimDirection);
            else RotateTowardsAim(input.AimScreenPosition);
        }

        public bool TryDodge(DemoInputState input)
        {
            if (GameplayBlocked) return false;
            if (healthController != null && (healthController.IsHitStunned || healthController.IsRetryRequired)) return false;
            if (!input.DodgePressed || IsDodging || DodgedThisFrame || Time.time < _nextDodgeAt) return false;

            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float cooldown = player != null ? player.dodgeCooldown : 1.2f;
            float distance = player != null ? player.dodgeDistance : 2f;
            float duration = Mathf.Max(0.01f, player != null ? player.dodgeDuration : 0.2f);
            float invulnerability = player != null ? player.dodgeInvulnerability : 0.2f;
            Vector2 direction = input.Move.sqrMagnitude > 0.0001f ? input.Move.normalized : (Vector2)transform.up;
            _lastDodgeFrame = Time.frameCount;
            if (attackLockBehaviour != null) attackLockBehaviour.CancelForDodge();
            _dodgeDirection = direction;
            _dodgeRemaining = duration;
            _dodgeSpeed = Mathf.Max(0f, distance) / duration;
            _nextDodgeAt = Time.time + cooldown;
            _invulnerableUntil = Time.time + invulnerability;
            return true;
        }

        private void OnDisable() => _dodgeRemaining = 0f;

        private float MoveWithCollision(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.000001f) return 0f;
            Vector2 before = _body.position;
            float distance = delta.magnitude;
            Vector2 direction = delta / distance;
            int hitCount = _body.Cast(direction, _movementFilter, _movementHits, distance + movementCollisionSkin);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _movementHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - movementCollisionSkin));
            }
            Vector2 target = _body.position + direction * distance;
            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
            _body.position = target;
            return Vector2.Distance(before, target);
        }

        private void RotateTowards(Vector2 direction)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, targetAngle), rotationSpeed * Time.deltaTime);
        }

        private void RotateTowardsAim(Vector2 screenPosition)
        {
            if (aimCamera == null) return;
            Vector3 world = aimCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -aimCamera.transform.position.z));
            Vector2 direction = new Vector2(world.x - transform.position.x, world.y - transform.position.y);
            if (direction.sqrMagnitude > 0.0001f) RotateTowards(direction);
        }
    }
}
