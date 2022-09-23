/*
 Its not really clear why this file is needed... But without it building the dependent projects yield error like this

/Users/boxofyellow/Projects/Ascii3dEngine/obj/Ascii3dEngine.Tanks/Release/net6.0/Ascii3dEngine.Tanks.GlobalUsings.g.cs(2,22): error CS0400: The type or namespace name 'Ascii3dEngine' could not be found in the global namespace (are you missing an assembly reference?) [/Users/boxofyellow/Projects/Ascii3dEngine/Tanks/Ascii3dEngine.Tanks.csproj]
/Users/boxofyellow/Projects/Ascii3dEngine/obj/Ascii3dEngine.TechDemo/Release/net6.0/Ascii3dEngine.TechDemo.GlobalUsings.g.cs(2,22): error CS0400: The type or namespace name 'Ascii3dEngine' could not be found in the global namespace (are you missing an assembly reference?) [/Users/boxofyellow/Projects/Ascii3dEngine/TechDemo/Ascii3dEngine.TechDemo.csproj]
/Users/boxofyellow/Projects/Ascii3dEngine/obj/Ascii3dEngine.Tests/Release/net6.0/Ascii3dEngine.Tests.GlobalUsings.g.cs(2,22): error CS0400: The type or namespace name 'Ascii3dEngine' could not be found in the global namespace (are you missing an assembly reference?) [/Users/boxofyellow/Projects/Ascii3dEngine/Tests/Ascii3dEngine.Tests.csproj]
/Users/boxofyellow/Projects/Ascii3dEngine/obj/Ascii3dEngine.Benchmark/Release/net6.0/Ascii3dEngine.Benchmark.GlobalUsings.g.cs(2,22): error CS0400: The type or namespace name 'Ascii3dEngine' could not be found in the global namespace (are you missing an assembly reference?) [/Users/boxofyellow/Projects/Ascii3dEngine/Benchmark/Ascii3dEngine.Benchmark.csproj]        

 This problem started happing after I removed all the `namespace Ascii3dEngine.Engine` from all the files
 It might be that and the `<Using Include="Ascii3dEngine.Engine" />` in the depend projects is incompatible 🤷🏽‍♂️ 
*/
namespace Ascii3dEngine.Engine { internal class Dummy { } }
