# Project C# readability

Applies to project-owned C# in `Assets/_Game/Scripts` and `Assets/_Game/Tests`, including Editor and development verification code.

- Do not minify C# source. Keep generated project code human-readable.
- Use four spaces, UTF-8, LF and a final newline.
- Put braces on separate lines. Use braces for control-flow bodies.
- Put one executable statement on each line and one field variable in each declaration.
- Put each method in its own block with one blank line between methods.
- Target 120 characters, wrapping conditions, argument lists, LINQ and nested ternaries at meaningful boundaries. Keep grouping and operator precedence unchanged.
- Keep short properties and expression-bodied members concise when readable.
- Keep the existing block-scoped namespace convention.
- Preserve useful comments; do not add comments that simply restate code or dozens of regions.
- Never rename public/serialized fields, events, methods, Animator parameters or scene-reference strings during formatting.
- Formatting is not an opportunity to extract methods, optimize loops, replace LINQ, change accessibility, rearrange execution or alter coroutine behavior.
- Keep string literal contents, attributes, initialization order, events and callbacks intact.
- Leave Package/Plugin/Vendor code, Unity templates and genuine `.g.cs`/`.generated.cs` output alone. Historical code samples in Docs and recovery snapshots are not active project sources.

The root `.editorconfig` applies these C# settings only beneath `Assets/_Game`; it does not impose four-space indentation on Unity YAML or third-party sources.

## Formatter

CSharpier **0.30.6** is pinned in `.config/dotnet-tools.json`. It uses Roslyn parsing and validates its printed syntax. `.csharpierrc.json` sets a 120-character target. Use a .NET 8 SDK or a compatible SDK; this development tool is not a game runtime dependency.

```sh
dotnet tool restore
dotnet csharpier Assets/_Game/Scripts Assets/_Game/Tests --check --no-msbuild-check
```

`--no-msbuild-check` skips only project/package-version probing for Unity-generated project files; syntax validation stays enabled.

To apply whitespace formatting, omit `--check`. Do not run the formatter against the entire repository. CSharpier does not enforce every rule above: new code must already have braces and separate field declarations.

## Safe readability changes

Brace insertion must preserve the original parsed body and dangling-else association. Splitting grouped fields must preserve each name, type, modifier, attribute and initializer in the same order. Conditional branches require verification under their relevant symbols. Avoid automated restructuring across preprocessor directives.

For this pass, an independent Roslyn comparison checks all eight combinations of `UNITY_EDITOR`, `DEVELOPMENT_BUILD` and `UNITY_EDITOR_WIN`. It permits only the explicitly applied body braces, equivalent field/event-field declaration splitting, using-order changes and optional trailing initializer/enum commas. All remaining token kinds and text must match exactly, including identifiers, expressions and literals. Directive text/order and comment contents/counts are also checked. This is structural preservation evidence; it is supplemented by Unity compilation and a short Play Mode smoke test.
