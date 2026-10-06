/// <summary>
/// Marks the procedure that should begin after the generic framework preload has completed.
/// A project or an optional package may register at most one such procedure in AppConfigs.
/// </summary>
public interface IFrameworkStartupProcedure
{
}
