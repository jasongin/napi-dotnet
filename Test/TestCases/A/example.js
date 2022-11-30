// Load the addon module.
const example = require(process.env['TEST_NODE_API_MODULE_PATH']);

// Call a method exported by the addon module.
example.helloNoParam();
example.hello('World');

// Check the properties that are on the module.
console.log('example keys: ' + JSON.stringify(Object.keys(example)));

// Construct an instance of a class exported by the addon module.
const instance = new example.Another();

// Test static and instance properties and methods on the exported class.
console.log('Static property: ' + example.Another.staticValue);
console.log('Instance property: ' + instance.instanceValue);
example.Another.staticMethod();
instance.instanceMethod();

// Get and set a property exported directly by the addon module.
console.log('example property value: ' + example.value);
example.value = "goodbye";
console.log('modified property value: ' + example.value);
