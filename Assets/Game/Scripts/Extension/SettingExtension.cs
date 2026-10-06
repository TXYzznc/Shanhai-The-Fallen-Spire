using GameFramework;
using UnityGameFramework.Runtime;

/// <summary>
/// Persistent framework settings for localization and generic sound groups.
/// </summary>
public static class SettingExtension
{
    public static void SetABTestGroup(this SettingComponent component, string groupName)
    {
        component.SetString(ConstBuiltin.Setting.ABTestGroup, groupName ?? string.Empty);
    }

    public static string GetABTestGroup(this SettingComponent component)
    {
        return component.GetString(ConstBuiltin.Setting.ABTestGroup, string.Empty);
    }

    public static void SetLanguage(this SettingComponent component, GameFramework.Localization.Language language, bool saveSetting = true)
    {
        GFBuiltin.Localization.Language = language;
        component.SetString(ConstBuiltin.Setting.Language, language.ToString());
    }

    public static GameFramework.Localization.Language GetLanguage(this SettingComponent component)
    {
        string value = component.GetString(ConstBuiltin.Setting.Language, string.Empty);
        return System.Enum.TryParse(value, out GameFramework.Localization.Language language)
            ? language
            : GameFramework.Localization.Language.Unspecified;
    }

    public static void SetMediaMute(this SettingComponent component, Const.SoundGroup group, bool isMuted)
    {
        string groupName = group.ToString();
        var soundGroup = GF.Sound.GetSoundGroup(groupName);
        if (soundGroup == null)
        {
            return;
        }

        soundGroup.Mute = isMuted;
        component.SetBool($"Sound.{groupName}.Mute", isMuted);
    }

    public static bool GetMediaMute(this SettingComponent component, Const.SoundGroup group, bool defaultValue = true)
    {
        return component.GetBool($"Sound.{group}.Mute", defaultValue);
    }

    public static void SetMediaVolume(this SettingComponent component, Const.SoundGroup group, float volume)
    {
        string groupName = group.ToString();
        var soundGroup = GF.Sound.GetSoundGroup(groupName);
        if (soundGroup == null)
        {
            return;
        }

        soundGroup.Volume = volume;
        component.SetFloat($"Sound.{groupName}.Volume", volume);
    }

    public static float GetMediaVolume(this SettingComponent component, Const.SoundGroup group, float defaultValue = 1f)
    {
        return component.GetFloat($"Sound.{group}.Volume", defaultValue);
    }
}
