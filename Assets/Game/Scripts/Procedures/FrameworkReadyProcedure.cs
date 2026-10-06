using GameFramework.Fsm;
using System;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

/// <summary>
/// Framework terminal state after generic services and core data are initialized.
/// A concrete project transitions from here to its own startup procedure.
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public sealed class FrameworkReadyProcedure : ProcedureBase
{
    protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GFBuiltin.BuiltinView?.HideLoadingProgress();
        Log.Info("GF_X framework is ready. Add project procedures and content to begin a game.");

        try
        {
            AppConfigs appConfigs = await AppConfigs.GetInstanceSync();
            Type startupProcedure = FindStartupProcedure(appConfigs?.Procedures);
            if (startupProcedure != null)
            {
                Log.Info("GF_X framework is starting configured procedure '{0}'.", startupProcedure.FullName);
                ChangeState(procedureOwner, startupProcedure);
            }
        }
        catch (Exception exception)
        {
            Log.Error("GF_X framework failed to select a startup procedure: {0}", exception);
        }
    }

    private static Type FindStartupProcedure(string[] procedureNames)
    {
        if (procedureNames == null)
        {
            return null;
        }

        Type startupProcedure = null;
        foreach (string procedureName in procedureNames)
        {
            Type procedureType = FindProcedureType(procedureName);
            if (procedureType == null || !typeof(ProcedureBase).IsAssignableFrom(procedureType) ||
                !typeof(IFrameworkStartupProcedure).IsAssignableFrom(procedureType))
            {
                continue;
            }

            if (startupProcedure != null)
            {
                throw new InvalidOperationException(
                    $"AppConfigs contains more than one {nameof(IFrameworkStartupProcedure)}: " +
                    $"'{startupProcedure.FullName}' and '{procedureType.FullName}'.");
            }

            startupProcedure = procedureType;
        }

        return startupProcedure;
    }

    private static Type FindProcedureType(string procedureName)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
        {
            return null;
        }

        Type type = Type.GetType(procedureName);
        if (type != null)
        {
            return type;
        }

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(procedureName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
