using System;
using GameFramework;
using UnityEngine;

namespace Shanhai.Demo.Combat
{
    /// <summary>同步通知的借用快照；监听者不得保留引用。发布期间不可变，回收后清空。</summary>
    public sealed class DemoCombatPresentationEvent : IReference
    {
        public float Amount { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector2 Forward { get; private set; }
        public float Radius { get; private set; }
        public float FanAngle { get; private set; }
        public DemoPlayerAction Action { get; private set; }
        public bool IsPlayer { get; private set; }

        internal static void Damage(Action<DemoCombatPresentationEvent> listeners, float amount, Vector3 position, bool player)
        {
            if (listeners == null || amount <= 0f) return;
            var args = ReferencePool.Acquire<DemoCombatPresentationEvent>();
            args.Amount = amount; args.Position = position; args.IsPlayer = player;
            try { listeners(args); } finally { ReferencePool.Release(args); }
        }

        internal static void Attack(Action<DemoCombatPresentationEvent> listeners, DemoPlayerAction action, Vector3 position, Vector2 forward, float radius, float fanAngle, bool player = true)
        {
            if (listeners == null) return;
            var args = ReferencePool.Acquire<DemoCombatPresentationEvent>();
            args.IsPlayer = player; args.Action = action; args.Position = position; args.Forward = forward; args.Radius = radius; args.FanAngle = fanAngle;
            try { listeners(args); } finally { ReferencePool.Release(args); }
        }

        public void Clear()
        {
            Amount = Radius = FanAngle = 0f; Position = Vector3.zero; Forward = Vector2.zero;
            Action = DemoPlayerAction.None; IsPlayer = false;
        }
    }
}
