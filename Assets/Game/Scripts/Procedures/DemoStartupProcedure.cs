using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

/// <summary>
/// Demo 启动入口：框架预加载完成后进入开始界面。
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public sealed class DemoStartupProcedure : ProcedureBase, IFrameworkStartupProcedure
{
    private const string DemoSceneName = "Demo/DemoMenu";

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        if (!UnityEngine.Debug.isDebugBuild && GFBuiltin.Debugger != null)
            GFBuiltin.Debugger.ActiveWindow = false;
        GFTrace.Info("Procedure", "DemoStartup.Enter", null, GFTrace.Data("scene", DemoSceneName));
        procedureOwner.SetData("SceneName", (VarString)DemoSceneName);
        ChangeState<ChangeSceneProcedure>(procedureOwner);
    }
}
