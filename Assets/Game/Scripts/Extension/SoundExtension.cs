using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

public static class SoundExtension
{
    /// <summary>
    /// Play a non-positional sound effect through the framework's default sound group.
    /// The framework does not provide any sound assets; callers supply a project asset name.
    /// </summary>
    public static int PlayEffect(this SoundComponent soundComponent, string name, string groupName = null)
    {
        return soundComponent.PlayEffect(name, Vector3.zero, groupName);
    }

    /// <summary>
    /// Play a positional sound effect through a configured sound group.
    /// Missing configuration or assets are reported and ignored safely.
    /// </summary>
    public static int PlayEffect(this SoundComponent soundComponent, string name, Vector3 worldPosition, string groupName = null)
    {
        if (soundComponent == null || string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        groupName ??= Const.SoundGroup.Sound.ToString();
        if (!soundComponent.HasSoundGroup(groupName))
        {
            GFTrace.Warning(
                "Sound",
                "PlayEffect.MissingGroup",
                null,
                GFTrace.Data("group", groupName, "asset", name));
            return 0;
        }

        return soundComponent.PlaySound(name, groupName, worldPosition);
    }

    public static int PlaySound(
        this SoundComponent soundComponent,
        string name,
        string groupName,
        Vector3 worldPosition,
        bool loop = false)
    {
        if (soundComponent == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(groupName))
        {
            return 0;
        }

        string assetName = UtilityBuiltin.AssetsPath.GetSoundPath(name);
        if (GFBuiltin.Resource == null || GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist)
        {
            GFTrace.Warning(
                "Sound",
                "PlaySound.MissingAsset",
                null,
                GFTrace.Data("asset", assetName, "group", groupName));
            return 0;
        }

        var parameters = ReferencePool.Acquire<GameFramework.Sound.PlaySoundParams>();
        parameters.Clear();
        parameters.Loop = loop;
        return soundComponent.PlaySound(assetName, groupName, 0, parameters, worldPosition);
    }
}
