
## Example: Using .NET Semantic Kernel to call Azure OpenAI
The `example.js` script dynamically loads the `Microsoft.SemanticKernel` .NET assembly and uses it
to make a call to Azure OpenAI to summarize some text.

| Command                          | Explanation
|----------------------------------|--------------------------------------------------
| `dotnet publish -f net7.0 ../..` | Build NodeApi .NET host.
| `dotnet pack ../..`              | Build NodeApi .NET & npm packages.
| `npm install`                    | Install `node-api-dotnet` npm package into the project.
| `dotnet restore`                 | Install `SemanticKernel` nuget package into the project.
| `node example.js`                | Run example JS code that uses that package.

#### Type Definitions (Optional)
To generate type definitions for the example JavaScript code, run the following command, after building the Node API Generator project. (Replace `win-x64` with the current platform if necessary.)
```
dotnet ..\..\out\bin\Debug\NodeApi.Generator\net6.0\win-x64\Microsoft.JavaScript.NodeApi.Generator.dll -a pkg\microsoft.semantickernel\0.8.48.1-preview\lib\netstandard2.1\Microsoft.SemanticKernel.dll -t Microsoft.SemanticKernel.d.ts -r pkg\microsoft.extensions.logging.abstractions\7.0.0\lib\netstandard2.0\Microsoft.Extensions.Logging.Abstractions.dll
```

_Soon the Node API TS geneator tool will be packaged as an npm package. Then it can be more easily referenced as an npm dependency of this project._
