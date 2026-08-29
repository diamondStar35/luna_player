# PrismSharp

Managed bindings for the [Prism](https://github.com/ethindp/prism) speech library
(`prism.dll` / `libprism.so`), plus a screen-reader selection layer and thread confinement.

Tracks **Prism SDK v0.18.2**. Windows and Linux, x64. No third-party dependencies.
**NativeAOT-compatible** — verified by publishing and running a self-contained native binary.

## Layout

| Namespace | Contents |
|---|---|
| `PrismSharp` | The binding. `Context`, `Backend`, `Registry`, `RegistryBuilder`, `Log`, `PrismVersion`, `AudioChunk`, `Error`, `Features`, `Ids`, and the custom-backend adapter. |
| `PrismSharp.Native` | Internal. `[LibraryImport]` declarations, opaque handle types, and the vtable layout. |
| `PrismSharp.Speech` | `ScreenReaderWorker`, `ISpeechDispatcher`, `SpeechBackendAvailability`. |
| `PrismSharp.Speech.ScreenReaders` | `IScreenReader` and the backend-selection policy behind `Factory.Create()`. |
| `PrismSharp.Speech.Playback` | `IPlayer`, the seam for routing synthesized audio into a host mixer, and `Ring`, a circular sample buffer to help implement one. |

Every public member is documented, and the build treats a missing doc comment as an error.

## Native library

Prism itself is not included. Copy `prism.dll` **and `tolk.dll`** (Windows) or `libprism.so`
(Linux) next to your executable; `LibraryImport("prism")` resolves the platform-specific name.

`Context`'s constructor calls `PrismVersion.EnsureSupported()` and throws if the loaded library
predates 0.18.2. Prism is pre-1.0 and its manual warns that minor releases may break compatibility,
so this check exists because a silent ABI mismatch corrupts memory rather than failing cleanly.

## Quick start

```csharp
using var context = new Context();
using var backend = context.CreateBest();   // already initialized
backend.Speak("Hello.", interrupt: true);
```

Or through the screen-reader layer, which applies a selection policy and survives a backend
disappearing:

```csharp
var reader = Factory.Create();
using var worker = new ScreenReaderWorker(reader);
worker.Invoke(r => r.Initialize());
worker.Invoke(r => r.Speak("Hello."));
```

## Interop design

The binding uses `[LibraryImport]` with assembly-wide `DisableRuntimeMarshalling`, so the interop
stubs are generated at compile time and every signature is blittable — C `bool` is a `byte`, `size_t`
is `nuint`, and strings cross as UTF-8. Callbacks into managed code are `[UnmanagedCallersOnly]`
static methods rather than delegates, so the function pointers Prism holds are plain code addresses
with nothing to keep alive and nothing for a trimmer to remove.

`Context` and `Backend` serialize their own native calls, so casual use is safe without external
locking.

## Threading

Prism backend handles are **not** thread-safe. `Context` and `Backend` lock internally, and
`ScreenReaderWorker` additionally confines an `IScreenReader` to one thread — which is stricter than
Prism demands, but matters in practice because Prism initializes COM as apartment-threaded on
Windows.

By default the worker owns a private thread. A host that already has a suitable one — a UI or game
loop — implements `ISpeechDispatcher` and passes it in:

```csharp
using var worker = new ScreenReaderWorker(reader, player, myUiThread);
```

`Invoke<T>` is synchronous because callers want results. `Post` is the non-blocking path; the
interface supplies a default implementation that hops through the thread pool, and a host with a
native post or begin-invoke should override it. `Post` is what Prism's availability poll thread uses,
and that thread cannot scan again until the callback returns.

## Synthesizing to memory

```csharp
foreach (var chunk in backend.SpeakToMemory("Hello"))
    mixer.Write(chunk.Samples.Span, chunk.Channels, chunk.SampleRate);
```

Pull-based: enumerating drives synthesis, a bounded queue applies back-pressure to a slow consumer,
and sample buffers come from the shared array pool and are returned as the sequence advances. A
chunk's samples are valid only until the enumeration advances.

Each `AudioChunk` carries its own format rather than making you read it back from the backend. That
is deliberate: synthesis holds the backend's lock until it finishes, so asking the backend for
`SampleRate` mid-enumeration would deadlock. Abandoning the sequence early is safe.

## Backend availability

Prism can poll for backends appearing and disappearing:

```csharp
using var context = new Context(new ContextOptions
{
    AvailabilityChanged = (id, name, available) => { /* must not block */ },
    PollIntervalMs = 1000,
});
```

The first scan establishes a baseline silently, so query initial availability directly.
`IScreenReader.BackendAvailabilityChanged` surfaces the same signal above the binding.

## Custom and plugin backends

Derive from `PrismBackendBase`, overriding what you support and declaring matching feature bits:

```csharp
using var builder = new RegistryBuilder();
builder.AddBackend("MyTts", priority: 250, () => new MyBackend());
builder.AddLibrary(@"C:\plugins\my_prism_plugin.dll");   // optional
using var registry = builder.Freeze();
using var context = new Context(new ContextOptions { Registry = registry });
```

`DeclaredFeatures` must match the methods you actually override. Prism fills a vtable slot **only**
when its feature bit is declared and rejects any mismatch with `Error.InvalidParam`, so a bit
declared without an override — or an override never declared — is a registration failure rather than
a silent no-op.

The factory is also called once at registration (and the instance disposed) to read
`DeclaredFeatures`, so it must tolerate producing an instance that is never used.

## Notes

- Enumerating backends should use `CreateUninitialized` and read `Features`. Initializing a backend
  merely to list it is slow and, for some backends, prompts the user.
- `CreateBest` / `AcquireBest` return already-initialized backends; do not call `Initialize` on them.
- `Acquire` returns a shared cached instance, so voice and rate changes are visible to every other
  holder. Use `Create` for isolated state.
- Voice parameters are normalized to `[0.0, 1.0]`, with `0.5` the backend default for rate and pitch.
- This library is the binding and the reusable layers only. Application policy — interrupt flags,
  speech-duration estimation, caption mirroring — belongs in the host.
