using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.Presentation;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>克隆场景三类敌人，通过实际AI和受击事件读取状态及Sprite颜色；不代替正式资源验收。</summary>
    public sealed class DemoEnemyVisualAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private GameObject common;
        [SerializeField] private GameObject elite;
        [SerializeField] private GameObject boss;
        public int IdleCases { get; private set; }
        public int MovingCases { get; private set; }
        public int AttackCases { get; private set; }
        public int HitCases { get; private set; }
        public int StaggerCases { get; private set; }
        public int DeathCases { get; private set; }
        public int ResetCases { get; private set; }
        public bool EliteChargeAndDashVisible { get; private set; }
        public string EliteDetails { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private GameObject _clone;
        private DemoEnemyController _ai;
        private DemoDamageReceiver _receiver;
        private DemoSpriteFeedback _feedback;
        private SpriteRenderer _sprite;
        private Rigidbody2D _body;
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoRunFlowController _flow;
        private DemoPlayerHealthController _health;
        private bool _movementEnabled, _combatEnabled, _flowEnabled, _captured;
        private float _captureDelta;
        private Vector2 _start;
        private Color _baseColor;
        private IEnumerator Start()
        {
            yield return null;
            _body = GetComponent<Rigidbody2D>(); _start = _body.position;
            _movement = GetComponent<DemoPlayerController>(); _combat = GetComponent<DemoPlayerCombatController>();
            _flow = GetComponent<DemoRunFlowController>(); _health = GetComponent<DemoPlayerHealthController>();
            _movementEnabled = _movement.enabled; _combatEnabled = _combat.enabled; _flowEnabled = _flow.enabled;
            _captureDelta = Time.captureDeltaTime; _captured = true; Time.captureDeltaTime = 0.05f;
            _movement.enabled = _combat.enabled = _flow.enabled = false;
            foreach (var source in new[] { common, elite, boss })
            {
                _body.position = new Vector2(10f, 0f); _health.ResetForRetry();
                _clone = Instantiate(source); _clone.name = "QA_Visual_" + source.name;
                _clone.transform.position = new Vector3(10f, 6f); _clone.SetActive(true);
                _ai = _clone.GetComponent<DemoEnemyController>(); _receiver = _clone.GetComponent<DemoDamageReceiver>();
                _feedback = _clone.GetComponent<DemoSpriteFeedback>(); _sprite = _clone.GetComponent<SpriteRenderer>();
                _baseColor = _sprite.color;
                _ai.enabled = false;
                yield return new WaitForSeconds(0.3f);
                if (_feedback.CurrentState == DemoSpriteState.Idle && _sprite.sprite != null && Near(_sprite.color, _baseColor)) IdleCases++;
                _ai.enabled = true;
                yield return WaitState(DemoSpriteState.Moving, 2f);
                if (_feedback.CurrentState == DemoSpriteState.Moving && !Near(_sprite.color, _baseColor)) MovingCases++;
                _clone.GetComponent<Rigidbody2D>().position = new Vector2(10f, source == elite ? 3f : 1.2f); Physics2D.SyncTransforms();
                _receiver.ResetHealth(); _health.ResetForRetry();
                if (source == elite)
                {
                    yield return WaitState(DemoSpriteState.Charging, 2f);
                    bool charge = _feedback.CurrentState == DemoSpriteState.Charging && !Near(_sprite.color, _baseColor);
                    string start = _clone.transform.position.ToString();
                    yield return WaitState(DemoSpriteState.Dashing, 2f);
                    EliteChargeAndDashVisible = charge && _feedback.CurrentState == DemoSpriteState.Dashing && !Near(_sprite.color, _baseColor);
                    EliteDetails = $"windup={charge}, dashState={_feedback.CurrentState}, start={start}, end={_clone.transform.position}, attacks={_ai.AttackCount}";
                    if (EliteChargeAndDashVisible) AttackCases++;
                }
                else
                {
                    yield return WaitState(DemoSpriteState.Attacking, 2f);
                    if (_feedback.CurrentState == DemoSpriteState.Attacking && _ai.AttackCount > 0 && !Near(_sprite.color, _baseColor)) AttackCases++;
                }
                _ai.enabled = false;
                _receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
                yield return null;
                if (_feedback.CurrentState == DemoSpriteState.Hit && Near(_sprite.color, Color.white)) HitCases++;
                _receiver.ConfigureStagger(1f, 3f, 2f, 1f);
                _receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
                yield return null;
                if (_feedback.CurrentState == DemoSpriteState.Staggered && Near(_sprite.color, new Color(0.35f, 0.75f, 1f))) StaggerCases++;
                _receiver.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
                yield return null;
                if (_feedback.CurrentState == DemoSpriteState.Dead && Near(_sprite.color, Color.Lerp(_baseColor, Color.black, 0.45f))) DeathCases++;
                _receiver.ResetHealth(); _ai.ResetForRetry();
                yield return new WaitForSeconds(0.3f);
                if (_feedback.CurrentState == DemoSpriteState.Idle && Near(_sprite.color, _baseColor)) ResetCases++;
                Destroy(_clone); _clone = null; yield return null;
            }
            Completed = IdleCases == 3 && MovingCases == 3 && AttackCases == 3 && HitCases == 3 && StaggerCases == 3
                && DeathCases == 3 && ResetCases == 3 && EliteChargeAndDashVisible;
            Details = $"idle/move/attack/hit/stagger/death/reset={IdleCases}/{MovingCases}/{AttackCases}/{HitCases}/{StaggerCases}/{DeathCases}/{ResetCases}, eliteDash={EliteChargeAndDashVisible}";
            Finished = true; Restore();
        }
        private IEnumerator WaitState(DemoSpriteState state, float timeout) { float until = Time.time + timeout; while (_feedback.CurrentState != state && Time.time < until) yield return null; }
        private static bool Near(Color a, Color b) => Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a) < 0.01f;
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            if (_clone != null) Destroy(_clone);
            _body.position = _start; _health.ResetForRetry(); _movement.enabled = _movementEnabled;
            _combat.enabled = _combatEnabled; _flow.enabled = _flowEnabled; Time.captureDeltaTime = _captureDelta; _captured = false;
        }
    }
}
