
const array: object[] = [];
array[0] = {};
const arrayProxy = new Proxy(array, {});
console.log('Array string: ' + arrayProxy.toString());
console.log('Proxy string: ' + arrayProxy.toString());
console.log('Proxy is array: ' + Array.isArray(arrayProxy));
console.log('Proxy instanceof array: ' + (arrayProxy instanceof Array));
console.log('Proxy[0]: ' + arrayProxy[0]);

const map = new Map();
map[0] = {};
const mapProxy = new Proxy(map, {});
console.log('Map string: ' + mapProxy.toString());
console.log('Proxy string: ' + mapProxy.toString());
console.log('Proxy instanceof Map: ' + (mapProxy instanceof Map));
console.log('Proxy[0]: ' + mapProxy[0]);

const iterableArray: Iterable<object> = array;

const set: ReadonlySet<number> = new Set([1, 2, 3]);

console.log(set.size);

