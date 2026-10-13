using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>通过输入抽象与真实2D碰撞步行验收通路；隔离AI，注入击败事件，不替代实战验收。</summary>
    public sealed class DemoMapWalkAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoEnemyController[] enemies;
        [SerializeField] private DemoEncounterController elite;
        [SerializeField] private DemoEncounterController boss;
        [SerializeField] private DemoDamageReceiver eliteReceiver;
        [SerializeField] private DemoDamageReceiver bossReceiver;
        [SerializeField] private GameObject eliteGate;
        [SerializeField] private GameObject bossGate;
        [SerializeField] private DemoCheckpointController checkpoint;
        public float FurthestY { get; private set; }
        public bool StartsAtOrigin { get; private set; }
        public bool BossEntryInitiallyOpen { get; private set; }
        public bool EliteReachable { get; private set; }
        public bool CheckpointReachedByWalking { get; private set; }
        public bool EliteGateBlocksUntilDefeat { get; private set; }
        public bool EliteDefeatOpensRoad { get; private set; }
        public bool BossRoomReached { get; private set; }
        public bool BossSealsBehindPlayer { get; private set; }
        public bool BossDefeatOpensExit { get; private set; }
        public bool EndingReached { get; private set; }
        public bool ReturnedByWalking { get; private set; }
        public bool RetryRestoresGatesAndEncounters { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        private DemoInputState _input;
        private DemoPlayerController _movement;
        private object _originalInput;
        private float _oldCaptureDelta;
        private bool _captured;
        private Rigidbody2D _body;
        public DemoInputState ReadState() => _input;
        private void OnDisable()
        {
            if (!_captured) return;
            if (_movement!=null) typeof(DemoPlayerController).GetField("_inputSource",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_movement,_originalInput);
            Time.captureDeltaTime=_oldCaptureDelta;
        }
        private void Move(Vector2 direction) => _input=new DemoInputState(direction,Vector2.zero,false,false,false,false,direction.sqrMagnitude>0f?direction:Vector2.up);
        private IEnumerator WalkTo(float y, float timeout)
        {
            float until=Time.time+timeout;
            while(Mathf.Abs(_body.position.y-y)>0.25f && Time.time<until)
            {
                Move(_body.position.y<y?Vector2.up:Vector2.down);
                FurthestY=Mathf.Max(FurthestY,_body.position.y);
                yield return null;
            }
            Move(Vector2.zero);
        }
        private IEnumerator Start()
        {
            yield return null;
            _movement=GetComponent<DemoPlayerController>();_body=GetComponent<Rigidbody2D>();
            _originalInput=typeof(DemoPlayerController).GetField("_inputSource",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_movement);
            _oldCaptureDelta=Time.captureDeltaTime;_captured=true;Time.captureDeltaTime=0.05f;
            typeof(DemoPlayerController).GetField("_inputSource",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_movement,this);
            GetComponent<DemoPlayerCombatController>().enabled=false;
            for(int i=0;i<enemies.Length;i++) enemies[i].enabled=false;
            StartsAtOrigin=_body.position.sqrMagnitude<0.01f;
            BossEntryInitiallyOpen=!bossGate.activeSelf;
            yield return WalkTo(34.5f,9f);
            EliteReachable=_body.position.y>34f && elite.HasStarted;
            CheckpointReachedByWalking=checkpoint.IsActivated;
            if(!EliteReachable) {Finished=true;yield break;}
            yield return WalkAcross(3f,2f);
            yield return WalkTo(43f,3f);
            EliteGateBlocksUntilDefeat=eliteGate.activeSelf && _body.position.y>40f && _body.position.y<42f && !elite.IsCompleted;
            eliteReceiver.ReceiveDamage(new DemoDamageInfo(10000f,gameObject));
            eliteReceiver.GetComponent<DemoEnemyController>().enabled=true;
            yield return null;yield return null;
            EliteDefeatOpensRoad=elite.IsCompleted && !eliteGate.activeSelf;
            yield return WalkAcross(0f,2f);
            yield return WalkTo(46.5f,3f);
            BossRoomReached=_body.position.y>45.5f && boss.HasStarted;
            BossSealsBehindPlayer=bossGate.activeSelf && _body.position.y>bossGate.transform.position.y+0.7f;
            Move(Vector2.down);float before=_body.position.y;float until=Time.time+1f;
            while(Time.time<until)yield return null;
            Move(Vector2.zero);
            BossSealsBehindPlayer &= _body.position.y>bossGate.transform.position.y && _body.position.y<before;
            bossReceiver.ReceiveDamage(new DemoDamageInfo(10000f,gameObject));
            bossReceiver.GetComponent<DemoEnemyController>().enabled=true;
            yield return null;yield return null;
            BossDefeatOpensExit=boss.IsCompleted && !bossGate.activeSelf;
            yield return WalkTo(52f,3f);EndingReached=_body.position.y>51.5f;
            yield return WalkTo(0f,12f);ReturnedByWalking=Mathf.Abs(_body.position.y)<0.3f;
            GetComponent<DemoRunFlowController>().RestartRun();
            yield return null;
            RetryRestoresGatesAndEncounters=eliteGate.activeSelf && !bossGate.activeSelf
                && !elite.HasStarted && !elite.IsCompleted && !boss.HasStarted && !boss.IsCompleted
                && eliteReceiver.CurrentHealth==eliteReceiver.MaxHealth && bossReceiver.CurrentHealth==bossReceiver.MaxHealth;
            Completed=StartsAtOrigin && BossEntryInitiallyOpen && EliteReachable && CheckpointReachedByWalking
                && EliteGateBlocksUntilDefeat && EliteDefeatOpensRoad && BossRoomReached && BossSealsBehindPlayer
                && BossDefeatOpensExit && EndingReached && ReturnedByWalking && RetryRestoresGatesAndEncounters;
            Finished=true;Time.captureDeltaTime=_oldCaptureDelta;
        }
        private IEnumerator WalkAcross(float x, float timeout)
        {
            float until=Time.time+timeout;
            while(Mathf.Abs(_body.position.x-x)>0.15f && Time.time<until)
            {
                Move(_body.position.x<x?Vector2.right:Vector2.left);
                yield return null;
            }
            Move(Vector2.zero);
        }
    }
}
