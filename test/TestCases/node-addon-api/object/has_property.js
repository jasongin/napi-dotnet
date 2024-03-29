// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

'use strict';

const assert = require('assert');

module.exports = require('../../common').runTest(test);

function test (binding) {
  function testHasProperty (nativeHasProperty) {
    const obj = { one: 1 };

    Object.defineProperty(obj, 'two', { value: 2 });

    assert.strictEqual(nativeHasProperty(obj, 'one'), true);
    assert.strictEqual(nativeHasProperty(obj, 'two'), true);
    assert.strictEqual('toString' in obj, true);
    assert.strictEqual(nativeHasProperty(obj, 'toString'), true);
  }

  function testShouldThrowErrorIfKeyIsInvalid (nativeHasProperty) {
    assert.throws(() => {
      nativeHasProperty(undefined, 'test');
    }, /Cannot convert undefined or null to object/);
  }

  const objectWithInt32Key = { 12: 101 };
  assert.strictEqual(binding.object.hasPropertyWithUInt32(objectWithInt32Key, 12), true);

  testHasProperty(binding.object.hasPropertyWithNapiValue);
  testHasProperty(binding.object.hasPropertyWithNapiWrapperValue);
  testHasProperty(binding.object.hasPropertyWithLatin1StyleString);
  testHasProperty(binding.object.hasPropertyWithUtf8StyleString);
  testHasProperty(binding.object.hasPropertyWithCSharpStyleString);

  testShouldThrowErrorIfKeyIsInvalid(binding.object.hasPropertyWithNapiValue);
  testShouldThrowErrorIfKeyIsInvalid(binding.object.hasPropertyWithNapiWrapperValue);
  testShouldThrowErrorIfKeyIsInvalid(binding.object.hasPropertyWithLatin1StyleString);
  testShouldThrowErrorIfKeyIsInvalid(binding.object.hasPropertyWithUtf8StyleString);
  testShouldThrowErrorIfKeyIsInvalid(binding.object.hasPropertyWithCSharpStyleString);
}
