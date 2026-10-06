using GameFramework;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

public class GF : GFBuiltin
{
    public static DataModelComponent DataModel { get; private set; }
    public static VariablePoolComponent VariablePool { get; private set; }
    public static StaticUIComponent StaticUI { get; private set; }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes framework extension components that have no project-specific
    /// configuration. Safe to call repeatedly from a startup procedure.
    /// </summary>
    public static void Initialize()
    {
        var baseComponent = GFBuiltin.Base ?? GameEntry.GetComponent<BaseComponent>();
        if (baseComponent == null)
        {
            GFTrace.Failure("GF", "Initialize.MissingBaseComponent");
            return;
        }

        DataModel = GameEntry.GetComponent<DataModelComponent>() ?? baseComponent.gameObject.AddComponent<DataModelComponent>();
        VariablePool = GameEntry.GetComponent<VariablePoolComponent>() ?? baseComponent.gameObject.AddComponent<VariablePoolComponent>();
        StaticUI = GameEntry.GetComponent<StaticUIComponent>();
        GFTrace.Success("GF", "Initialize", null, GFTrace.Data("hasDataModel", (DataModel != null).ToString(), "hasStaticUI", (StaticUI != null).ToString(), "hasVariablePool", (VariablePool != null).ToString()));
    }

    private void OnApplicationQuit()
    {
        OnExitGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (Application.isMobilePlatform && pause)
        {
            OnExitGame();
        }
    }

    public Vector2 GetCanvasSize()
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        return rect.sizeDelta;
    }

    public Vector2 World2ScreenPoint(Camera cam, Vector3 worldPoint)
    {
        var rect = RootCanvas.GetComponent<RectTransform>();
        Vector2 sPoint = cam.WorldToViewportPoint(worldPoint) * rect.sizeDelta;
        return sPoint - rect.sizeDelta * 0.5f;
    }

    private void OnExitGame()
    {
        GFTrace.Info("GF", "Application.Exit");
        GF.Event.FireNow(this, GFEventArgs.Create(GFEventType.ApplicationQuit));
        var exitTime = DateTime.UtcNow.ToString();
        GF.Setting.SetString(ConstBuiltin.Setting.QuitAppTime, exitTime);
        GF.Setting.Save();
        UnityGameFramework.Runtime.Log.Info("Application Quit:{0}", exitTime);
    }
}
