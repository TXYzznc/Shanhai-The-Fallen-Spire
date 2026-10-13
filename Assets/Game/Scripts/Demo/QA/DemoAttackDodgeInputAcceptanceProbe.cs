using System.Collections;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoAttackDodgeInputAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoDamageReceiver feedbackEnemy;
        public float FirstDamage { get; private set; }
        public float SecondDamage { get; private set; }
        public float ThirdDamage { get; private set; }
        public bool WindupObserved { get; private set; }
        public bool MovementDuringAttack { get; private set; }
        public bool DodgeCannotCancel { get; private set; }
        public bool FanDirectionValid { get; private set; }
        public bool TwoStepCycle { get; private set; }
        public bool ExpiredComboRestarts { get; private set; }
        public bool CustomDamageAndWindup { get; private set; }
        public bool CustomRecovery { get; private set; }
        public bool DodgeDistanceAndInvulnerability { get; private set; }
        public bool DodgeCooldown { get; private set; }
        public bool DodgeInvulnerabilityExpires { get; private set; }
        public bool DodgeStopsAtWall { get; private set; }
        public bool DefaultDodgeMatchesConfig { get; private set; }
        public bool DefaultDodgeCooldown { get; private set; }
        public bool HitFlashObserved { get; private set; }
        public bool HitFlashRecovers { get; private set; }
        public bool Completed { get; private set; }
        private DemoInputState _input;
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private object _oldMoveInput, _oldCombatInput;
        private DemoGameplayConfig _oldMoveConfig, _oldCombatConfig;
        private float _oldCaptureDelta;
        private bool _captured;
        public DemoInputState ReadState() => _input;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        private void OnDisable()
        {
            if (!_captured) return;
            if (_movement != null) { Field(_movement,"_inputSource").SetValue(_movement,_oldMoveInput);Field(_movement,"gameplayConfig").SetValue(_movement,_oldMoveConfig); }
            if (_combat != null) { Field(_combat,"_inputSource").SetValue(_combat,_oldCombatInput);Field(_combat,"gameplayConfig").SetValue(_combat,_oldCombatConfig); }
            Time.captureDeltaTime = _oldCaptureDelta;
        }
        private void Input(Vector2 move = default, bool attack = false, bool dodge = false) => _input = new DemoInputState(move,Vector2.zero,attack,dodge,false,false,Vector2.up);
        private static DemoDamageReceiver Target(string name, Vector2 position)
        {
            var root=new GameObject(name);root.transform.position=position;
            root.AddComponent<BoxCollider2D>().isTrigger=true;
            var receiver=root.AddComponent<DemoDamageReceiver>();receiver.ConfigureHealth(500f,0f);return receiver;
        }
        private IEnumerator Start()
        {
            yield return null;
            _movement=GetComponent<DemoPlayerController>();_combat=GetComponent<DemoPlayerCombatController>();
            _oldMoveInput=Field(_movement,"_inputSource").GetValue(_movement);_oldCombatInput=Field(_combat,"_inputSource").GetValue(_combat);
            _oldMoveConfig=(DemoGameplayConfig)Field(_movement,"gameplayConfig").GetValue(_movement);_oldCombatConfig=(DemoGameplayConfig)Field(_combat,"gameplayConfig").GetValue(_combat);
            _oldCaptureDelta=Time.captureDeltaTime;_captured=true;Time.captureDeltaTime=0.02f;
            Field(_movement,"_inputSource").SetValue(_movement,this);Field(_combat,"_inputSource").SetValue(_combat,this);
            GetComponent<DemoMarkLoadout>().ResetForRetry();GetComponent<DemoBuffLoadout>().ResetForRetry();
            GetComponent<DemoPlayerHealthController>().ResetForRetry();_combat.ResetForRetry();_movement.ResetForRetry();
            var body=GetComponent<Rigidbody2D>();body.position=Vector2.zero;body.rotation=0f;
            var front=Target("QA_AttackFront",Vector2.up);var back=Target("QA_AttackBack",Vector2.down);var side=Target("QA_AttackSide",Vector2.right);
            Physics2D.SyncTransforms();
            Input(attack:true);yield return null;Input(dodge:true);
            WindupObserved=front.CurrentHealth==500f && _combat.IsAttackLocked;
            Vector2 start=body.position;yield return null;Input();
            DodgeCannotCancel=Vector2.Distance(body.position,start)<0.001f && !_movement.IsInvulnerable;
            Input(Vector2.right);yield return null;yield return null;
            MovementDuringAttack=body.position.x>0.05f && _combat.IsAttackLocked;
            Input();front.transform.position=body.position+Vector2.up;Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.2f);FirstDamage=500f-front.CurrentHealth;
            FanDirectionValid=back.CurrentHealth==500f && side.CurrentHealth==500f;
            yield return new WaitForSeconds(0.15f);
            float before=front.CurrentHealth;Input(attack:true);yield return null;Input();yield return new WaitForSeconds(0.4f);SecondDamage=before-front.CurrentHealth;
            before=front.CurrentHealth;Input(attack:true);yield return null;Input();yield return new WaitForSeconds(0.4f);ThirdDamage=before-front.CurrentHealth;
            TwoStepCycle=Mathf.Abs(FirstDamage-10f)<0.001f && Mathf.Abs(SecondDamage-13f)<0.001f && Mathf.Abs(ThirdDamage-10f)<0.001f;
            yield return new WaitForSeconds(0.9f);before=front.CurrentHealth;Input(attack:true);yield return null;Input();yield return new WaitForSeconds(0.4f);
            ExpiredComboRestarts=Mathf.Abs(before-front.CurrentHealth-10f)<0.001f;
            start=body.position;Input(dodge:true);yield return null;Input();
            DefaultDodgeMatchesConfig=Mathf.Abs(Vector2.Distance(body.position,start)-1f)<0.02f && _movement.IsInvulnerable;
            start=body.position;yield return new WaitForSeconds(1f);Input(dodge:true);yield return null;Input();
            DefaultDodgeCooldown=Vector2.Distance(body.position,start)<0.001f;
            yield return new WaitForSeconds(0.25f);Input(dodge:true);yield return null;Input();
            DefaultDodgeCooldown &= Mathf.Abs(Vector2.Distance(body.position,start)-1f)<0.02f;
            front.transform.position=body.position+Vector2.up;Physics2D.SyncTransforms();
            var config=Instantiate(_oldCombatConfig);config.Player.baseAttackDamage=7f;config.Player.attackWindup=0.2f;config.Player.attackRecovery=0.25f;config.Player.attackCooldown=0.5f;
            Field(_combat,"gameplayConfig").SetValue(_combat,config);_combat.ResetForRetry();
            before=front.CurrentHealth;Input(attack:true);yield return null;Input();for(int i=0;i<6;i++)yield return null;
            CustomDamageAndWindup=front.CurrentHealth==before;
            yield return new WaitForSeconds(0.12f);CustomDamageAndWindup &= Mathf.Abs(before-front.CurrentHealth-7f)<0.001f;
            yield return new WaitForSeconds(0.1f);CustomRecovery=_combat.IsAttackLocked;
            yield return new WaitForSeconds(0.16f);CustomRecovery &= !_combat.IsAttackLocked;
            Destroy(front.gameObject);Destroy(back.gameObject);Destroy(side.gameObject);
            config.Player.dodgeDistance=1.2f;config.Player.dodgeCooldown=0.6f;config.Player.dodgeInvulnerability=0.1f;
            Field(_movement,"gameplayConfig").SetValue(_movement,config);_movement.ResetForRetry();body.position=Vector2.zero;Physics2D.SyncTransforms();
            Input(dodge:true);yield return null;Input();
            DodgeDistanceAndInvulnerability=Mathf.Abs(body.position.y-1.2f)<0.02f && _movement.IsInvulnerable;
            start=body.position;Input(dodge:true);yield return null;Input();DodgeCooldown=Vector2.Distance(body.position,start)<0.001f;
            yield return new WaitForSeconds(0.12f);DodgeInvulnerabilityExpires=!_movement.IsInvulnerable;
            yield return new WaitForSeconds(0.6f);body.position=Vector2.zero;Physics2D.SyncTransforms();
            var wall=new GameObject("QA_DodgeWall");wall.transform.position=new Vector3(0f,0.8f,0f);wall.AddComponent<BoxCollider2D>().size=new Vector2(8f,0.1f);Physics2D.SyncTransforms();
            Input(dodge:true);yield return null;Input();DodgeStopsAtWall=body.position.y>0f && body.position.y<0.7f;
            Destroy(wall);
            feedbackEnemy.gameObject.SetActive(true);
            feedbackEnemy.GetComponent<Shanhai.Demo.Enemies.DemoEnemyController>().enabled=false;
            feedbackEnemy.ResetHealth();
            var sprite=feedbackEnemy.GetComponent<SpriteRenderer>();Color baseColor=sprite.color;
            feedbackEnemy.ReceiveDamage(new DemoDamageInfo(1f,gameObject));yield return null;
            HitFlashObserved=sprite.sprite!=null && sprite.enabled && sprite.color==Color.white;
            yield return new WaitForSeconds(0.2f);HitFlashRecovers=sprite.color==baseColor;
            feedbackEnemy.ResetHealth();
            Field(_movement,"gameplayConfig").SetValue(_movement,_oldMoveConfig);Field(_combat,"gameplayConfig").SetValue(_combat,_oldCombatConfig);Destroy(config);
            Field(_movement,"_inputSource").SetValue(_movement,_oldMoveInput);Field(_combat,"_inputSource").SetValue(_combat,_oldCombatInput);Time.captureDeltaTime=_oldCaptureDelta;
            Completed=WindupObserved && MovementDuringAttack && DodgeCannotCancel && FanDirectionValid && TwoStepCycle && ExpiredComboRestarts
                && CustomDamageAndWindup && CustomRecovery && DodgeDistanceAndInvulnerability && DodgeCooldown && DodgeInvulnerabilityExpires && DodgeStopsAtWall
                && DefaultDodgeMatchesConfig && DefaultDodgeCooldown && HitFlashObserved && HitFlashRecovers;
        }
    }
}
