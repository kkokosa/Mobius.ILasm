// Assert result 23
.assembly ConsoleApp
{
	.ver 1:0:0:0
}

.module ConsoleApp.exe

.mresource public 'resource.txt' {}

.class public auto ansi abstract sealed beforefieldinit ConsoleApp.Program
	extends [mscorlib]System.Object
{
    .method public hidebysig static 
        int32 Main (
            string[] args
        ) cil managed 
    {
        .maxstack 2
        .entrypoint
        .locals init (
            [0] int32 result
        )        

        ldtoken ConsoleApp.Program
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        callvirt instance class [mscorlib]System.Reflection.Assembly [mscorlib]System.Type::get_Assembly()
        ldstr "resource.txt"
        callvirt instance class [mscorlib]System.IO.Stream [mscorlib]System.Reflection.Assembly::GetManifestResourceStream(string)
        newobj instance void [mscorlib]System.IO.StreamReader::.ctor(class [mscorlib]System.IO.Stream)
        dup
        callvirt instance string [mscorlib]System.IO.TextReader::ReadToEnd()
        call int32 [mscorlib]System.Int32::Parse(string)
        stloc.0
        callvirt instance void [mscorlib]System.IO.TextReader::Close()
        ldloc.0
        ret
    }
}