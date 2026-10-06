using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace AiFriendlyFrame.Sample.CircuitPuzzle
{
    /// <summary>
    /// Optional package entry point. The framework invokes it only while the package has registered
    /// this type through AppConfigs, after common data, configuration and dictionaries are ready.
    /// </summary>
    public sealed class CircuitPuzzleSampleProcedure : ProcedureBase, IFrameworkStartupProcedure
    {
        private const string SceneAssetName = "Assets/Sample/CircuitPuzzle/Scenes/CircuitPuzzle.unity";

        private bool _eventsSubscribed;

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            _eventsSubscribed = true;

            GFTrace.Info("Sample", "CircuitPuzzle.LoadScene", null, GFTrace.Data("asset", SceneAssetName));
            GF.Scene.LoadScene(SceneAssetName, this);
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            if (_eventsSubscribed)
            {
                _eventsSubscribed = false;
                GF.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
                GF.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            }

            base.OnLeave(procedureOwner, isShutdown);
        }

        private void OnLoadSceneSuccess(object sender, GameEventArgs eventArgs)
        {
            var args = (LoadSceneSuccessEventArgs)eventArgs;
            if (args.UserData != this)
            {
                return;
            }

            GFTrace.Success("Sample", "CircuitPuzzle.Ready", null, GFTrace.Data("asset", args.SceneAssetName));
        }

        private void OnLoadSceneFailure(object sender, GameEventArgs eventArgs)
        {
            var args = (LoadSceneFailureEventArgs)eventArgs;
            if (args.UserData != this)
            {
                return;
            }

            GFTrace.Failure("Sample", "CircuitPuzzle.LoadSceneFailed", args.ErrorMessage,
                GFTrace.Data("asset", args.SceneAssetName));
            Log.Error("Circuit Puzzle sample scene could not be loaded: {0}", args.ErrorMessage);
        }
    }
}
