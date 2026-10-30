using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LunaPlayer.Playback;

internal static partial class SofaFilterValidator
{
    internal static bool IsValid(string filter)
    {
        nint graph = 0;
        nint inputs = 0;
        nint outputs = 0;
        try
        {
            graph = AllocateGraph();
            return graph != 0 && ParseGraph(graph, filter, ref inputs, ref outputs, 0) >= 0;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return false;
        }
        finally
        {
            if (inputs != 0)
                FreeInOut(ref inputs);
            if (outputs != 0)
                FreeInOut(ref outputs);
            if (graph != 0)
                FreeGraph(ref graph);
        }
    }

    [LibraryImport("avfilter", EntryPoint = "avfilter_graph_alloc")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint AllocateGraph();

    [LibraryImport("avfilter", EntryPoint = "avfilter_graph_parse_ptr", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ParseGraph(nint graph, string filters, ref nint inputs, ref nint outputs, nint logContext);

    [LibraryImport("avfilter", EntryPoint = "avfilter_inout_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void FreeInOut(ref nint inOut);

    [LibraryImport("avfilter", EntryPoint = "avfilter_graph_free")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void FreeGraph(ref nint graph);
}
