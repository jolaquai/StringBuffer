The `StringBuffer` package exposes a custom high-performance builder for `string`s with a mutable, directly accessible buffer and a versatile API for manipulating the contents.

# Consumption

The assembly multi-targets `netstandard2.0`, `net6.0` and `net7.0`.

- Core functionality is exposed in the `netstandard2.0` compilation, meaning any conforming project platform can use it.
- A dependency on `PCRE.NET` is introduced to facilitate all regex operations on `StringBuffer` to meet performance goals/allocation minimums for `< net7.0`.
- For `>= net7.0`, for the `Replace*` methods that take a `PcreRegex`, analogous methods that take `Regex` instance and utilize the Span-based APIs introduced in `System.Text.RegularExpressions` are also exposed.
- Several quality-of-life APIs are also introduced in their respective compilations, such as support for `ISpanFormattable` on `>= net6.0`.

# Contribution

Opening issues and submitting PRs are welcome. All changes must be appropriately covered by tests.
Support for `netstandard2.0` must always be maintained. If possible, new functionality should be added to all target frameworks. New dependencies may be introduced after I vet the decision to do so.

Or get in touch on Discord `@eyeoftheenemy`