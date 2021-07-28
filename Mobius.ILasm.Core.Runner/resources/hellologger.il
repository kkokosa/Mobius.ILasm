.assembly ConsoleApp
{
.ver 1:0:0:0
}

.module ConsoleApp.exe

.class private auto ansi beforefieldinit ConsoleApp.Program
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname 
	instance void .ctor () cil managed 
	{
		.maxstack 8

		ldarg.0
		call      instance void [mscorlib]System.Object::.ctor()
		nop
		ret
	}

	.method private hidebysig static 
	void Main (
		string[] args
		) cil managed 
	{
		.maxstack 3
		.entrypoint
        .locals init (
            [0] class ConsoleApp.Logger logger,
            [1] int32 level
        )

        ldc.i4.3
        newobj instance void ConsoleApp.Logger::.ctor(int32)
        stloc.0
        ldc.i4.s 44
        stloc.1
        ldloc.0
        ldloc.1
        ldstr "Hello logger!"
        callvirt instance void ConsoleApp.Logger::Log(int32, string)
        ret
	}
}

.class public auto ansi beforefieldinit ConsoleApp.Logger
    extends [mscorlib]System.Object
{
    // Fields
    .field private int32 _level

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 level
        ) cil managed 
    {
        // Method begins at RVA 0x2079
        // Code size 14 (0xe)
        .maxstack 8

        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ldarg.0
        ldarg.1
        stfld int32 ConsoleApp.Logger::_level
        ret
    } // end of method Logger::.ctor

    .method public hidebysig 
        instance void Log (
            int32 level,
            string str
        ) cil managed 
    {
        // Method begins at RVA 0x2088
        // Code size 16 (0x10)
        .maxstack 8

        ldarg.1
        ldarg.0
        ldfld int32 ConsoleApp.Logger::_level
        blt.s IL_000f

        ldarg.2
        call void [mscorlib]System.Console::WriteLine(string)

IL_000f: ret
    } // end of method Logger::Log

} // end of class Logger