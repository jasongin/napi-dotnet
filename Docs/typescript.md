# TypeScript type definitions for C# APIs

## Building type definitions
.

## Type projections reference

### Primitive types
| C# Type  | TypeScript Projection |
|----------|-----------------------|
| `bool`   | `boolean`             |
| `sbyte`  | `number`              |
| `byte`   | `number`              |
| `short`  | `number`              |
| `ushort` | `number`              |
| `int`    | `number`              |
| `uint`   | `number`              |
| `long`   | `number`              |
| `ulong`  | `number`              |
| `nint`   | `number`              |
| `nuint`  | `number`              |
| `float`  | `number`              |
| `double` | `number`              |
| `string` | `string`              |

### Object types

| C# Type                     | TypeScript Projection             |
|-----------------------------|-----------------------------------|
| `class Example`             | `class Example`                   |
| `struct Example`            | `class Example`                   |
| `interface IExample`        | `interface IExample`              |
| `delegate A Example(B)`     | `example: (B) => A`               |
| `enum Example { None = 0 }` | `const enum Example { None = 0 }` |

### Array types
| C# Type     | TypeScript Projection |
|-------------|-----------------------|
| `sbyte[]`   | `Int8Array`           |
| `sbyte[]`   | `UInt8Array`          |
| `short[]`   | `Int16Array`          |
| `ushort[]`  | `UInt16Array`         |
| `int[]`     | `Int32Array`          |
| `uint[]`    | `UInt32Array`         |
| `long[]`    | `BigInt64Array`       |
| `ulong[]`   | `BigUInt64Array`      |
| `float[]`   | `Float32Array`        |
| `double[]`  | `Float64Array`        |
| other `T[]` | `T[]` (`Array<T>`)    |

Typed arrays exported from C# use shared memory to avoid copying. Regular arrays (for which no JS "typed" arrays exist) are copied across the C#/JS boundary, therefore using collection interfaces such as `IList<T>` may be more efficient for larger arrays.

### Collection types

| C# Type                  | TypeScript Projection |
|--------------------------|-----------------------|
| `IEnumerable<T>`         | `Iterable<T>`         |
| `IReadOnlyCollection<T>` | `ReadonlySet<T>`      |
| `ICollection<T>`         | `Set<T>`              |
| `IReadOnlySet<T>`        | `ReadonlySet<T>`      |
| `ISet<T>`                | `Set<T>`              |
| `IReadOnlyList<T>`       | `readonly T[]` (`ReadonlyArray<T>`) |
| `IList<T>`               | `T[]` (`Array<T>`)    |
| `IReadOnlyDictionary<T>` | `ReadonlyMap<T>`      |
| `IDictionary<T>`         | `Map<T>`              |

Collections exported from C# use JavaScript proxies with C# handlers to avoid copying items across the C#/JS boundary. But this implementation detail does not affect the types visible to JavaScript code, e.g. a dictionary from C# still satisfies `instanceof Map` in JS.

### Special types

| C# Type          | TypeScript Projection |
|------------------|-----------------------|
| `DateTime`       | `Date`                |
| `DateTimeOffset` | `[Date, number]`      |
| `TimeSpan`       | `number`              |

The `DateTime.Kind` property is not represented in JavaScript, so dates marshalled from JavaScript will be `Unspecified` kind. A `TimeSpan` is projected to JavaScript as a decimal number of milliseconds. A `DateTimeOffset` is projected as a tuple of the UTC date-time and the offset in (positive or negative) milliseconds.
