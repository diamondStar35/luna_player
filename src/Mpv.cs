// Single-file C# binding for libmpv, ported from the accompanying mpv.py.
// Original Python wrapper copyright (C) 2017-2020 Sebastian Götte <code@jaseg.net>.
// C# port generated for this project in 2026.
// SPDX-License-Identifier: AGPL-3.0-or-later
#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MpvNet
{
    public readonly struct MpvHandle
    {
        public MpvHandle(IntPtr value)
        {
            Value = value;
        }

        public IntPtr Value { get; }
        public bool IsNull => Value == IntPtr.Zero;

        public static implicit operator IntPtr(MpvHandle handle) => handle.Value;

        public static implicit operator MpvHandle(IntPtr value) => new MpvHandle(value);
    }

    public readonly struct MpvRenderCtxHandle
    {
        public MpvRenderCtxHandle(IntPtr value)
        {
            Value = value;
        }

        public IntPtr Value { get; }
        public bool IsNull => Value == IntPtr.Zero;

        public static implicit operator IntPtr(MpvRenderCtxHandle handle) => handle.Value;

        public static implicit operator MpvRenderCtxHandle(IntPtr value) =>
            new MpvRenderCtxHandle(value);
    }

    [Obsolete("Use MpvRenderContext and MpvRenderCtxHandle instead.")]
    public readonly struct MpvOpenGLCbContext
    {
        public MpvOpenGLCbContext(IntPtr value)
        {
            Value = value;
        }

        public IntPtr Value { get; }
        public bool IsNull => Value == IntPtr.Zero;

        public static implicit operator IntPtr(MpvOpenGLCbContext handle) => handle.Value;

        public static implicit operator MpvOpenGLCbContext(IntPtr value) =>
            new MpvOpenGLCbContext(value);
    }

    public interface IOverlay : IDisposable
    {
        int Id { get; }
        void Remove();
    }

    public sealed class FileOverlay : IOverlay
    {
        private readonly MPV mpv;
        private bool removed;
        public int Id { get; }
        public string? Filename { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        internal FileOverlay(
            MPV mpv,
            int id,
            string? filename,
            int width,
            int height,
            int? stride,
            int x,
            int y
        )
        {
            this.mpv = mpv;
            Id = id;
            Filename = filename;
            Width = width;
            Height = height;
            Stride = stride ?? width * 4;
            X = x;
            Y = y;
            if (filename != null)
                Update();
        }

        public void Update(
            string? filename = null,
            int? width = null,
            int? height = null,
            int? stride = null,
            int? x = null,
            int? y = null
        )
        {
            if (removed)
                throw new ObjectDisposedException(nameof(FileOverlay));
            Filename = filename ?? Filename;
            Width = width ?? Width;
            Height = height ?? Height;
            Stride = stride ?? (Stride == 0 ? Width * 4 : Stride);
            X = x ?? X;
            Y = y ?? Y;
            if (Filename == null)
                throw new InvalidOperationException("An overlay filename is required");
            if (Width <= 0 || Height <= 0)
                throw new ArgumentOutOfRangeException("Overlay dimensions must be positive");
            mpv.OverlayAdd(Id, X, Y, Filename, 0, "bgra", Width, Height, Stride);
        }

        public void Remove()
        {
            if (removed)
                return;
            removed = true;
            mpv.RemoveOverlay(Id);
        }

        public void Dispose() => Remove();
    }

    public sealed class ImageOverlay : IOverlay
    {
        private readonly MPV mpv;
        private GCHandle pin;
        private byte[]? buffer;
        private bool removed;
        public int Id { get; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        internal ImageOverlay(MPV mpv, int id, byte[]? bgra, int width, int height, int x, int y)
        {
            this.mpv = mpv;
            Id = id;
            X = x;
            Y = y;
            if (bgra != null)
                Update(bgra, width, height, x, y);
        }

        internal ImageOverlay(MPV mpv, int id, MpvImage image, int x, int y)
        {
            this.mpv = mpv;
            Id = id;
            X = x;
            Y = y;
            Update(image, x, y);
        }

        public void Update(MpvImage image, int? x = null, int? y = null)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            byte[] bgra = new byte[checked(image.Width * image.Height * 4)];
            for (int row = 0; row < image.Height; row++)
            {
                int sourceRow = row * image.Stride;
                for (int column = 0; column < image.Width; column++)
                {
                    int destination = (row * image.Width + column) * 4;
                    if (
                        image.Format.Equals("bgra", StringComparison.OrdinalIgnoreCase)
                        || image.Format.Equals("bgr0", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        int source = sourceRow + column * 4;
                        bgra[destination] = image.Data[source];
                        bgra[destination + 1] = image.Data[source + 1];
                        bgra[destination + 2] = image.Data[source + 2];
                        bgra[destination + 3] = image.Format.Equals(
                            "bgr0",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? (byte)255
                            : image.Data[source + 3];
                    }
                    else if (image.Format.Equals("rgba", StringComparison.OrdinalIgnoreCase))
                    {
                        int source = sourceRow + column * 4;
                        bgra[destination] = image.Data[source + 2];
                        bgra[destination + 1] = image.Data[source + 1];
                        bgra[destination + 2] = image.Data[source];
                        bgra[destination + 3] = image.Data[source + 3];
                    }
                    else if (image.Format.Equals("rgb24", StringComparison.OrdinalIgnoreCase))
                    {
                        int source = sourceRow + column * 3;
                        bgra[destination] = image.Data[source + 2];
                        bgra[destination + 1] = image.Data[source + 1];
                        bgra[destination + 2] = image.Data[source];
                        bgra[destination + 3] = 255;
                    }
                    else
                        throw new NotSupportedException(
                            $"Unsupported image format '{image.Format}'."
                        );
                }
            }
            Update(bgra, image.Width, image.Height, x, y, true);
        }

        public void Update(
            byte[]? bgra = null,
            int? width = null,
            int? height = null,
            int? x = null,
            int? y = null,
            bool premultiplyAlpha = true
        )
        {
            if (removed)
                throw new ObjectDisposedException(nameof(ImageOverlay));
            Width = width ?? Width;
            Height = height ?? Height;
            X = x ?? X;
            Y = y ?? Y;
            if (bgra != null)
            {
                if (Width <= 0 || Height <= 0 || bgra.Length < checked(Width * Height * 4))
                    throw new ArgumentException("BGRA data and positive dimensions are required");
                if (pin.IsAllocated)
                    pin.Free();
                buffer = (byte[])bgra.Clone();
                if (premultiplyAlpha)
                    for (int i = 0; i < Width * Height * 4; i += 4)
                    {
                        int a = buffer[i + 3];
                        buffer[i] = (byte)(buffer[i] * a / 255);
                        buffer[i + 1] = (byte)(buffer[i + 1] * a / 255);
                        buffer[i + 2] = (byte)(buffer[i + 2] * a / 255);
                    }
                pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }
            if (buffer == null)
                throw new InvalidOperationException("Image data is required");
            mpv.OverlayAdd(
                Id,
                X,
                Y,
                "&" + pin.AddrOfPinnedObject().ToInt64(),
                0,
                "bgra",
                Width,
                Height,
                Width * 4
            );
        }

        public void Remove()
        {
            if (removed)
                return;
            removed = true;
            try
            {
                mpv.RemoveOverlay(Id);
            }
            finally
            {
                if (pin.IsAllocated)
                    pin.Free();
                buffer = null;
            }
        }

        public void Dispose() => Remove();
    }

    internal sealed class RenderParameters : IDisposable
    {
        private readonly List<IntPtr> memory = new List<IntPtr>();
        private readonly List<Delegate> delegates = new List<Delegate>();
        public IntPtr Pointer { get; }

        public RenderParameters(
            IEnumerable<KeyValuePair<string, object?>> values,
            bool terminate = true
        )
        {
            var ps = values.Select(v => Make(v.Key, v.Value)).ToList();
            int size = Marshal.SizeOf<NativeRenderParam>();
            Pointer = Alloc(size * (ps.Count + (terminate ? 1 : 0)));
            for (int i = 0; i < ps.Count; i++)
                Marshal.StructureToPtr(ps[i], IntPtr.Add(Pointer, i * size), false);
            if (terminate)
                Marshal.StructureToPtr(
                    new NativeRenderParam(),
                    IntPtr.Add(Pointer, ps.Count * size),
                    false
                );
        }

        public NativeRenderParam Single => Marshal.PtrToStructure<NativeRenderParam>(Pointer);

        private NativeRenderParam Make(string name, object? value)
        {
            int type = name switch
            {
                "invalid" => 0,
                "api_type" => 1,
                "opengl_init_params" => 2,
                "opengl_fbo" => 3,
                "flip_y" => 4,
                "depth" => 5,
                "icc_profile" => 6,
                "ambient_light" => 7,
                "x11_display" => 8,
                "wl_display" => 9,
                "advanced_control" => 10,
                "next_frame_info" => 11,
                "block_for_target_time" => 12,
                "skip_rendering" => 13,
                "drm_display" => 14,
                "drm_draw_surface_size" => 15,
                "drm_display_v2" => 16,
                _ => throw new ArgumentException("Unknown render parameter " + name),
            };
            IntPtr data;
            switch (type)
            {
                case 0:
                    data = IntPtr.Zero;
                    break;
                case 1:
                    byte[] z = Native.Z(Convert.ToString(value) ?? "");
                    data = Alloc(z.Length);
                    Marshal.Copy(z, 0, data, z.Length);
                    break;
                case 2:
                    Func<string, IntPtr> get = value switch
                    {
                        MpvOpenGLInitParams initParams => initParams.GetProcAddress,
                        Func<string, IntPtr> function => function,
                        _ => throw new ArgumentException(
                            "opengl_init_params must be MpvOpenGLInitParams or Func<string, IntPtr>"
                        ),
                    };
                    Native.GetProcAddressCallback cb = (_, p) => get(Native.Utf8(p) ?? "");
                    delegates.Add(cb);
                    var init = new NativeOpenGLInitParams
                    {
                        GetProcAddress = Marshal.GetFunctionPointerForDelegate(cb),
                    };
                    data = Struct(init);
                    break;
                case 3:
                    data = Struct(
                        value is MpvOpenGLFbo fbo
                            ? fbo
                            : throw new ArgumentException("opengl_fbo must be MpvOpenGLFbo")
                    );
                    break;
                case 4:
                case 5:
                case 7:
                case 10:
                case 12:
                case 13:
                    int i = value is bool b ? (b ? 1 : 0) : Convert.ToInt32(value);
                    data = Alloc(sizeof(int));
                    Marshal.WriteInt32(data, i);
                    break;
                case 6:
                    byte[] bytes =
                        value as byte[]
                        ?? throw new ArgumentException("icc_profile must be byte[]");
                    IntPtr raw = Alloc(bytes.Length);
                    if (bytes.Length > 0)
                        Marshal.Copy(bytes, 0, raw, bytes.Length);
                    data = Struct(
                        new NativeByteArray { Data = raw, Size = (UIntPtr)(uint)bytes.Length }
                    );
                    break;
                case 8:
                case 9:
                    data = value is IntPtr ptr
                        ? ptr
                        : throw new ArgumentException(name + " must be IntPtr");
                    break;
                case 11:
                    data = Struct(value is MpvRenderFrameInfo fi ? fi : new MpvRenderFrameInfo());
                    break;
                case 14:
                    data = Struct(
                        value is MpvOpenGLDrmParams drm
                            ? drm
                            : throw new ArgumentException(name + " must be MpvOpenGLDrmParams")
                    );
                    break;
                case 16:
                    data = Struct(
                        value is MpvOpenGLDrmParamsV2 drmV2
                            ? drmV2
                            : throw new ArgumentException(name + " must be MpvOpenGLDrmParamsV2")
                    );
                    break;
                case 15:
                    data = Struct(
                        value is MpvOpenGLDrmDrawSurfaceSize sz
                            ? sz
                            : throw new ArgumentException(
                                "drm_draw_surface_size must be MpvOpenGLDrmDrawSurfaceSize"
                            )
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new NativeRenderParam { Type = type, Data = data };
        }

        private IntPtr Struct<T>(T value)
            where T : struct
        {
            IntPtr p = Alloc(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(value, p, false);
            return p;
        }

        private IntPtr Alloc(int size)
        {
            var p = Marshal.AllocHGlobal(Math.Max(size, 1));
            memory.Add(p);
            return p;
        }

        public void Dispose()
        {
            foreach (IntPtr p in memory.AsEnumerable().Reverse())
                Marshal.FreeHGlobal(p);
            memory.Clear();
        }
    }

    public sealed class MpvRenderContext : DynamicObject, IDisposable
    {
        private IntPtr handle;
        private Native.RenderUpdateCallback? updateWrapper;
        private Action? updateCallback;
        public IntPtr Handle => handle;
        public Action? UpdateCallback
        {
            get => updateCallback;
            set
            {
                Check();
                updateCallback = value;
                Action call = value ?? (() => { });
                updateWrapper = _ => call();
                Native.mpv_render_context_set_update_callback(handle, updateWrapper, IntPtr.Zero);
            }
        }

        public MpvRenderContext(
            MPV mpv,
            string apiType,
            IDictionary<string, object?>? parameters = null
        )
        {
            var d =
                parameters != null
                    ? new Dictionary<string, object?>(parameters)
                    : new Dictionary<string, object?>();
            d["api_type"] = apiType;
            using var p = new RenderParameters(d);
            MpvError.Throw(Native.mpv_render_context_create(out handle, mpv.Handle, p.Pointer));
        }

        public MpvRenderContext(MPV mpv, string apiType, IEnumerable<MpvRenderParameter> parameters)
            : this(
                mpv,
                apiType,
                parameters.ToDictionary(
                    parameter => parameter.Name,
                    parameter => parameter.Value,
                    StringComparer.Ordinal
                )
            ) { }

        public void SetParameter(string name, object? value)
        {
            Check();
            using var p = new RenderParameters(
                new[] { new KeyValuePair<string, object?>(name, value) },
                false
            );
            var native = p.Single;
            MpvError.Throw(Native.mpv_render_context_set_parameter(handle, ref native));
        }

        public MpvRenderFrameInfo GetNextFrameInfo()
        {
            Check();
            using var p = new RenderParameters(
                new[]
                {
                    new KeyValuePair<string, object?>("next_frame_info", new MpvRenderFrameInfo()),
                },
                false
            );
            var native = p.Single;
            MpvError.Throw(Native.mpv_render_context_get_info(handle, ref native));
            return Marshal.PtrToStructure<MpvRenderFrameInfo>(native.Data);
        }

        public object GetInfo(string name)
        {
            if (name == "next_frame_info")
                return GetNextFrameInfo();
            throw new NotSupportedException(
                $"libmpv does not define a readable value for render parameter '{name}'."
            );
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder.Name == "update_cb")
            {
                result = UpdateCallback;
                return true;
            }
            if (binder.Name == "handle")
            {
                result = Handle;
                return true;
            }
            result = GetInfo(binder.Name);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (binder.Name == "update_cb")
            {
                UpdateCallback = (Action?)value;
                return true;
            }
            SetParameter(binder.Name, value);
            return true;
        }

        public bool Update()
        {
            Check();
            return (Native.mpv_render_context_update(handle) & 1) != 0;
        }

        public void Render(IDictionary<string, object?> parameters)
        {
            Check();
            using var p = new RenderParameters(parameters);
            MpvError.Throw(Native.mpv_render_context_render(handle, p.Pointer));
        }

        public void Render(MpvOpenGLFbo fbo, bool flipY = false) =>
            Render(new Dictionary<string, object?> { { "opengl_fbo", fbo }, { "flip_y", flipY } });

        public void ReportSwap()
        {
            Check();
            Native.mpv_render_context_report_swap(handle);
        }

        public void Free() => Dispose();

        public void Dispose()
        {
            IntPtr h = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (h != IntPtr.Zero)
                Native.mpv_render_context_free(h);
            GC.SuppressFinalize(this);
        }

        ~MpvRenderContext()
        {
            Dispose();
        }

        private void Check()
        {
            if (handle == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(MpvRenderContext));
        }
    }

    [Obsolete("libmpv deprecated the OpenGL callback API in 0.29. Use MpvRenderContext instead.")]
    public sealed class MpvOpenGLCallbackContext
    {
        private readonly IntPtr handle;
        private Native.OpenGlUpdateCallback? updateWrapper;
        private Native.OpenGlGetProcAddressCallback? getProcAddressWrapper;

        public MpvOpenGLCallbackContext(MPV mpv)
        {
            try
            {
                handle = Native.mpv_get_sub_api(mpv.Handle, 1);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new NotSupportedException(
                    "This libmpv does not export the deprecated OpenGL callback API.",
                    ex
                );
            }
            if (handle == IntPtr.Zero)
                throw new NotSupportedException(
                    "This libmpv does not provide the deprecated OpenGL callback API."
                );
        }

        public IntPtr Handle => handle;

        public void SetUpdateCallback(Action callback)
        {
            updateWrapper = _ => callback();
            Native.mpv_opengl_cb_set_update_callback(handle, updateWrapper, IntPtr.Zero);
        }

        public void Initialize(Func<string, IntPtr> getProcAddress, string? extensions = null)
        {
            getProcAddressWrapper = (_, name) => getProcAddress(Native.Utf8(name) ?? "");
            MpvError.Throw(
                Native.mpv_opengl_cb_init_gl(
                    handle,
                    extensions == null ? null : Native.Z(extensions),
                    getProcAddressWrapper,
                    IntPtr.Zero
                )
            );
        }

        public void Draw(int framebuffer, int width, int height) =>
            MpvError.Throw(Native.mpv_opengl_cb_draw(handle, framebuffer, width, height));

        public void Render(int framebuffer, int viewport) =>
            MpvError.Throw(Native.mpv_opengl_cb_render(handle, framebuffer, viewport));

        public void ReportFlip(ulong presentationTimestamp = 0) =>
            MpvError.Throw(Native.mpv_opengl_cb_report_flip(handle, presentationTimestamp));

        public void Uninitialize() => MpvError.Throw(Native.mpv_opengl_cb_uninit_gl(handle));
    }

    public sealed class MpvShutdownException : InvalidOperationException
    {
        public MpvShutdownException(string message)
            : base(message) { }
    }

    public sealed class MpvPropertyUnavailableException : InvalidOperationException
    {
        public MpvPropertyUnavailableException(string message)
            : base(message) { }
    }

    public sealed class MpvException : Exception
    {
        public int ErrorCode { get; }

        public MpvException(int code, string message)
            : base(message)
        {
            ErrorCode = code;
        }
    }

    public enum MpvFormat
    {
        None = 0,
        String = 1,
        OsdString = 2,
        Flag = 3,
        Int64 = 4,
        Double = 5,
        Node = 6,
        NodeArray = 7,
        NodeMap = 8,
        ByteArray = 9,
    }

    public enum MpvEventId
    {
        None = 0,
        Shutdown = 1,
        LogMessage = 2,
        GetPropertyReply = 3,
        SetPropertyReply = 4,
        CommandReply = 5,
        StartFile = 6,
        EndFile = 7,
        FileLoaded = 8,
        TracksChanged = 9,
        TrackSwitched = 10,
        Idle = 11,
        Pause = 12,
        Unpause = 13,
        Tick = 14,
        ScriptInputDispatch = 15,
        ClientMessage = 16,
        VideoReconfig = 17,
        AudioReconfig = 18,
        MetadataUpdate = 19,
        Seek = 20,
        PlaybackRestart = 21,
        PropertyChange = 22,
        ChapterChange = 23,
        QueueOverflow = 24,
        Hook = 25,
    }

    public static class MpvEventIds
    {
        public static readonly IReadOnlyList<MpvEventId> Any = Enum.GetValues<MpvEventId>()
            .Where(id => id != MpvEventId.None)
            .ToArray();

        public static MpvEventId FromString(string name)
        {
            string normalized = name.Replace("-", string.Empty).Replace("_", string.Empty);
            foreach (MpvEventId id in Enum.GetValues<MpvEventId>())
            {
                if (string.Equals(id.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                    return id;
            }
            throw new ArgumentException($"Unknown mpv event name '{name}'.", nameof(name));
        }

        public static MpvEventId FromStr(string name) => FromString(name);

        public static string GetName(MpvEventId id) =>
            Native.Utf8(Native.mpv_event_name(id)) ?? id.ToString();
    }

    public static class MpvDecoders
    {
        public static object Identity(byte[] value) => value;

        public static object IdentityDecoder(byte[] value) => Identity(value);

        public static object Strict(byte[] value) => Native.StrictDecode(value);

        public static object StrictDecoder(byte[] value) => Strict(value);

        public static object Lazy(byte[] value) => Native.LazyDecode(value);

        public static object LazyDecoder(byte[] value) => Lazy(value);
    }

    public enum MpvEndFileReason
    {
        Eof = 0,
        Restarted = 1,
        Aborted = 2,
        Stop = 2,
        Quit = 3,
        Error = 4,
        Redirect = 5,
    }

    public enum MpvSubApi
    {
        OpenGlCallback = 1,
    }

    public sealed class MpvOpenGLInitParams
    {
        public MpvOpenGLInitParams(Func<string, IntPtr> getProcAddress)
        {
            GetProcAddress =
                getProcAddress ?? throw new ArgumentNullException(nameof(getProcAddress));
        }

        public Func<string, IntPtr> GetProcAddress { get; }
    }

    public sealed class MpvRenderParameter
    {
        public MpvRenderParameter(string name, object? value = null)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object? Value { get; }
    }

    public static class MpvRenderParameters
    {
        public static IReadOnlyList<MpvRenderParameter> KwargsToRenderParamArray(
            IReadOnlyDictionary<string, object?> values
        )
        {
            var parameters = values
                .Select(pair => new MpvRenderParameter(pair.Key, pair.Value))
                .ToList();
            parameters.Add(new MpvRenderParameter("invalid"));
            return parameters;
        }
    }

    public sealed class MpvNode
    {
        public MpvNode(MpvFormat format, object? value)
        {
            Format = format;
            Value = value;
        }

        public MpvFormat Format { get; }
        public object? Value { get; }

        public object? NodeValue() => Value;

        public static object? NodeCastValue(MpvNode node) => node.Value;
    }

    public sealed class MpvNodeUnion
    {
        public string? String { get; init; }
        public bool? Flag { get; init; }
        public long? Int64 { get; init; }
        public double? Double { get; init; }
        public MpvNode? Node { get; init; }
        public MpvNodeList? List { get; init; }
        public MpvByteArray? ByteArray { get; init; }
    }

    public sealed class MpvNodeList
    {
        private readonly IReadOnlyList<object?> values;
        private readonly IReadOnlyDictionary<string, object?>? map;

        public MpvNodeList(IReadOnlyList<object?> values)
        {
            this.values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public MpvNodeList(IReadOnlyDictionary<string, object?> values)
        {
            map = values ?? throw new ArgumentNullException(nameof(values));
            this.values = values.Values.ToArray();
        }

        public IReadOnlyList<object?> ArrayValue() => values;

        public IReadOnlyDictionary<string, object?> DictionaryValue() =>
            map ?? throw new InvalidOperationException("This node list does not contain map keys.");

        public IReadOnlyDictionary<string, object?> DictValue() => DictionaryValue();
    }

    public sealed class MpvByteArray
    {
        public MpvByteArray(byte[] value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public byte[] Value { get; }

        public byte[] BytesValue() => (byte[])Value.Clone();
    }

    public static class MpvError
    {
        public const int Success = 0,
            EventQueueFull = -1,
            NoMemory = -2,
            Uninitialized = -3,
            InvalidParameter = -4,
            OptionNotFound = -5,
            OptionFormat = -6,
            OptionError = -7,
            PropertyNotFound = -8,
            PropertyFormat = -9,
            PropertyUnavailable = -10,
            PropertyError = -11,
            Command = -12,
            LoadingFailed = -13,
            AudioOutputInitFailed = -14,
            VideoOutputInitFailed = -15,
            NothingToPlay = -16,
            UnknownFormat = -17,
            Unsupported = -18,
            NotImplemented = -19,
            Generic = -20;

        internal static void Throw(int code)
        {
            if (code >= 0)
                return;
            string text = Native.Utf8(Native.mpv_error_string(code)) ?? "Unknown libmpv error";
            if (code == PropertyUnavailable)
                throw new MpvPropertyUnavailableException(text);
            throw new MpvException(code, text);
        }

        public static Exception DefaultErrorHandler(int code) =>
            new MpvException(
                code,
                Native.Utf8(Native.mpv_error_string(code)) ?? "Unknown libmpv error"
            );

        public static void RaiseForEc(int code) => Throw(code);

        public static void RaiseForErrorCode(int code) => Throw(code);
    }

    public sealed class MpvEvent
    {
        public MpvEventId EventId { get; internal set; }
        public int Error { get; internal set; }
        public ulong ReplyUserData { get; internal set; }
        public object? Data { get; internal set; }

        public override string ToString() => $"{EventId} (error {Error})";

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?>
            {
                ["event_id"] = (int)EventId,
                ["error"] = Error,
                ["reply_userdata"] = ReplyUserData,
                ["event"] = Data,
            };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvPropertyEvent
    {
        public string Name { get; internal set; } = "";
        public MpvFormat Format { get; internal set; }
        public object? Value { get; internal set; }

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?>
            {
                ["name"] = Name,
                ["format"] = Format,
                ["value"] = Value,
            };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvLogMessage
    {
        public string Prefix { get; internal set; } = "";
        public string Level { get; internal set; } = "";
        public object? Text { get; internal set; }

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?>
            {
                ["prefix"] = Prefix,
                ["level"] = Level,
                ["text"] = Text,
            };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvEndFileEvent
    {
        public MpvEndFileReason Reason { get; internal set; }
        public int Error { get; internal set; }
        public long PlaylistEntryId { get; internal set; }
        public long PlaylistInsertId { get; internal set; }
        public int PlaylistInsertNumEntries { get; internal set; }
        public int Value => (int)Reason;

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?> { ["reason"] = (int)Reason, ["error"] = Error };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvClientMessage
    {
        public IReadOnlyList<string> Arguments { get; internal set; } = Array.Empty<string>();

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?> { ["args"] = Arguments };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvScriptInputDispatch
    {
        public int Argument0 { get; internal set; }
        public string Type { get; internal set; } = "";

        public IReadOnlyDictionary<string, object?> AsDictionary() =>
            new Dictionary<string, object?> { ["arg0"] = Argument0, ["type"] = Type };

        public IReadOnlyDictionary<string, object?> AsDict() => AsDictionary();
    }

    public sealed class MpvImage
    {
        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public byte[] Data { get; }
        public string Format { get; }

        public MpvImage(int width, int height, int stride, byte[] data, string format)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Data = data;
            Format = format;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeNode
    {
        public NativeNodeValue Value;
        public MpvFormat Format;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct NativeNodeValue
    {
        [FieldOffset(0)]
        public IntPtr String;

        [FieldOffset(0)]
        public int Flag;

        [FieldOffset(0)]
        public long Int64;

        [FieldOffset(0)]
        public double Double;

        [FieldOffset(0)]
        public IntPtr Node;

        [FieldOffset(0)]
        public IntPtr List;

        [FieldOffset(0)]
        public IntPtr ByteArray;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeNodeList
    {
        public int Count;
        public IntPtr Values;
        public IntPtr Keys;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeByteArray
    {
        public IntPtr Data;
        public UIntPtr Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEvent
    {
        public MpvEventId EventId;
        public int Error;
        public ulong ReplyUserData;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEventProperty
    {
        public IntPtr Name;
        public MpvFormat Format;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEventLogMessage
    {
        public IntPtr Prefix,
            Level,
            Text;
        public MpvFormat LogLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEventEndFile
    {
        public MpvEndFileReason Reason;
        public int Error;
        public long PlaylistEntryId,
            PlaylistInsertId;
        public int PlaylistInsertNumEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEventClientMessage
    {
        public int NumArgs;
        public IntPtr Args;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeEventScriptInputDispatch
    {
        public int Argument0;
        public IntPtr Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeStreamCallbackInfo
    {
        public IntPtr Cookie,
            Read,
            Seek,
            Size,
            Close,
            Cancel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvOpenGLFbo
    {
        public int Fbo,
            Width,
            Height,
            InternalFormat;

        public MpvOpenGLFbo(int width, int height, int fbo = 0, int internalFormat = 0)
        {
            Fbo = fbo;
            Width = width;
            Height = height;
            InternalFormat = internalFormat;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvRenderFrameInfo
    {
        public long Flags,
            TargetTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvOpenGLDrmParams
    {
        public int FileDescriptor;
        public int CrtcId,
            ConnectorId;
        public IntPtr AtomicRequest;
        public int RenderFileDescriptor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvOpenGLDrmParamsV2
    {
        public int FileDescriptor;
        public int CrtcId,
            ConnectorId;
        public IntPtr AtomicRequest;
        public int RenderFileDescriptor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvOpenGLDrmDrawSurfaceSize
    {
        public int Width,
            Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRenderParam
    {
        public int Type;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeOpenGLInitParams
    {
        public IntPtr GetProcAddress,
            Context,
            ExtraExtensions;
    }

    internal static class Native
    {
        private const string Lib = "mpv";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void WakeupCallback(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RenderUpdateCallback(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void OpenGlUpdateCallback(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr OpenGlGetProcAddressCallback(IntPtr context, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr GetProcAddressCallback(IntPtr context, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int StreamOpenCallback(IntPtr userData, IntPtr uri, IntPtr info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate long StreamReadCallback(IntPtr cookie, IntPtr buffer, ulong count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate long StreamSeekCallback(IntPtr cookie, long offset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate long StreamSizeCallback(IntPtr cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void StreamCloseCallback(IntPtr cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void StreamCancelCallback(IntPtr cookie);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong mpv_client_api_version();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_error_string(int error);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_event_name(MpvEventId id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_create();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_create_client(IntPtr handle, byte[] name);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_client_name(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_initialize(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_destroy(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_detach_destroy(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_terminate_destroy(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_free(IntPtr data);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_load_config_file(IntPtr handle, byte[] file);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long mpv_get_time_us(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_set_option(
            IntPtr handle,
            byte[] name,
            MpvFormat format,
            IntPtr data
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_set_option_string(IntPtr handle, byte[] name, byte[] value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_command(IntPtr handle, IntPtr args);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_command_string(IntPtr handle, byte[] command);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_command_async(
            IntPtr handle,
            ulong replyUserData,
            IntPtr args
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_command_node(
            IntPtr handle,
            ref NativeNode args,
            out NativeNode result
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_command_node_async(
            IntPtr handle,
            ulong replyUserData,
            ref NativeNode args
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_set_property_string(
            IntPtr handle,
            byte[] name,
            byte[] value
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_set_property(
            IntPtr handle,
            byte[] name,
            MpvFormat format,
            IntPtr data
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_set_property_async(
            IntPtr handle,
            ulong replyUserData,
            byte[] name,
            MpvFormat format,
            IntPtr data
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_get_property(
            IntPtr handle,
            byte[] name,
            MpvFormat format,
            IntPtr data
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_get_property_string(IntPtr handle, byte[] name);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_get_property_osd_string(IntPtr handle, byte[] name);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_get_property_async(
            IntPtr handle,
            ulong replyUserData,
            byte[] name,
            MpvFormat format
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_observe_property(
            IntPtr handle,
            ulong id,
            byte[] name,
            MpvFormat format
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_unobserve_property(IntPtr handle, ulong id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_request_log_messages(IntPtr handle, byte[] level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_request_event(IntPtr handle, MpvEventId eventId, int enable);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_wait_event(IntPtr handle, double timeout);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_wakeup(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_set_wakeup_callback(
            IntPtr handle,
            WakeupCallback callback,
            IntPtr context
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_get_wakeup_pipe(IntPtr handle);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_stream_cb_add_ro(
            IntPtr handle,
            byte[] protocol,
            IntPtr userData,
            StreamOpenCallback open
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_free_node_contents(ref NativeNode node);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_render_context_create(
            out IntPtr context,
            IntPtr handle,
            IntPtr parameters
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_render_context_set_parameter(
            IntPtr context,
            ref NativeRenderParam parameter
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_render_context_get_info(
            IntPtr context,
            ref NativeRenderParam parameter
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_render_context_set_update_callback(
            IntPtr context,
            RenderUpdateCallback callback,
            IntPtr callbackContext
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong mpv_render_context_update(IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_render_context_render(IntPtr context, IntPtr parameters);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_render_context_report_swap(IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_render_context_free(IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr mpv_get_sub_api(IntPtr handle, int subApi);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void mpv_opengl_cb_set_update_callback(
            IntPtr context,
            OpenGlUpdateCallback callback,
            IntPtr callbackContext
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_opengl_cb_init_gl(
            IntPtr context,
            byte[]? extensions,
            OpenGlGetProcAddressCallback getProcAddress,
            IntPtr callbackContext
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_opengl_cb_draw(
            IntPtr context,
            int fbo,
            int width,
            int height
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_opengl_cb_render(IntPtr context, int fbo, int viewport);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_opengl_cb_report_flip(
            IntPtr context,
            ulong presentationTimestamp
        );

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int mpv_opengl_cb_uninit_gl(IntPtr context);

        internal static byte[] Z(string text) => Encoding.UTF8.GetBytes(text + "\0");

        internal static string? Utf8(IntPtr p) =>
            p == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(p);

        internal static object? Decode(IntPtr p, Func<byte[], object>? decoder = null)
        {
            if (p == IntPtr.Zero)
                return null;
            int n = 0;
            while (Marshal.ReadByte(p, n) != 0)
                n++;
            var b = new byte[n];
            Marshal.Copy(p, b, 0, n);
            return (decoder ?? StrictDecode)(b);
        }

        internal static object StrictDecode(byte[] bytes) =>
            new UTF8Encoding(false, true).GetString(bytes);

        internal static object LazyDecode(byte[] bytes)
        {
            try
            {
                return StrictDecode(bytes);
            }
            catch (DecoderFallbackException)
            {
                return bytes;
            }
        }

        internal static object RawDecode(byte[] bytes) => bytes;

        internal static object? NodeValue(NativeNode node, Func<byte[], object>? decoder = null)
        {
            decoder ??= StrictDecode;
            switch (node.Format)
            {
                case MpvFormat.None:
                    return null;
                case MpvFormat.String:
                    return Decode(node.Value.String, decoder);
                case MpvFormat.OsdString:
                    return Utf8(node.Value.String);
                case MpvFormat.Flag:
                    return node.Value.Flag != 0;
                case MpvFormat.Int64:
                    return node.Value.Int64;
                case MpvFormat.Double:
                    return node.Value.Double;
                case MpvFormat.Node:
                    return node.Value.Node == IntPtr.Zero
                        ? null
                        : NodeValue(Marshal.PtrToStructure<NativeNode>(node.Value.Node), decoder);
                case MpvFormat.NodeArray:
                case MpvFormat.NodeMap:
                    if (node.Value.List == IntPtr.Zero)
                        return null;
                    var list = Marshal.PtrToStructure<NativeNodeList>(node.Value.List);
                    int ns = Marshal.SizeOf<NativeNode>();
                    if (node.Format == MpvFormat.NodeArray)
                    {
                        var a = new List<object?>(list.Count);
                        for (int i = 0; i < list.Count; i++)
                            a.Add(
                                NodeValue(
                                    Marshal.PtrToStructure<NativeNode>(
                                        IntPtr.Add(list.Values, i * ns)
                                    ),
                                    decoder
                                )
                            );
                        return a;
                    }
                    var d = new Dictionary<string, object?>(list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        string key = Utf8(Marshal.ReadIntPtr(list.Keys, i * IntPtr.Size)) ?? "";
                        d[key] = NodeValue(
                            Marshal.PtrToStructure<NativeNode>(IntPtr.Add(list.Values, i * ns)),
                            decoder
                        );
                    }
                    return d;
                case MpvFormat.ByteArray:
                    if (node.Value.ByteArray == IntPtr.Zero)
                        return null;
                    var ba = Marshal.PtrToStructure<NativeByteArray>(node.Value.ByteArray);
                    int len = checked((int)ba.Size.ToUInt64());
                    var bytes = new byte[len];
                    if (len != 0)
                        Marshal.Copy(ba.Data, bytes, 0, len);
                    return bytes;
                default:
                    throw new NotSupportedException("Unknown mpv node format " + node.Format);
            }
        }
    }

    internal sealed class UnmanagedArgs : IDisposable
    {
        private readonly List<IntPtr> allocated = new List<IntPtr>();
        public IntPtr Pointer { get; }

        public UnmanagedArgs(IEnumerable<object?> args)
        {
            var items = args.Where(x => x != null).Select(Coerce).ToList();
            Pointer = Marshal.AllocHGlobal((items.Count + 1) * IntPtr.Size);
            allocated.Add(Pointer);
            for (int i = 0; i < items.Count; i++)
            {
                var p = Marshal.AllocHGlobal(items[i].Length);
                allocated.Add(p);
                Marshal.Copy(items[i], 0, p, items[i].Length);
                Marshal.WriteIntPtr(Pointer, i * IntPtr.Size, p);
            }
            Marshal.WriteIntPtr(Pointer, items.Count * IntPtr.Size, IntPtr.Zero);
        }

        private static byte[] Coerce(object? o)
        {
            if (o is byte[] b)
                return b.Length > 0 && b[b.Length - 1] == 0
                    ? b
                    : b.Concat(new byte[] { 0 }).ToArray();
            return Native.Z(
                Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture) ?? ""
            );
        }

        public void Dispose()
        {
            foreach (var p in allocated.AsEnumerable().Reverse())
                Marshal.FreeHGlobal(p);
        }
    }

    internal sealed class NodeStringArray : IDisposable
    {
        private readonly List<IntPtr> memory = new List<IntPtr>();
        public NativeNode Node;

        public NodeStringArray(IEnumerable<object?> values)
        {
            var vals = values
                .Select(v =>
                    v is bool b
                        ? (b ? "yes" : "no")
                        : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)
                            ?? ""
                )
                .ToList();
            int nodeSize = Marshal.SizeOf<NativeNode>();
            IntPtr nodes = Alloc(nodeSize * vals.Count);
            IntPtr listPtr = Alloc(Marshal.SizeOf<NativeNodeList>());
            for (int i = 0; i < vals.Count; i++)
            {
                byte[] z = Native.Z(vals[i]);
                IntPtr s = Alloc(z.Length);
                Marshal.Copy(z, 0, s, z.Length);
                var n = new NativeNode
                {
                    Format = MpvFormat.String,
                    Value = new NativeNodeValue { String = s },
                };
                Marshal.StructureToPtr(n, IntPtr.Add(nodes, i * nodeSize), false);
            }
            Marshal.StructureToPtr(
                new NativeNodeList
                {
                    Count = vals.Count,
                    Values = nodes,
                    Keys = IntPtr.Zero,
                },
                listPtr,
                false
            );
            Node = new NativeNode
            {
                Format = MpvFormat.NodeArray,
                Value = new NativeNodeValue { List = listPtr },
            };
        }

        private IntPtr Alloc(int n)
        {
            var p = Marshal.AllocHGlobal(Math.Max(n, 1));
            memory.Add(p);
            return p;
        }

        public void Dispose()
        {
            foreach (var p in memory.AsEnumerable().Reverse())
                Marshal.FreeHGlobal(p);
        }
    }

    public interface IMpvStream : IDisposable
    {
        long? Size { get; }
        int Read(byte[] buffer, int offset, int count);
        long Seek(long offset);
        void Cancel();
    }

    public sealed class GeneratorStream : IMpvStream
    {
        private readonly Func<IEnumerable<byte[]>> generator;
        private IEnumerator<byte[]>? iterator;
        private byte[] chunk = Array.Empty<byte>();
        private int chunkOffset;
        public long? Size { get; }

        public GeneratorStream(Func<IEnumerable<byte[]>> generator, long? size = null)
        {
            this.generator = generator;
            Size = size;
            Seek(0);
        }

        public long Seek(long offset)
        {
            if (offset != 0)
                throw new NotSupportedException("GeneratorStream only supports seeking to zero");
            iterator?.Dispose();
            iterator = generator().GetEnumerator();
            chunk = Array.Empty<byte>();
            chunkOffset = 0;
            return 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (iterator == null)
                return 0;
            int written = 0;
            while (written < count)
            {
                if (chunkOffset >= chunk.Length)
                {
                    if (!iterator.MoveNext())
                        break;
                    chunk = iterator.Current ?? Array.Empty<byte>();
                    chunkOffset = 0;
                    if (chunk.Length == 0)
                        break;
                }
                int n = Math.Min(count - written, chunk.Length - chunkOffset);
                Buffer.BlockCopy(chunk, chunkOffset, buffer, offset + written, n);
                chunkOffset += n;
                written += n;
            }
            return written;
        }

        public void Cancel() => Dispose();

        public void Dispose()
        {
            iterator?.Dispose();
            iterator = null;
            chunk = Array.Empty<byte>();
        }
    }

    internal sealed class Registration : IDisposable
    {
        private Action? remove;

        public Registration(Action remove)
        {
            this.remove = remove;
        }

        public void Dispose() => Interlocked.Exchange(ref remove, null)?.Invoke();
    }

    public sealed class MpvRegistration<THandler> : IDisposable
        where THandler : Delegate
    {
        private IDisposable? registration;

        internal MpvRegistration(THandler handler, IDisposable registration)
        {
            Handler = handler;
            this.registration = registration;
        }

        public THandler Handler { get; }

        public void Unregister() => Dispose();

        public void Dispose() => Interlocked.Exchange(ref registration, null)?.Dispose();
    }

    public sealed class MpvWaitScope : IDisposable
    {
        private Action? finish;

        internal MpvWaitScope(Action finish)
        {
            this.finish = finish;
        }

        public void Dispose() => Interlocked.Exchange(ref finish, null)?.Invoke();
    }

    public sealed class PropertyProxy : DynamicObject
    {
        private readonly MPV mpv;
        private readonly Func<byte[], object> decoder;
        private readonly bool readOnly;

        internal PropertyProxy(MPV mpv, Func<byte[], object> decoder, bool readOnly = false)
        {
            this.mpv = mpv;
            this.decoder = decoder;
            this.readOnly = readOnly;
        }

        public object? this[string name]
        {
            get => mpv.GetProperty(name, decoder);
            set
            {
                if (readOnly)
                    throw new InvalidOperationException("OSD properties are read-only");
                mpv.SetProperty(name, value);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = mpv.GetProperty(binder.Name.Replace('_', '-'), decoder);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (readOnly)
                throw new InvalidOperationException("OSD properties are read-only");
            mpv.SetProperty(binder.Name.Replace('_', '-'), value);
            return true;
        }
    }

    public sealed class OsdPropertyProxy : DynamicObject
    {
        private readonly MPV mpv;

        internal OsdPropertyProxy(MPV mpv)
        {
            this.mpv = mpv;
        }

        public string? this[string name] => mpv.GetOsdProperty(name);

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = mpv.GetOsdProperty(binder.Name.Replace('_', '-'));
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value) =>
            throw new InvalidOperationException("OSD properties are read-only");
    }

    public sealed class FileLocalProxy : IEnumerable<string>
    {
        private readonly MPV mpv;

        internal FileLocalProxy(MPV mpv)
        {
            this.mpv = mpv;
        }

        public object? this[string name]
        {
            get => mpv.GetProperty("file-local-options/" + name, Native.LazyDecode);
            set => mpv.SetProperty("file-local-options/" + name, value);
        }

        public IEnumerator<string> GetEnumerator() => mpv.OptionNames.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class MpvNativeApi
    {
        public static (int Major, int Minor) ClientApiVersion
        {
            get
            {
                ulong version = Native.mpv_client_api_version();
                return ((int)(version >> 16), (int)(version & 0xffff));
            }
        }

        public static string ErrorString(int errorCode) =>
            Native.Utf8(Native.mpv_error_string(errorCode)) ?? "Unknown libmpv error";

        public static string EventName(MpvEventId eventId) =>
            Native.Utf8(Native.mpv_event_name(eventId)) ?? eventId.ToString();

        public static IntPtr Create()
        {
            IntPtr handle = Native.mpv_create();
            return handle != IntPtr.Zero
                ? handle
                : throw new OutOfMemoryException("mpv_create returned NULL");
        }

        public static IntPtr CreateClient(IntPtr handle, string name)
        {
            IntPtr client = Native.mpv_create_client(handle, Native.Z(name));
            return client != IntPtr.Zero
                ? client
                : throw new InvalidOperationException("mpv_create_client returned NULL");
        }

        public static string ClientName(IntPtr handle) =>
            Native.Utf8(Native.mpv_client_name(handle)) ?? "";

        public static void Initialize(IntPtr handle) =>
            MpvError.Throw(Native.mpv_initialize(handle));

        public static void Destroy(IntPtr handle) => Native.mpv_destroy(handle);

        public static void DetachDestroy(IntPtr handle)
        {
            try
            {
                Native.mpv_detach_destroy(handle);
            }
            catch (EntryPointNotFoundException)
            {
                Native.mpv_destroy(handle);
            }
        }

        public static void TerminateDestroy(IntPtr handle) => Native.mpv_terminate_destroy(handle);

        public static void LoadConfigFile(IntPtr handle, string filename) =>
            MpvError.Throw(Native.mpv_load_config_file(handle, Native.Z(filename)));

        public static long GetTimeMicroseconds(IntPtr handle) => Native.mpv_get_time_us(handle);

        public static void SetOptionString(IntPtr handle, string name, string value) =>
            MpvError.Throw(Native.mpv_set_option_string(handle, Native.Z(name), Native.Z(value)));

        public static void Command(IntPtr handle, string name, params object?[] arguments)
        {
            using var args = new UnmanagedArgs(new object?[] { name }.Concat(arguments));
            MpvError.Throw(Native.mpv_command(handle, args.Pointer));
        }

        public static void CommandString(IntPtr handle, string command) =>
            MpvError.Throw(Native.mpv_command_string(handle, Native.Z(command)));

        public static void CommandAsync(
            IntPtr handle,
            ulong replyUserData,
            string name,
            params object?[] arguments
        )
        {
            using var args = new UnmanagedArgs(new object?[] { name }.Concat(arguments));
            MpvError.Throw(Native.mpv_command_async(handle, replyUserData, args.Pointer));
        }

        public static void SetPropertyString(IntPtr handle, string name, string value) =>
            MpvError.Throw(Native.mpv_set_property_string(handle, Native.Z(name), Native.Z(value)));

        public static string? GetPropertyString(IntPtr handle, string name, bool osd = false)
        {
            IntPtr value = osd
                ? Native.mpv_get_property_osd_string(handle, Native.Z(name))
                : Native.mpv_get_property_string(handle, Native.Z(name));
            if (value == IntPtr.Zero)
                return null;
            try
            {
                return Native.Utf8(value);
            }
            finally
            {
                Native.mpv_free(value);
            }
        }

        public static void GetPropertyAsync(
            IntPtr handle,
            ulong replyUserData,
            string name,
            MpvFormat format = MpvFormat.Node
        ) =>
            MpvError.Throw(
                Native.mpv_get_property_async(handle, replyUserData, Native.Z(name), format)
            );

        public static void RequestEvent(IntPtr handle, MpvEventId eventId, bool enable = true) =>
            MpvError.Throw(Native.mpv_request_event(handle, eventId, enable ? 1 : 0));

        public static void RequestLogMessages(IntPtr handle, string level) =>
            MpvError.Throw(Native.mpv_request_log_messages(handle, Native.Z(level)));

        public static MpvEvent? WaitEvent(IntPtr handle, double timeout = -1)
        {
            IntPtr eventPointer = Native.mpv_wait_event(handle, timeout);
            if (eventPointer == IntPtr.Zero)
                return null;
            NativeEvent nativeEvent = Marshal.PtrToStructure<NativeEvent>(eventPointer);
            return nativeEvent.EventId == MpvEventId.None ? null : MPV.CopyEvent(nativeEvent);
        }

        public static void Wakeup(IntPtr handle) => Native.mpv_wakeup(handle);

        public static int GetWakeupPipe(IntPtr handle) => Native.mpv_get_wakeup_pipe(handle);

        public static IntPtr GetSubApi(IntPtr handle, MpvSubApi subApi) =>
            Native.mpv_get_sub_api(handle, (int)subApi);
    }

    public sealed class MPV : DynamicObject, IDisposable, IEnumerable<string>
    {
        private IntPtr handle,
            eventHandle;
        private Thread? eventThread;
        private volatile bool coreShutdown,
            disposed;
        private readonly object handlerLock = new object();
        private readonly object signatureLock = new object();

        /// <summary>The arguments mpv names for each of its commands, read from the library itself the
        /// first time it matters, or null until then.</summary>
        private Dictionary<string, string[]>? commandArguments;
        private readonly List<Action<MpvEvent>> eventCallbacks = new List<Action<MpvEvent>>();
        private readonly Dictionary<string, List<Action<string, object?>>> propertyHandlers =
            new Dictionary<string, List<Action<string, object?>>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ulong> propertyIds = new Dictionary<string, ulong>(
            StringComparer.Ordinal
        );
        private ulong nextPropertyId = 1;
        private readonly Dictionary<string, Action<string[]>> messageHandlers = new Dictionary<
            string,
            Action<string[]>
        >(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<string, string?, string?>> keyHandlers =
            new Dictionary<string, Action<string, string?, string?>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Native.StreamOpenCallback> protocolCallbacks =
            new Dictionary<string, Native.StreamOpenCallback>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<IntPtr, StreamState> openStreams =
            new ConcurrentDictionary<IntPtr, StreamState>();
        private readonly Dictionary<
            string,
            (Func<IEnumerable<byte[]>> Generator, long? Size)
        > pythonStreams = new Dictionary<string, (Func<IEnumerable<byte[]>>, long?)>(
            StringComparer.Ordinal
        );
        private Func<
            string,
            (Func<IEnumerable<byte[]>> Generator, long? Size)
        >? pythonStreamCatchall;
        private readonly HashSet<int> overlayIds = new HashSet<int>();
        private readonly Dictionary<int, IOverlay> overlays = new Dictionary<int, IOverlay>();
        private Action<string, string, object?>? logHandler;
        private Native.WakeupCallback? wakeupCallback;

        public PropertyProxy Raw { get; }
        public PropertyProxy Strict { get; }
        public PropertyProxy Lazy { get; }
        public OsdPropertyProxy Osd { get; }
        public FileLocalProxy FileLocal { get; }
        public bool CoreShutdown => coreShutdown;
        public IntPtr Handle
        {
            get
            {
                CheckCoreAlive();
                return handle;
            }
        }
        public string ClientName => Native.Utf8(Native.mpv_client_name(handle)) ?? "";
        public static (int Major, int Minor) ClientApiVersion
        {
            get
            {
                ulong v = Native.mpv_client_api_version();
                return ((int)(v >> 16), (int)(v & 0xffff));
            }
        }

        public MPV(
            IEnumerable<string>? flags = null,
            IDictionary<string, object?>? options = null,
            Action<string, string, object?>? logHandler = null,
            bool startEventThread = true,
            string? logLevel = null
        )
        {
            handle = Native.mpv_create();
            if (handle == IntPtr.Zero)
                throw new OutOfMemoryException("mpv_create returned NULL");
            try
            {
                MpvError.Throw(
                    Native.mpv_set_option_string(handle, Native.Z("audio-display"), Native.Z("no"))
                );
                if (flags != null)
                    foreach (string flag in flags)
                        MpvError.Throw(
                            Native.mpv_set_option_string(handle, Native.Z(flag), Native.Z(""))
                        );
                if (options != null)
                    foreach (var pair in options)
                    {
                        string value = pair.Value is bool b
                            ? (b ? "yes" : "no")
                            : Convert.ToString(
                                pair.Value,
                                System.Globalization.CultureInfo.InvariantCulture
                            ) ?? "";
                        MpvError.Throw(
                            Native.mpv_set_option_string(
                                handle,
                                Native.Z(pair.Key.Replace('_', '-')),
                                Native.Z(value)
                            )
                        );
                    }
            }
            finally
            {
                try
                {
                    MpvError.Throw(Native.mpv_initialize(handle));
                }
                catch
                {
                    Native.mpv_terminate_destroy(handle);
                    handle = IntPtr.Zero;
                    throw;
                }
            }

            Raw = new PropertyProxy(this, Native.RawDecode);
            Strict = new PropertyProxy(this, Native.StrictDecode);
            Lazy = new PropertyProxy(this, Native.LazyDecode);
            Osd = new OsdPropertyProxy(this);
            FileLocal = new FileLocalProxy(this);
            this.logHandler = logHandler;
            eventHandle = Native.mpv_create_client(handle, Native.Z("cs_event_handler"));
            if (eventHandle == IntPtr.Zero)
            {
                Native.mpv_terminate_destroy(handle);
                handle = IntPtr.Zero;
                throw new InvalidOperationException("mpv_create_client returned NULL");
            }
            RegisterStreamProtocol("python", OpenPythonStream);
            if (logLevel != null || logHandler != null)
                SetLogLevel(logLevel ?? "terminal-default");
            if (startEventThread)
            {
                eventThread = new Thread(EventLoop)
                {
                    IsBackground = true,
                    Name = "MPVEventHandlerThread",
                };
                eventThread.Start();
            }
        }

        public MPV(params string[] flags)
            : this(flags, null, null, true, null) { }

        ~MPV()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
            IntPtr h = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (h == IntPtr.Zero)
                return;
            if (Thread.CurrentThread == eventThread)
            {
                try
                {
                    CommandOn(h, "quit");
                }
                catch { }
                return;
            }
            Native.mpv_terminate_destroy(h);
            if (disposing && eventThread != null && eventThread.IsAlive)
                eventThread.Join();
            foreach (var stream in openStreams.Values)
                stream.Dispose();
            openStreams.Clear();
        }

        public void Terminate() => Dispose();

        private void EventLoop()
        {
            while (true)
            {
                IntPtr p = Native.mpv_wait_event(eventHandle, -1);
                if (p == IntPtr.Zero)
                    return;
                NativeEvent raw = Marshal.PtrToStructure<NativeEvent>(p);
                if (raw.EventId == MpvEventId.None)
                    continue;
                try
                {
                    MpvEvent ev = CopyEvent(raw);
                    Action<MpvEvent>[] callbacks;
                    lock (handlerLock)
                    {
                        if (ev.EventId == MpvEventId.Shutdown)
                            coreShutdown = true;
                        callbacks = eventCallbacks.ToArray();
                    }
                    foreach (var cb in callbacks)
                        Safe(() => cb(ev));
                    if (ev.EventId == MpvEventId.PropertyChange && ev.Data is MpvPropertyEvent prop)
                    {
                        Action<string, object?>[] hs;
                        lock (handlerLock)
                            hs = propertyHandlers.TryGetValue(prop.Name, out var list)
                                ? list.ToArray()
                                : Array.Empty<Action<string, object?>>();
                        foreach (var h in hs)
                            Safe(() => h(prop.Name, prop.Value));
                    }
                    else if (
                        ev.EventId == MpvEventId.LogMessage
                        && ev.Data is MpvLogMessage log
                        && logHandler != null
                    )
                        Safe(() => logHandler(log.Level, log.Prefix, log.Text));
                    else if (
                        ev.EventId == MpvEventId.ClientMessage
                        && ev.Data is MpvClientMessage cm
                        && cm.Arguments.Count > 0
                    )
                    {
                        Action<string[]>? mh;
                        lock (handlerLock)
                            messageHandlers.TryGetValue(cm.Arguments[0], out mh);
                        if (mh != null)
                            Safe(() => mh(cm.Arguments.Skip(1).ToArray()));
                    }
                    if (ev.EventId == MpvEventId.Shutdown)
                    {
                        try
                        {
                            Native.mpv_detach_destroy(eventHandle);
                        }
                        catch (EntryPointNotFoundException)
                        {
                            Native.mpv_destroy(eventHandle);
                        }
                        eventHandle = IntPtr.Zero;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception inside C# mpv event loop: " + ex);
                }
            }
        }

        private static void Safe(Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception inside C# mpv callback: " + ex);
            }
        }

        internal static MpvEvent CopyEvent(NativeEvent e)
        {
            object? data = null;
            if (e.Data != IntPtr.Zero)
                switch (e.EventId)
                {
                    case MpvEventId.PropertyChange:
                    case MpvEventId.GetPropertyReply:
                        var p = Marshal.PtrToStructure<NativeEventProperty>(e.Data);
                        object? value = null;
                        if (p.Data != IntPtr.Zero)
                        {
                            if (p.Format == MpvFormat.Node)
                                value = Native.NodeValue(
                                    Marshal.PtrToStructure<NativeNode>(p.Data),
                                    Native.LazyDecode
                                );
                            else if (
                                p.Format == MpvFormat.String
                                || p.Format == MpvFormat.OsdString
                            )
                                value = Native.Decode(
                                    p.Data,
                                    p.Format == MpvFormat.OsdString
                                        ? Native.StrictDecode
                                        : Native.LazyDecode
                                );
                            else if (p.Format == MpvFormat.Flag)
                                value = Marshal.ReadInt32(p.Data) != 0;
                            else if (p.Format == MpvFormat.Int64)
                                value = Marshal.ReadInt64(p.Data);
                            else if (p.Format == MpvFormat.Double)
                                value = Marshal.PtrToStructure<double>(p.Data);
                        }
                        data = new MpvPropertyEvent
                        {
                            Name = Native.Utf8(p.Name) ?? "",
                            Format = p.Format,
                            Value = value,
                        };
                        break;
                    case MpvEventId.LogMessage:
                        var l = Marshal.PtrToStructure<NativeEventLogMessage>(e.Data);
                        data = new MpvLogMessage
                        {
                            Prefix = Native.Utf8(l.Prefix) ?? "",
                            Level = Native.Utf8(l.Level) ?? "",
                            Text = Native.Decode(l.Text, Native.LazyDecode),
                        };
                        break;
                    case MpvEventId.EndFile:
                        var f = Marshal.PtrToStructure<NativeEventEndFile>(e.Data);
                        data = new MpvEndFileEvent
                        {
                            Reason = f.Reason,
                            Error = f.Error,
                            PlaylistEntryId = f.PlaylistEntryId,
                            PlaylistInsertId = f.PlaylistInsertId,
                            PlaylistInsertNumEntries = f.PlaylistInsertNumEntries,
                        };
                        break;
                    case MpvEventId.ClientMessage:
                        var c = Marshal.PtrToStructure<NativeEventClientMessage>(e.Data);
                        var args = new string[c.NumArgs];
                        for (int i = 0; i < args.Length; i++)
                            args[i] =
                                Native.Utf8(Marshal.ReadIntPtr(c.Args, i * IntPtr.Size)) ?? "";
                        data = new MpvClientMessage { Arguments = args };
                        break;
                    case MpvEventId.ScriptInputDispatch:
                        var input = Marshal.PtrToStructure<NativeEventScriptInputDispatch>(e.Data);
                        data = new MpvScriptInputDispatch
                        {
                            Argument0 = input.Argument0,
                            Type = Native.Utf8(input.Type) ?? "",
                        };
                        break;
                    case MpvEventId.CommandReply:
                        data = Native.NodeValue(
                            Marshal.PtrToStructure<NativeNode>(e.Data),
                            Native.LazyDecode
                        );
                        break;
                }
            return new MpvEvent
            {
                EventId = e.EventId,
                Error = e.Error,
                ReplyUserData = e.ReplyUserData,
                Data = data,
            };
        }

        public void CheckCoreAlive()
        {
            if (coreShutdown || handle == IntPtr.Zero)
                throw new MpvShutdownException("libmpv core has been shut down");
        }

        public void SetLogLevel(string level)
        {
            CheckCoreAlive();
            MpvError.Throw(Native.mpv_request_log_messages(eventHandle, Native.Z(level)));
        }

        public void LoadConfigFile(string file)
        {
            CheckCoreAlive();
            MpvError.Throw(Native.mpv_load_config_file(handle, Native.Z(file)));
        }

        public long TimeMicroseconds
        {
            get
            {
                CheckCoreAlive();
                return Native.mpv_get_time_us(handle);
            }
        }
        public int WakeupPipe
        {
            get
            {
                CheckCoreAlive();
                return Native.mpv_get_wakeup_pipe(handle);
            }
        }

        public string GetEventName(MpvEventId eventId) => MpvEventIds.GetName(eventId);

        public string GetErrorString(int errorCode) =>
            Native.Utf8(Native.mpv_error_string(errorCode)) ?? "Unknown libmpv error";

        public void RequestEvent(MpvEventId eventId, bool enable = true)
        {
            CheckCoreAlive();
            MpvError.Throw(Native.mpv_request_event(handle, eventId, enable ? 1 : 0));
        }

        public void Wakeup()
        {
            CheckCoreAlive();
            Native.mpv_wakeup(handle);
        }

        public IDisposable SetWakeupCallback(Action callback)
        {
            CheckCoreAlive();
            Native.WakeupCallback wrapper = _ => callback();
            lock (handlerLock)
            {
                wakeupCallback = wrapper;
                Native.mpv_set_wakeup_callback(handle, wrapper, IntPtr.Zero);
            }
            return new Registration(() =>
            {
                lock (handlerLock)
                {
                    if (wakeupCallback == wrapper)
                    {
                        Native.WakeupCallback noop = _ => { };
                        wakeupCallback = noop;
                        Native.mpv_set_wakeup_callback(handle, noop, IntPtr.Zero);
                    }
                }
            });
        }

        public MpvEvent? WaitEvent(double timeout = 0)
        {
            CheckCoreAlive();
            IntPtr p = Native.mpv_wait_event(handle, timeout);
            if (p == IntPtr.Zero)
                return null;
            var raw = Marshal.PtrToStructure<NativeEvent>(p);
            return raw.EventId == MpvEventId.None ? null : CopyEvent(raw);
        }

        public void Command(string name, params object?[] args)
        {
            CheckCoreAlive();
            CommandOn(handle, name, args);
        }

        private static void CommandOn(IntPtr h, string name, params object?[] args)
        {
            using var a = new UnmanagedArgs(new object?[] { name }.Concat(args));
            MpvError.Throw(Native.mpv_command(h, a.Pointer));
        }

        public void CommandString(string command)
        {
            CheckCoreAlive();
            MpvError.Throw(Native.mpv_command_string(handle, Native.Z(command)));
        }

        public void CommandAsync(ulong replyUserData, string name, params object?[] args)
        {
            CheckCoreAlive();
            using var a = new UnmanagedArgs(new object?[] { name }.Concat(args));
            MpvError.Throw(Native.mpv_command_async(handle, replyUserData, a.Pointer));
        }

        public object? NodeCommand(string name, params object?[] args)
        {
            CheckCoreAlive();
            using var input = new NodeStringArray(new object?[] { name }.Concat(args));
            NativeNode output;
            MpvError.Throw(Native.mpv_command_node(handle, ref input.Node, out output));
            try
            {
                return Native.NodeValue(output);
            }
            finally
            {
                Native.mpv_free_node_contents(ref output);
            }
        }

        public void NodeCommandAsync(ulong replyUserData, string name, params object?[] args)
        {
            CheckCoreAlive();
            using var input = new NodeStringArray(new object?[] { name }.Concat(args));
            MpvError.Throw(Native.mpv_command_node_async(handle, replyUserData, ref input.Node));
        }

        public void GetPropertyAsync(
            ulong replyUserData,
            string name,
            MpvFormat format = MpvFormat.Node
        )
        {
            CheckCoreAlive();
            MpvError.Throw(
                Native.mpv_get_property_async(handle, replyUserData, Native.Z(name), format)
            );
        }

        public void SetPropertyAsync(ulong replyUserData, string name, object? value)
        {
            CheckCoreAlive();
            if (value is IEnumerable && value is not string && value is not byte[])
            {
                IEnumerable<object?> vals = value is IDictionary d
                    ? d.Keys.Cast<object?>()
                    : ((IEnumerable)value).Cast<object?>();
                using var n = new NodeStringArray(vals);
                IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf<NativeNode>());
                try
                {
                    Marshal.StructureToPtr(n.Node, p, false);
                    MpvError.Throw(
                        Native.mpv_set_property_async(
                            handle,
                            replyUserData,
                            Native.Z(name),
                            MpvFormat.Node,
                            p
                        )
                    );
                }
                finally
                {
                    Marshal.FreeHGlobal(p);
                }
            }
            else
            {
                string s = value is bool b
                    ? (b ? "yes" : "no")
                    : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                        ?? "";
                byte[] z = Native.Z(s);
                IntPtr text = Marshal.AllocHGlobal(z.Length);
                IntPtr pointer = Marshal.AllocHGlobal(IntPtr.Size);
                try
                {
                    Marshal.Copy(z, 0, text, z.Length);
                    Marshal.WriteIntPtr(pointer, text);
                    MpvError.Throw(
                        Native.mpv_set_property_async(
                            handle,
                            replyUserData,
                            Native.Z(name),
                            MpvFormat.String,
                            pointer
                        )
                    );
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                    Marshal.FreeHGlobal(text);
                }
            }
        }

        public string? GetPropertyString(string name) => GetAllocatedPropertyString(name, false);

        public string? GetPropertyOsdString(string name) => GetAllocatedPropertyString(name, true);

        private string? GetAllocatedPropertyString(string name, bool osd)
        {
            CheckCoreAlive();
            IntPtr p = osd
                ? Native.mpv_get_property_osd_string(handle, Native.Z(name))
                : Native.mpv_get_property_string(handle, Native.Z(name));
            if (p == IntPtr.Zero)
                return null;
            try
            {
                return Native.Utf8(p);
            }
            finally
            {
                Native.mpv_free(p);
            }
        }

        public object? GetProperty(string name) => GetProperty(name, Native.StrictDecode);

        public object? GetProperty(string name, Func<byte[], object> decoder)
        {
            CheckCoreAlive();
            IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf<NativeNode>());
            try
            {
                int ec = Native.mpv_get_property(handle, Native.Z(name), MpvFormat.Node, p);
                if (ec == MpvError.PropertyUnavailable)
                    return null;
                MpvError.Throw(ec);
                var n = Marshal.PtrToStructure<NativeNode>(p);
                try
                {
                    return Native.NodeValue(n, decoder);
                }
                finally
                {
                    Native.mpv_free_node_contents(ref n);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public string? GetOsdProperty(string name)
        {
            CheckCoreAlive();
            IntPtr p = Marshal.AllocHGlobal(IntPtr.Size);
            try
            {
                int ec = Native.mpv_get_property(handle, Native.Z(name), MpvFormat.OsdString, p);
                if (ec == MpvError.PropertyUnavailable)
                    return null;
                MpvError.Throw(ec);
                IntPtr value = Marshal.ReadIntPtr(p);
                try
                {
                    return Native.Utf8(value);
                }
                finally
                {
                    if (value != IntPtr.Zero)
                        Native.mpv_free(value);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public void SetProperty(string name, object? value)
        {
            CheckCoreAlive();
            if (value is IEnumerable && value is not string && value is not byte[])
            {
                IEnumerable<object?> vals = value is IDictionary dict
                    ? dict.Keys.Cast<object?>()
                    : ((IEnumerable)value).Cast<object?>();
                using var n = new NodeStringArray(vals);
                IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf<NativeNode>());
                try
                {
                    Marshal.StructureToPtr(n.Node, p, false);
                    MpvError.Throw(
                        Native.mpv_set_property(handle, Native.Z(name), MpvFormat.Node, p)
                    );
                }
                finally
                {
                    Marshal.FreeHGlobal(p);
                }
            }
            else
            {
                string s = value is bool b
                    ? (b ? "yes" : "no")
                    : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                        ?? "";
                MpvError.Throw(Native.mpv_set_property_string(handle, Native.Z(name), Native.Z(s)));
            }
        }

        public object? this[string option]
        {
            get => GetProperty("options/" + option, Native.LazyDecode);
            set => SetProperty("options/" + option, value);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = GetProperty(binder.Name.Replace('_', '-'), Native.LazyDecode);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            SetProperty(binder.Name.Replace('_', '-'), value);
            return true;
        }

        public IReadOnlyList<string> PropertyNames =>
            (GetProperty("property-list", Native.StrictDecode) as IEnumerable<object?>)
                ?.Select(x => Convert.ToString(x) ?? "")
                .ToList()
            ?? new List<string>();
        public IReadOnlyList<string> OptionNames =>
            (
                GetProperty("options", Native.StrictDecode) as IDictionary<string, object?>
            )?.Keys.ToList() ?? new List<string>();
        public IReadOnlyDictionary<string, object?> Properties =>
            PropertyNames.ToDictionary(x => x, x => OptionInfo(x), StringComparer.Ordinal);

        public object? OptionInfo(string name)
        {
            try
            {
                return GetProperty("option-info/" + name);
            }
            catch (MpvException ex) when (ex.ErrorCode == MpvError.PropertyNotFound)
            {
                return null;
            }
        }

        public IEnumerator<string> GetEnumerator() => OptionNames.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IDisposable RegisterEventCallback(Action<MpvEvent> callback)
        {
            lock (handlerLock)
            {
                CheckCoreAlive();
                eventCallbacks.Add(callback);
            }
            return new Registration(() => UnregisterEventCallback(callback));
        }

        public IDisposable OnEvent(Action<MpvEvent> callback, params MpvEventId[] types)
        {
            var wanted = types.Length == 0 ? null : new HashSet<MpvEventId>(types);
            Action<MpvEvent> wrapper = e =>
            {
                if (wanted == null || wanted.Contains(e.EventId))
                    callback(e);
            };
            return RegisterEventCallback(wrapper);
        }

        public void UnregisterEventCallback(Action<MpvEvent> callback)
        {
            lock (handlerLock)
                eventCallbacks.Remove(callback);
        }

        public Func<Action<MpvEvent>, MpvRegistration<Action<MpvEvent>>> EventCallback(
            params MpvEventId[] types
        )
        {
            return callback => new MpvRegistration<Action<MpvEvent>>(
                callback,
                OnEvent(callback, types)
            );
        }

        public Func<Action<MpvEvent>, MpvRegistration<Action<MpvEvent>>> EventCallback(
            params string[] types
        )
        {
            return EventCallback(types.Select(MpvEventIds.FromString).ToArray());
        }

        public Func<Action<MpvEvent>, MpvRegistration<Action<MpvEvent>>> EventCallback()
        {
            return EventCallback(Array.Empty<MpvEventId>());
        }

        public IDisposable ObserveProperty(string name, Action<string, object?> handler)
        {
            lock (handlerLock)
            {
                CheckCoreAlive();
                if (!propertyHandlers.TryGetValue(name, out var list))
                {
                    list = new List<Action<string, object?>>();
                    propertyHandlers[name] = list;
                    ulong id = nextPropertyId++;
                    propertyIds[name] = id;
                    MpvError.Throw(
                        Native.mpv_observe_property(eventHandle, id, Native.Z(name), MpvFormat.Node)
                    );
                }
                list.Add(handler);
            }
            return new Registration(() => UnobserveProperty(name, handler));
        }

        public Func<
            Action<string, object?>,
            MpvRegistration<Action<string, object?>>
        > PropertyObserver(string name)
        {
            return handler => new MpvRegistration<Action<string, object?>>(
                handler,
                ObserveProperty(name, handler)
            );
        }

        public void UnobserveProperty(string name, Action<string, object?> handler)
        {
            lock (handlerLock)
            {
                if (!propertyHandlers.TryGetValue(name, out var list) || !list.Remove(handler))
                    return;
                if (list.Count == 0)
                {
                    propertyHandlers.Remove(name);
                    if (propertyIds.Remove(name, out ulong id) && eventHandle != IntPtr.Zero)
                        MpvError.Throw(Native.mpv_unobserve_property(eventHandle, id));
                }
            }
        }

        public void UnobserveAllProperties(Action<string, object?> handler)
        {
            string[] names;
            lock (handlerLock)
                names = propertyHandlers.Keys.ToArray();
            foreach (string n in names)
                UnobserveProperty(n, handler);
        }

        public IDisposable RegisterMessageHandler(string target, Action<string[]> handler)
        {
            lock (handlerLock)
                messageHandlers[target] = handler;
            return new Registration(() => UnregisterMessageHandler(target));
        }

        public Func<Action<string[]>, MpvRegistration<Action<string[]>>> RegisterMessageHandler(
            string target
        )
        {
            return MessageHandler(target);
        }

        public void UnregisterMessageHandler(string target)
        {
            lock (handlerLock)
                messageHandlers.Remove(target);
        }

        public void UnregisterMessageHandler(Action<string[]> handler)
        {
            lock (handlerLock)
            {
                foreach (
                    string key in messageHandlers
                        .Where(x => x.Value == handler)
                        .Select(x => x.Key)
                        .ToArray()
                )
                    messageHandlers.Remove(key);
            }
        }

        public Func<Action<string[]>, MpvRegistration<Action<string[]>>> MessageHandler(
            string target
        )
        {
            return handler => new MpvRegistration<Action<string[]>>(
                handler,
                RegisterMessageHandler(target, handler)
            );
        }

        public void WaitForEvent(params MpvEventId[] types) => WaitForEvent(_ => true, types);

        public void WaitForEvent(Func<MpvEvent, bool> condition, params MpvEventId[] types)
        {
            using (PrepareAndWaitForEvent(condition, types)) { }
        }

        public MpvWaitScope PrepareAndWaitForEvent(params MpvEventId[] types) =>
            PrepareAndWaitForEvent(_ => true, types);

        public MpvWaitScope PrepareAndWaitForEvent(
            Func<MpvEvent, bool> condition,
            params MpvEventId[] types
        )
        {
            var signal = new ManualResetEventSlim();
            var shut = OnEvent(_ => signal.Set(), MpvEventId.Shutdown);
            var target = OnEvent(
                e =>
                {
                    if (condition(e))
                        signal.Set();
                },
                types
            );
            return new MpvWaitScope(() =>
            {
                try
                {
                    signal.Wait();
                    CheckCoreAlive();
                }
                finally
                {
                    target.Dispose();
                    shut.Dispose();
                    signal.Dispose();
                }
            });
        }

        public void WaitForProperty(
            string name,
            Func<object?, bool>? condition = null,
            bool levelSensitive = true
        )
        {
            using (PrepareAndWaitForProperty(name, condition, levelSensitive)) { }
        }

        public MpvWaitScope PrepareAndWaitForProperty(
            string name,
            Func<object?, bool>? condition = null,
            bool levelSensitive = true
        )
        {
            condition ??= (v => v is bool b ? b : v != null);
            var test = condition;
            var signal = new ManualResetEventSlim();
            var obs = ObserveProperty(
                name,
                (_, v) =>
                {
                    if (test(v))
                        signal.Set();
                }
            );
            var shut = OnEvent(_ => signal.Set(), MpvEventId.Shutdown);
            return new MpvWaitScope(() =>
            {
                try
                {
                    if (!levelSensitive || !test(GetProperty(name)))
                        signal.Wait();
                    CheckCoreAlive();
                }
                finally
                {
                    shut.Dispose();
                    obs.Dispose();
                    signal.Dispose();
                }
            });
        }

        public void WaitUntilPaused() => WaitForProperty("core-idle");

        public void WaitUntilPlaying() => WaitForProperty("core-idle", v => v is bool b && !b);

        public void WaitForPlayback() => WaitForEvent(MpvEventId.EndFile);

        public void WaitForShutdown()
        {
            if (coreShutdown)
                return;
            using var signal = new ManualResetEventSlim();
            using var reg = OnEvent(_ => signal.Set(), MpvEventId.Shutdown);
            signal.Wait();
        }

        public void Seek(
            object amount,
            string reference = "relative",
            string precision = "default-precise"
        ) => Command("seek", amount, reference, precision);

        public void RevertSeek() => Command("revert-seek");

        public void FrameStep()
        {
            // One frame is what this has always meant, and is the default mpv states for the argument.
            if (CommandTakes("frame-step", "frames"))
                Command("frame-step", 1);
            else
                Command("frame-step");
        }

        public void FrameBackStep() => Command("frame-back-step");

        public void PropertyAdd(string name, object value) => Command("add", name, value);

        public void PropertyAdd(string name) => PropertyAdd(name, 1);

        public void PropertyMultiply(string name, object factor) =>
            Command("multiply", name, factor);

        public void Cycle(string name, string direction = "up") =>
            Command("cycle", name, direction);

        public void Screenshot(string includes = "subtitles", string mode = "single") =>
            Command("screenshot", includes, mode);

        public void ScreenshotToFile(string filename, string includes = "subtitles") =>
            Command("screenshot-to-file", filename, includes);

        public MpvImage ScreenshotRaw(string includes = "subtitles")
        {
            // The pixel format became an argument of its own; bgr0 is mpv's default for it and the only
            // one the decoding below understands, so it is asked for by name rather than left to chance.
            var r =
                (
                    CommandTakes("screenshot-raw", "format")
                        ? NodeCommand("screenshot-raw", includes, "bgr0")
                        : NodeCommand("screenshot-raw", includes)
                ) as IDictionary<string, object?>
                ?? throw new InvalidDataException("Invalid screenshot result");
            string fmt = Convert.ToString(r["format"]) ?? "";
            if (fmt != "bgr0")
                throw new NotSupportedException("Unknown screenshot format " + fmt);
            int stride = Convert.ToInt32(r["stride"]),
                h = Convert.ToInt32(r["h"]);
            byte[] src = (byte[])r["data"]!;
            byte[] rgb = new byte[(stride / 4) * h * 3];
            for (int i = 0, j = 0; i < src.Length && j + 2 < rgb.Length; i += 4)
            {
                rgb[j++] = src[i + 2];
                rgb[j++] = src[i + 1];
                rgb[j++] = src[i];
            }
            return new MpvImage(stride / 4, h, (stride / 4) * 3, rgb, "rgb24");
        }

        public int AllocateOverlayId()
        {
            lock (overlayIds)
            {
                for (int i = 0; i < 64; i++)
                    if (overlayIds.Add(i))
                        return i;
            }
            throw new InvalidOperationException("All overlay IDs are in use");
        }

        public void FreeOverlayId(int id)
        {
            lock (overlayIds)
            {
                if (!overlayIds.Remove(id))
                    throw new KeyNotFoundException($"Overlay ID {id} is not allocated.");
            }
        }

        public void RemoveOverlay(int id)
        {
            OverlayRemove(id);
            lock (overlayIds)
            {
                overlayIds.Remove(id);
                overlays.Remove(id);
            }
        }

        public FileOverlay CreateFileOverlay(
            string? filename = null,
            int width = 0,
            int height = 0,
            int? stride = null,
            int x = 0,
            int y = 0
        )
        {
            int id = AllocateOverlayId();
            var o = new FileOverlay(this, id, filename, width, height, stride, x, y);
            overlays[id] = o;
            return o;
        }

        public ImageOverlay CreateImageOverlay(
            byte[]? bgra = null,
            int width = 0,
            int height = 0,
            int x = 0,
            int y = 0
        )
        {
            int id = AllocateOverlayId();
            var o = new ImageOverlay(this, id, bgra, width, height, x, y);
            overlays[id] = o;
            return o;
        }

        public ImageOverlay CreateImageOverlay(MpvImage image, int x = 0, int y = 0)
        {
            int id = AllocateOverlayId();
            var o = new ImageOverlay(this, id, image, x, y);
            overlays[id] = o;
            return o;
        }

        public void OverlayAdd(
            int id,
            int x,
            int y,
            object fileOrFd,
            long offset,
            string format,
            int width,
            int height,
            int stride
        )
        {
            // Newer mpv takes a display size and a colour description as well. Every one of the added
            // arguments is passed the default mpv itself states for it, so an overlay added through this
            // is placed exactly as it was before they existed.
            if (CommandTakes("overlay-add", "dw"))
                Command(
                    "overlay-add", id, x, y, fileOrFd, offset, format, width, height, stride,
                    0, 0, false, 0.0, "auto", "auto"
                );
            else
                Command("overlay-add", id, x, y, fileOrFd, offset, format, width, height, stride);
        }

        public void OverlayRemove(int id) => Command("overlay-remove", id);

        public void PlaylistNext(string mode = "weak") => Command("playlist-next", mode);

        public void PlaylistPrev(string mode = "weak") => Command("playlist-prev", mode);

        public void PlaylistPlayIndex(int index) => Command("playlist-play-index", index);

        private static string EncodeOptions(IDictionary<string, object?>? options) =>
            options == null
                ? ""
                : string.Join(",", options.Select(x => $"{x.Key.Replace('_', '-')}={x.Value}"));

        /// <summary>Whether the mpv this is running against declares <paramref name="argument"/> as part
        /// of <paramref name="command"/>.</summary>
        ///
        /// <remarks>
        /// mpv has added required arguments to commands over the years - loadfile and loadlist take an
        /// insertion index, frame-step a frame count, keypress a scale, screenshot-raw a pixel format - and
        /// a call written for the older form is rejected outright rather than being defaulted. The two
        /// forms are mutually exclusive: one library takes the extra argument and refuses the call without
        /// it, the other refuses the call with it.
        ///
        /// Which to send cannot be settled from the client API version, because that version tracks the C
        /// API and says nothing about the commands; a build made between two releases carries whichever
        /// set its source had while still reporting the older version number.
        ///
        /// So mpv is asked instead. Its command-list property describes every command it accepts and names
        /// every argument, which is exact for whatever library is actually loaded - including one a user
        /// substituted for the one shipped, which the library's licence entitles them to do.
        /// </remarks>
        private bool CommandTakes(string command, string argument)
        {
            lock (signatureLock)
            {
                commandArguments ??= ReadCommandArguments();
                return commandArguments.TryGetValue(command, out string[]? names)
                    && Array.IndexOf(names, argument) >= 0;
            }
        }

        private Dictionary<string, string[]> ReadCommandArguments()
        {
            var found = new Dictionary<string, string[]>(StringComparer.Ordinal);
            try
            {
                if (GetProperty("command-list") is not IEnumerable<object?> commands)
                    return found;
                foreach (var entry in commands.OfType<IDictionary<string, object?>>())
                {
                    if (!entry.TryGetValue("name", out object? name))
                        continue;
                    string command = Convert.ToString(name) ?? "";
                    if (command.Length == 0)
                        continue;
                    found[command] =
                        entry.TryGetValue("args", out object? args) && args is IEnumerable<object?> list
                            ? list.OfType<IDictionary<string, object?>>()
                                .Select(argument =>
                                    argument.TryGetValue("name", out object? argumentName)
                                        ? Convert.ToString(argumentName) ?? ""
                                        : ""
                                )
                                .ToArray()
                            : Array.Empty<string>();
                }
            }
            catch (MpvException)
            {
                // An mpv too old to describe itself is old enough to want the older call shapes, which is
                // what an empty map produces.
            }
            return found;
        }

        public void LoadFile(
            string filename,
            string mode = "replace",
            IDictionary<string, object?>? options = null
        )
        {
            // The insertion index goes between the flags and the options. -1 is the default mpv states for
            // it, and means the end of the playlist.
            if (CommandTakes("loadfile", "index"))
                Command("loadfile", filename, mode, -1, EncodeOptions(options));
            else
                Command("loadfile", filename, mode, EncodeOptions(options));
        }

        public void LoadList(string playlist, string mode = "replace")
        {
            if (CommandTakes("loadlist", "index"))
                Command("loadlist", playlist, mode, -1);
            else
                Command("loadlist", playlist, mode);
        }

        public void PlaylistClear() => Command("playlist-clear");

        public void PlaylistRemove(object? index = null) =>
            Command("playlist-remove", index ?? "current");

        public void PlaylistMove(int from, int to) => Command("playlist-move", from, to);

        public void PlaylistShuffle() => Command("playlist-shuffle");

        public void PlaylistUnshuffle() => Command("playlist-unshuffle");

        public void Run(string command, params object?[] args) =>
            Command(
                "run",
                new object?[] { command }
                    .Concat(args)
                    .ToArray()
            );

        public void Quit(int? code = null) => Command("quit", code);

        public void QuitWatchLater(int? code = null) => Command("quit-watch-later", code);

        public void Stop(bool keepPlaylist = false) =>
            Command("stop", keepPlaylist ? "keep-playlist" : null);

        public void AudioAdd(
            string url,
            string flags = "select",
            string? title = null,
            string? language = null
        ) => Command("audio-add", url, flags, title, language);

        public void AudioRemove(object? id = null) => Command("audio-remove", id);

        public void AudioReload(object? id = null) => Command("audio-reload", id);

        public void VideoAdd(
            string url,
            string flags = "select",
            string? title = null,
            string? language = null
        ) => Command("video-add", url, flags, title, language);

        public void VideoRemove(object? id = null) => Command("video-remove", id);

        public void VideoReload(object? id = null) => Command("video-reload", id);

        public void SubAdd(
            string url,
            string flags = "select",
            string? title = null,
            string? language = null
        ) => Command("sub-add", url, flags, title, language);

        public void SubRemove(object? id = null) => Command("sub-remove", id);

        public void SubReload(object? id = null) => Command("sub-reload", id);

        public void SubStep(object skip) => Command("sub-step", skip);

        public void SubSeek(object skip) => Command("sub-seek", skip);

        public void ToggleOsd() => Command("osd");

        public void PrintText(string text) => Command("print-text", text);

        public void ShowText(string text, object? duration = null, object? level = null) =>
            Command("show-text", text, duration ?? "-1", level);

        public object? ExpandText(string text) => NodeCommand("expand-text", text);

        public object? ExpandPath(string path) => NodeCommand("expand-path", path);

        public void ShowProgress() => Command("show-progress");

        public void RescanExternalFiles(string mode = "reselect") =>
            Command("rescan-external-files", mode);

        public void DiscNav(string command) => Command("discnav", command);

        public void Mouse(int x, int y, object? button = null, string mode = "single") =>
            Command("mouse", x, y, button, mode);

        public void KeyPress(string name)
        {
            // The scale is how far a press counts for a binding that reads one, such as a scroll. 1 is a
            // single ordinary press, and the default mpv states.
            if (CommandTakes("keypress", "scale"))
                Command("keypress", name, 1.0);
            else
                Command("keypress", name);
        }

        public void KeyDown(string name) => Command("keydown", name);

        public void KeyUp(string? name = null) => Command("keyup", name);

        public void KeyBind(string name, string command) => Command("keybind", name, command);

        public void WriteWatchLaterConfig() => Command("write-watch-later-config");

        public void ScriptMessage(params object?[] args) => Command("script-message", args);

        public void ScriptMessageTo(string target, params object?[] args) =>
            Command(
                "script-message-to",
                new object?[] { target }
                    .Concat(args)
                    .ToArray()
            );

        public void Play(string filename) => LoadFile(filename);

        public void PlaylistAppend(string filename, IDictionary<string, object?>? options = null) =>
            LoadFile(filename, "append", options);

        public IReadOnlyList<string> PlaylistFilenames =>
            (GetProperty("playlist") as IEnumerable<object?>)
                ?.OfType<IDictionary<string, object?>>()
                .Select(x => Convert.ToString(x["filename"]) ?? "")
                .ToList()
            ?? new List<string>();

        private static string BindingName(string key) => "cs_kb_" + StableHash(key).ToString("x16");

        private static ulong StableHash(string s)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                foreach (byte b in Encoding.UTF8.GetBytes(s))
                {
                    h ^= b;
                    h *= 1099511628211UL;
                }
                return h;
            }
        }

        public IDisposable RegisterKeyBinding(
            string key,
            Action<string, string?, string?> callback,
            string mode = "force"
        )
        {
            if (!Regex.IsMatch(key, @"^(Shift\+)?(Ctrl\+)?(Alt\+)?(Meta\+)?(.|\w+)$"))
                throw new ArgumentException("Invalid key definition", nameof(key));
            string name = BindingName(key);
            lock (handlerLock)
            {
                keyHandlers[name] = callback;
                if (!messageHandlers.ContainsKey("key-binding"))
                    messageHandlers["key-binding"] = HandleKeyMessage;
            }
            Command("define-section", name, $"{key} script-binding cs_event_handler/{name}", mode);
            Command("enable-section", name, "allow-hide-cursor+allow-vo-dragging");
            return new Registration(() => UnregisterKeyBinding(key));
        }

        public IDisposable RegisterKeyBinding(string key, string command, string mode = "force")
        {
            string name = BindingName(key);
            Command("define-section", name, $"{key} {command}", mode);
            Command("enable-section", name, "allow-hide-cursor+allow-vo-dragging");
            return new Registration(() => UnregisterKeyBinding(key));
        }

        public IDisposable OnKeyPress(string key, Action callback, string mode = "force") =>
            RegisterKeyBinding(
                key,
                (state, _, __) =>
                {
                    if (state.Length > 0 && (state[0] == 'd' || state[0] == 'p'))
                        callback();
                },
                mode
            );

        public Func<Action, MpvRegistration<Action>> OnKeyPress(string key, string mode = "force")
        {
            return callback => new MpvRegistration<Action>(
                callback,
                OnKeyPress(key, callback, mode)
            );
        }

        public Func<
            Action<string, string?, string?>,
            MpvRegistration<Action<string, string?, string?>>
        > KeyBinding(string key, string mode = "force")
        {
            return callback => new MpvRegistration<Action<string, string?, string?>>(
                callback,
                RegisterKeyBinding(key, callback, mode)
            );
        }

        private void HandleKeyMessage(string[] args)
        {
            if (args.Length < 2)
                return;
            Action<string, string?, string?>? h;
            lock (handlerLock)
                keyHandlers.TryGetValue(args[0], out h);
            h?.Invoke(args[1], args.Length > 2 ? args[2] : null, args.Length > 3 ? args[3] : null);
        }

        public void UnregisterKeyBinding(string key)
        {
            string name = BindingName(key);
            Command("disable-section", name);
            Command("define-section", name, "");
            lock (handlerLock)
            {
                keyHandlers.Remove(name);
                if (keyHandlers.Count == 0)
                    messageHandlers.Remove("key-binding");
            }
        }

        private sealed class StreamState : IDisposable
        {
            public readonly MPV Owner;
            public readonly IMpvStream Stream;
            public readonly GCHandle GcHandle;
            public readonly Native.StreamReadCallback Read;
            public readonly Native.StreamSeekCallback Seek;
            public readonly Native.StreamSizeCallback Size;
            public readonly Native.StreamCloseCallback Close;
            public readonly Native.StreamCancelCallback Cancel;
            private int closed;
            public IntPtr Cookie => GCHandle.ToIntPtr(GcHandle);

            public StreamState(MPV owner, IMpvStream stream)
            {
                Owner = owner;
                Stream = stream;
                GcHandle = GCHandle.Alloc(this);
                Read = ReadImpl;
                Seek = SeekImpl;
                Size = SizeImpl;
                Close = _ => Dispose();
                Cancel = _ => stream.Cancel();
            }

            private long ReadImpl(IntPtr _, IntPtr buffer, ulong count)
            {
                try
                {
                    int n = checked((int)Math.Min(count, int.MaxValue));
                    var b = new byte[n];
                    int got = Stream.Read(b, 0, n);
                    if (got > 0)
                        Marshal.Copy(b, 0, buffer, got);
                    return got;
                }
                catch
                {
                    return -1;
                }
            }

            private long SeekImpl(IntPtr _, long offset)
            {
                try
                {
                    return Stream.Seek(offset);
                }
                catch
                {
                    return -1;
                }
            }

            private long SizeImpl(IntPtr _) => Stream.Size ?? -1;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref closed, 1) != 0)
                    return;
                Owner.openStreams.TryRemove(Cookie, out _);
                try
                {
                    Stream.Dispose();
                }
                finally
                {
                    GcHandle.Free();
                }
            }
        }

        public void RegisterStreamProtocol(string protocol, Func<string, IMpvStream> open)
        {
            lock (handlerLock)
            {
                if (protocolCallbacks.ContainsKey(protocol))
                    throw new InvalidOperationException("Stream protocol already registered");
                Native.StreamOpenCallback callback = (_, uri, infoPtr) =>
                {
                    try
                    {
                        var state = new StreamState(this, open(Native.Utf8(uri) ?? ""));
                        openStreams[state.Cookie] = state;
                        var info = new NativeStreamCallbackInfo
                        {
                            Cookie = state.Cookie,
                            Read = Marshal.GetFunctionPointerForDelegate(state.Read),
                            Seek = Marshal.GetFunctionPointerForDelegate(state.Seek),
                            Size = state.Stream.Size.HasValue
                                ? Marshal.GetFunctionPointerForDelegate(state.Size)
                                : IntPtr.Zero,
                            Close = Marshal.GetFunctionPointerForDelegate(state.Close),
                            Cancel = Marshal.GetFunctionPointerForDelegate(state.Cancel),
                        };
                        Marshal.StructureToPtr(info, infoPtr, false);
                        return 0;
                    }
                    catch
                    {
                        return MpvError.LoadingFailed;
                    }
                };
                protocolCallbacks[protocol] = callback;
                MpvError.Throw(
                    Native.mpv_stream_cb_add_ro(handle, Native.Z(protocol), IntPtr.Zero, callback)
                );
            }
        }

        public Func<
            Func<string, IMpvStream>,
            MpvRegistration<Func<string, IMpvStream>>
        > RegisterStreamProtocol(string protocol)
        {
            return StreamProtocol(protocol);
        }

        public Func<
            Func<string, IMpvStream>,
            MpvRegistration<Func<string, IMpvStream>>
        > StreamProtocol(string protocol)
        {
            return open =>
            {
                RegisterStreamProtocol(protocol, open);
                return new MpvRegistration<Func<string, IMpvStream>>(
                    open,
                    new Registration(() => { })
                );
            };
        }

        private IMpvStream OpenPythonStream(string uri)
        {
            var m = Regex.Match(uri, @"^python://(.*)$");
            if (!m.Success)
                throw new ArgumentException("Invalid python stream URI");
            string name = m.Groups[1].Value;
            (Func<IEnumerable<byte[]>> Generator, long? Size) item;
            lock (handlerLock)
            {
                if (!pythonStreams.TryGetValue(name, out item))
                {
                    if (pythonStreamCatchall == null)
                        throw new KeyNotFoundException("Python stream name not found");
                    item = pythonStreamCatchall(name);
                }
            }
            return new GeneratorStream(item.Generator, item.Size);
        }

        public IDisposable RegisterPythonStream(
            string name,
            Func<IEnumerable<byte[]>> generator,
            long? size = null
        )
        {
            lock (handlerLock)
            {
                if (pythonStreams.ContainsKey(name))
                    throw new InvalidOperationException("Python stream already registered");
                pythonStreams[name] = (generator, size);
            }
            return new Registration(() =>
            {
                lock (handlerLock)
                    pythonStreams.Remove(name);
            });
        }

        public Func<
            Func<IEnumerable<byte[]>>,
            MpvRegistration<Func<IEnumerable<byte[]>>>
        > PythonStream(string name, long? size = null)
        {
            return generator => new MpvRegistration<Func<IEnumerable<byte[]>>>(
                generator,
                RegisterPythonStream(name, generator, size)
            );
        }

        public IDisposable RegisterPythonStreamCatchall(
            Func<string, (Func<IEnumerable<byte[]>> Generator, long? Size)> callback
        )
        {
            lock (handlerLock)
            {
                if (pythonStreamCatchall != null)
                    throw new InvalidOperationException(
                        "A catch-all Python stream is already registered"
                    );
                pythonStreamCatchall = callback;
            }
            return new Registration(() =>
            {
                lock (handlerLock)
                    if (pythonStreamCatchall == callback)
                        pythonStreamCatchall = null;
            });
        }

        public MpvRegistration<
            Func<string, (Func<IEnumerable<byte[]>> Generator, long? Size)>
        > PythonStreamCatchall(
            Func<string, (Func<IEnumerable<byte[]>> Generator, long? Size)> callback
        )
        {
            return new MpvRegistration<
                Func<string, (Func<IEnumerable<byte[]>> Generator, long? Size)>
            >(callback, RegisterPythonStreamCatchall(callback));
        }
    }
}
