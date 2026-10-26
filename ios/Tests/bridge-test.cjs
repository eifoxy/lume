const vm=require('vm'),fs=require('fs'),assert=require('assert/strict');
let calls=[],response='{"value":""}';const window={prompt:(key,text)=>{assert.equal(key,'__LUME_NATIVE__');calls.push(JSON.parse(text));return response;}};
vm.runInNewContext(fs.readFileSync('ios/Bridge/ios.js','utf8'),{window});
window.Lume.configure('{"id":"AABBCCDDEEFF"}','Home',`quotes"\\and\n`,true,false);assert.deepEqual(calls[0],{method:'configure',args:['{"id":"AABBCCDDEEFF"}','Home',`quotes"\\and\n`,true,false]});
response='{"value":"OK:token"}';assert.equal(window.Lume.startPreview('{}','Reef',360,1,2,3,4,5),'OK:token');
response='{"error":"failed"}';assert.throws(()=>window.Lume.loadLibrary(),/failed/);response=null;assert.throws(()=>window.Lume.wifi(),/unavailable/);response='{}';assert.throws(()=>window.Lume.wifi(),/Invalid/);assert.equal(Object.isFrozen(window.Lume),true);assert.equal(window.Lume.arbitrary,undefined);
console.log('iOS bridge validation passed');
