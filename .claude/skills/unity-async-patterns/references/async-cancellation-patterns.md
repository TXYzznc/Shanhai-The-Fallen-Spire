# Unity 2022.3 Async Cancellation Reference

This reference targets Unity 2022.3 and the framework's embedded UniTask 2.5.10 package.

## Owner lifetime

```csharp
private CancellationTokenSource _lifetimeCts;

private void Awake()
{
    _lifetimeCts = new CancellationTokenSource();
}

private void OnDestroy()
{
    _lifetimeCts?.Cancel();
    _lifetimeCts?.Dispose();
    _lifetimeCts = null;
}
```

Pass `_lifetimeCts.Token` to every operation that belongs to the component. Do not reuse a disposed source.

## Linked cancellation

```csharp
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    _lifetimeCts.Token,
    timeoutCts.Token);

await UniTask.Delay(1000, cancellationToken: linkedCts.Token);
```

Use a linked token when an operation must end on either owner destruction, explicit replacement, or timeout.

## Error boundary

```csharp
private async UniTask RunAsync(CancellationToken cancellationToken)
{
    try
    {
        await ExecuteAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Expected cancellation.
    }
    catch (Exception exception)
    {
        Debug.LogException(exception);
        throw;
    }
}
```

Catch an exception only when the boundary can add context, recover, or rethrow. Never convert an unexpected exception into a silent success.

## Thread boundary

```csharp
var result = await UniTask.RunOnThreadPool(() => Compute(input), cancellationToken: cancellationToken);
await UniTask.SwitchToMainThread(cancellationToken);
ApplyResult(result);
```

`Compute` must not touch Unity objects. `ApplyResult` runs only after returning to the main thread.

## Coroutine boundary

Use coroutines for simple frame sequencing, and retain their `Coroutine` handle for targeted shutdown. Use UniTask when work needs cancellation composition, return values, concurrency, or exception propagation. A single operation must have one owner and one model.
