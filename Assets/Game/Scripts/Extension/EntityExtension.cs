using GameFramework;
using UnityGameFramework.Runtime;

public static class EntityExtension
{
    public static int ShowEntity(
        this EntityComponent entityComponent,
        string prefabName,
        string logicTypeName,
        string groupName,
        int priority,
        int entityId,
        object userData = null)
    {
        string assetName = UtilityBuiltin.AssetsPath.GetEntityPath(prefabName);
        entityComponent.ShowEntity(
            entityId,
            Utility.Assembly.GetType(logicTypeName),
            assetName,
            groupName,
            priority,
            userData);
        return entityId;
    }

    public static int ShowEntity(
        this EntityComponent entityComponent,
        string prefabName,
        string logicTypeName,
        string groupName,
        int entityId,
        object userData = null)
    {
        return entityComponent.ShowEntity(prefabName, logicTypeName, groupName, 0, entityId, userData);
    }

    public static int ShowEntity<T>(
        this EntityComponent entityComponent,
        string prefabName,
        string groupName,
        int priority,
        int entityId,
        object userData = null)
        where T : EntityLogic
    {
        string assetName = UtilityBuiltin.AssetsPath.GetEntityPath(prefabName);
        entityComponent.ShowEntity<T>(entityId, assetName, groupName, priority, userData);
        return entityId;
    }

    public static int ShowEntity<T>(
        this EntityComponent entityComponent,
        string prefabName,
        string groupName,
        int entityId,
        object userData = null)
        where T : EntityLogic
    {
        return entityComponent.ShowEntity<T>(prefabName, groupName, 0, entityId, userData);
    }

    public static void HideGroup(this EntityComponent entityComponent, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            Log.Warning("Entity group name is empty.");
            return;
        }

        var group = entityComponent.GetEntityGroup(groupName);
        if (group == null)
        {
            return;
        }

        foreach (Entity entity in group.GetAllEntities())
        {
            entityComponent.HideEntity(entity);
        }
    }

    public static void HideEntitySafe(this EntityComponent entityComponent, int entityId)
    {
        if (entityComponent.IsLoadingEntity(entityId))
        {
            GF.VariablePool.ClearVariables(entityId);
            entityComponent.HideEntity(entityId);
            return;
        }

        if (entityComponent.HasEntity(entityId))
        {
            entityComponent.HideEntity(entityId);
        }
    }

    public static void HideEntitySafe(this EntityComponent entityComponent, EntityLogic logic)
    {
        if (logic != null && logic.Available)
        {
            entityComponent.HideEntity(logic.Entity);
        }
    }

    public static T GetEntity<T>(this EntityComponent entityComponent, int entityId)
        where T : EntityLogic
    {
        return entityComponent.HasEntity(entityId)
            ? entityComponent.GetEntity(entityId).Logic as T
            : null;
    }
}
