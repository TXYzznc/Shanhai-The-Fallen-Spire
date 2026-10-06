---
name: unity-async-patterns
description: Unity 2022.3 异步、协程与取消模式。使用项目内置的 UniTask 2.5.10，覆盖生命周期取消、异常处理、主线程切换、并发去重与协程边界。
tags: unity-2022, unitask, coroutine-patterns, cancellation-tokens, async
tags_cn: Unity 2022.3 异步编程, UniTask, 协程模式, 取消令牌, 生命周期
---

# Unity 2022.3 异步正确性

## 版本与依赖边界

- 目标编辑器为 Unity 2022.3；异步实现统一使用 UniTask 和显式传递的 `CancellationToken`。
- 框架已内置 `Assets/Plugins/UniTask`（2.5.10）。异步实现使用 `Cysharp.Threading.Tasks`。
- 运行时方法以 `Async` 结尾并接受 `CancellationToken`；Unity 生命周期入口只负责启动、取消和记录异常。
- Unity API 只能在主线程调用。后台计算完成后显式切回主线程。

## 选择方式

| 场景 | 使用方式 |
|---|---|
| 单帧或定时的异步工作 | `UniTask.Yield`、`UniTask.Delay` |
| 需要取消或组合等待 | `CancellationTokenSource`、`UniTask.WhenAll` |
| 已有逐帧逻辑 | 协程；保存并在拥有者销毁时停止其句柄 |
| CPU 密集且不访问 Unity API | `UniTask.RunOnThreadPool`，结束后 `SwitchToMainThread` |
| 框架事件完成信号 | 复用已有 `UniTaskCompletionSource`／框架 Await 扩展，不轮询 |

不要让同一资源、操作或状态同时由协程和 UniTask 管理；选择一个拥有者和一条取消路径。

## 生命周期取消

每个 `MonoBehaviour` 持有自己的取消源，并在销毁时对称地取消与释放：

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class AsyncOwner : MonoBehaviour
{
    private CancellationTokenSource _lifetimeCts;

    private void Awake()
    {
        _lifetimeCts = new CancellationTokenSource();
    }

    private async UniTaskVoid Start()
    {
        try
        {
            await InitializeAsync(_lifetimeCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 生命周期结束是预期结果。
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static async UniTask InitializeAsync(CancellationToken cancellationToken)
    {
        await UniTask.Delay(100, cancellationToken: cancellationToken);
    }

    private void OnDestroy()
    {
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
    }
}
```

## 可替换操作与并发

搜索、加载、刷新等“只保留最新结果”的操作，为每次请求建立短生命周期的取消源，并与拥有者令牌链接。取消旧请求后再创建新请求；在应用结果前检查令牌。

```csharp
private CancellationTokenSource _operationCts;

private async UniTask RefreshAsync(CancellationToken lifetimeToken)
{
    _operationCts?.Cancel();
    _operationCts?.Dispose();
    _operationCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);

    var token = _operationCts.Token;
    await UniTask.Yield(cancellationToken: token);
    token.ThrowIfCancellationRequested();
    // 在主线程应用最新结果。
}
```

## 线程与异常

- `async void` 仅可作为 Unity 回调适配层，且必须在其中捕获、分类并记录异常。
- `UniTaskVoid` 同样只用于回调入口；内部工作使用 `UniTask` 或 `UniTask<T>`，使调用者可等待并处理异常。
- 使用 `UniTask.RunOnThreadPool` 的委托不得访问 `GameObject`、`Transform`、`ScriptableObject` 或任何 Unity API；完成后调用 `await UniTask.SwitchToMainThread(cancellationToken)`。
- 对 `OperationCanceledException` 按预期取消处理；其他异常不得静默吞掉。

## 批处理与协程

- 无图形批处理、无头环境和测试中，不依赖渲染阶段。协程使用 `yield return null`，UniTask 使用 `UniTask.Yield` 或 `UniTask.NextFrame`。
- 协程异常不会自然传播给启动者；需要可组合错误处理时使用 UniTask。
- 不使用 `StopAllCoroutines()` 作为清理方式；只停止自己保存的 `Coroutine` 句柄。

## 自检

- 所有异步链都能被拥有者取消，并在拥有者结束时释放 CTS。
- 没有 Unity API 从后台线程访问。
- 没有未观察的 `UniTask` 异常。
- 没有不属于 Unity 2022.3 与内置 UniTask 的异步 API。
