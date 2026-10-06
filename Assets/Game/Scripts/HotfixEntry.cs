using System;
using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

/// <summary>
/// Entry point invoked by the built-in HybridCLR loading procedure.
/// AppConfigs provides the enabled framework and project procedures.
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
public static class HotfixEntry
{
    // Framework callback adapter: exceptions are handled locally and reported to the framework log.
    public static async void StartHotfixLogic(bool enableHotfix)
    {
        try
        {
            AwaitExtension.SubscribeEvent();
            GF.Initialize();
            var appConfig = await AppConfigs.GetInstanceSync();
            if (appConfig == null)
            {
                throw new InvalidOperationException("Core/AppConfigs could not be loaded.");
            }

            string[] names = appConfig.Procedures ?? Array.Empty<string>();
            var procedures = new ProcedureBase[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                Type type = FindProcedureType(names[i]);
                if (type == null || !typeof(ProcedureBase).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"Invalid procedure configured in AppConfigs: '{names[i]}'.");
                }
                procedures[i] = (ProcedureBase)Activator.CreateInstance(type);
            }

            GFBuiltin.Fsm.DestroyFsm<IProcedureManager>();
            var fsmManager = GameFrameworkEntry.GetModule<IFsmManager>();
            var procedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            procedureManager.Initialize(fsmManager, procedures);
            procedureManager.StartProcedure<PreloadProcedure>();
        }
        catch (Exception exception)
        {
            Log.Error("GF_X framework startup failed: {0}", exception);
        }
    }

    private static Type FindProcedureType(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        Type type = Type.GetType(name);
        if (type != null) return type;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(name);
            if (type != null) return type;
        }
        return null;
    }
}
