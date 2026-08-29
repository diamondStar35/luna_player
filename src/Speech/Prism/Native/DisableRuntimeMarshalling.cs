using System.Runtime.CompilerServices;

// Every P/Invoke in this assembly passes blittable types only, so the runtime marshaller is
// switched off entirely. This is what makes the bindings allocation-free at the boundary and
// safe under NativeAOT: the LibraryImport source generator emits direct calls with no stubs.
[assembly: DisableRuntimeMarshalling]
