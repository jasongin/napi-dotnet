# Building C# Modules for JavaScript

 - Reference the NodeAPI nuget package
 - Reference the NodeAPI exports generator package, if using code generation
 - Reference the NodeAPI TS generator package, if using TS type definitions
 - Build platform-specific binaries, if using AOT

## AOT Compilation vs CLR Hosting
 - List of pros & cons and/or target scenarios

### Importing an AOT-Compiled C# module to JavaScript

### Importing a CLR-Hosted C# module to JavaScript

### C# module initialization
 - Optional use of `[JSModule]` attribute on one class or one init method
 - Module class
   - Constructor with optional context
   - Optional IDisposable
   - Public methods on the module are exported, along with others tagged with `[JSExport]`
 - Initialization method
   - Requires explicit use of `DefineClass()` and `ExportModule()` APIs
   - No other methods in the module class or assembly are auto-exported

### See also
[Exporting C# APIs to JavaScript](./cs_exports.md)
[Importing JavaScript APIs to C#](./js_imports.md)
