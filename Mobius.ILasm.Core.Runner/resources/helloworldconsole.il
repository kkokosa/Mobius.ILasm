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
int32 Main (
string[] args
) cil managed 
{
.maxstack 8
.entrypoint

nop
ldstr     "Hello world!"
call      void [mscorlib]System.Console::WriteLine(string)
nop
ldc.i4.s 44
ret
}
}