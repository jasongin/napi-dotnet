const dotnet = require('./bin/NodeApi.node');

/** @type {import('./bin/hello.d.ts')} */
const Example = dotnet.require('./bin/hello.dll');

// Call a method exported by the .NET module.
const result = Example.hello('.NET');

const assert = require('assert');
assert.strictEqual(result, 'Hello .NET!');
