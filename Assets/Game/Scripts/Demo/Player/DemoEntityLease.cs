using System;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.Player
{
    /// <summary>单次实体请求的所有权；加载取消后GF不会执行OnShow，参数由票据释放。</summary>
    public sealed class DemoEntityLease : IDisposable
    {
        private EntityParams _pending;
        private Action<GameObject> _shown;
        private Action<string> _failed;
        private Action<DemoEntityLease> _released;
        private bool _subscribed;
        public int Id { get; } = UtilityBuiltin.GenerateEntityId();
        public bool IsDisposed { get; private set; }
        public GameObject Instance { get; private set; }
        internal DemoEntityLease(Action<GameObject> shown, Action<string> failed, Action<DemoEntityLease> released)
        { _shown = shown; _failed = failed; _released = released; }
        internal void Begin(string asset, Vector3 position, Vector3 euler, Vector3 scale)
        {
            _pending = EntityParams.Create(position, euler, scale);
            _pending.OnShowCallback = Shown;
            GF.Event.Subscribe(ShowEntityFailureEventArgs.EventId, Failed); _subscribed = true;
            try { GF.Entity.ShowEntity(Id, typeof(DemoSessionEntity), asset, "Default", 0, _pending); }
            catch (Exception error) { Action<string> failed = _failed; Dispose(); failed?.Invoke(error.Message); }
        }
        private void Shown(EntityLogic logic)
        {
            _pending = null; Detach(); // 此后EntityBase.OnHide持有和释放参数。
            Instance = logic.gameObject;
            try { _shown?.Invoke(Instance); }
            catch (Exception error) { Action<string> failed = _failed; Dispose(); failed?.Invoke(error.Message); Debug.LogException(error); }
        }
        private void Failed(object sender, GameEventArgs e)
        {
            var args = (ShowEntityFailureEventArgs)e;
            if (args.EntityId != Id || IsDisposed) return;
            Action<string> failed = _failed; string message = args.ErrorMessage;
            Dispose(); failed?.Invoke(message);
        }
        private void Detach()
        {
            if (_subscribed && GF.Event != null) GF.Event.Unsubscribe(ShowEntityFailureEventArgs.EventId, Failed);
            _subscribed = false;
        }
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true; Detach();
            if (GF.Entity != null && (GF.Entity.IsLoadingEntity(Id) || GF.Entity.HasEntity(Id))) GF.Entity.HideEntity(Id);
            if (_pending != null) { ReferencePool.Release(_pending); _pending = null; }
            Instance = null; _shown = null; _failed = null;
            Action<DemoEntityLease> released = _released; _released = null; released?.Invoke(this);
        }
    }
}
