'use strict';
const fs=require('fs'),vm=require('vm'),assert=require('assert');
const elements=new Map(),listeners={},timers=new Map(),calls=[];let serial=0,remembered='';
const element=()=>({value:'',textContent:'',hidden:false,disabled:false,style:{},classList:{toggle(){},contains(){return false}},querySelectorAll(){return[]},setAttribute(){},showModal(){this.open=true},close(){this.open=false}});
const document={hidden:false,getElementById(id){if(!elements.has(id))elements.set(id,element());return elements.get(id)},querySelectorAll(){return[]},addEventListener(name,fn){listeners[name]=fn}};
const sample={Version:1,Selected:0,Profiles:[{Name:'Aquário',Channels:['A','B','C','D','E'],Values:Array.from({length:48},()=>[0,0,0,0,0])}]};
const native={loadLibrary(){return JSON.stringify(sample)},preferredDevice(){return remembered},rememberDevice(id){remembered=id},discoverAutomatic(){calls.push('auto')},discover(ip){calls.push(ip||'manual')},saveLibrary(){return ''}};
const context=vm.createContext({document,window:{Lume:native,matchMedia(){return{matches:true}},addEventListener(){}},setTimeout(fn,ms){const id=++serial;timers.set(id,{fn,ms});return id},clearTimeout(id){timers.delete(id)},requestAnimationFrame(){},console,performance:{now(){return 0}}});
vm.runInContext(fs.readFileSync(__dirname+'/../app/src/main/assets/app.js','utf8').replace(/boot\(\);\s*$/,''),context);
const run=code=>vm.runInContext(code,context);let checks=0;
function ok(code){assert(run(code),code);checks++}
function delay(){return timers.get(run('autoTimer'))?.ms}
function tick(){const id=run('autoTimer'),timer=timers.get(id);assert(timer);timers.delete(id);timer.fn()}
const one={id:'raw:111111111111111111111111',ip:'192.168.4.1',protocol:'smart'},other={id:'raw:222222222222222222222222',ip:'192.168.4.3',protocol:'smart'};
function event(name,data){context.window.nativeEvent(name,data)}
(async()=>{
 await run('boot()');assert.equal(delay(),600);checks++;
 tick();assert.deepEqual(calls,['auto']);checks++;ok('searching && !busy');
 // Navigation remains usable; commands cannot race with the UDP discovery socket.
 ok('requireDevice()===false');event('autoDevices',[one]);event('searching',false);ok('device.ip==="192.168.4.1" && preferredId===device.id');assert.equal(remembered,one.id);assert.equal(delay(),60000);checks++;
 tick();event('autoDevices',[{...one,ip:'192.168.4.9'}]);event('searching',false);ok('device.ip==="192.168.4.9"');
 tick();event('autoDevices',[]);event('searching',false);ok('device===null && devices.length===0');assert.equal(delay(),20000);checks++;
 tick();event('autoDevices',[other]);event('searching',false);ok('device===null'); // Never silently switch the target.
 tick();event('autoDevices',[one,other]);event('searching',false);ok('device.id===preferredId');
 run('preferredId="";device=null;acceptDevices('+JSON.stringify([one,other])+',true)');ok('device===null');
 run('chooseDevice(devices[1])');assert.equal(remembered,other.id);checks++;
 run('autoSearch()');event('autoDevices',[one,other]);event('searching',false);ok('device.id==="'+other.id+'"');
 event('busy',true);run('autoSearch()');ok('!searching');assert.equal(delay(),2000);checks++;event('busy',false);
 run('$("modal").open=true');const before=calls.length;tick();assert.equal(calls.length,before);assert.equal(delay(),2000);checks++;run('$("modal").open=false');
 event('foreground',false);ok('autoTimer===null');run('autoSearch()');assert.equal(calls.length,before);checks++;
 event('foreground',true);ok('device===null');assert.equal(delay(),600);checks++;
 tick();const scanCount=calls.length;run('autoSearch(); search()');assert.equal(calls.length,scanCount);checks++;
 event('autoDevices',[other]);event('searching',false);
 document.hidden=true;listeners.visibilitychange();ok('autoTimer===null');document.hidden=false;listeners.visibilitychange();assert.equal(delay(),600);checks++;
 // A manual IP search keeps the existing diagnostic flow and does not overlap automatic discovery.
 run('search("192.168.4.3")');assert.equal(calls.at(-1),'192.168.4.3');ok('searching && autoTimer===null');event('busy',true);event('devices',[other]);event('busy',false);ok('!searching && !busy && device.id===preferredId');assert.equal(delay(),60000);checks++;
 // A stored choice is loaded on each new launch; no device is considered connected until a reply.
 await run('device=null;boot()');ok('preferredId==="'+other.id+'" && device===null');assert.equal(delay(),600);checks++;
 console.log(`${checks} automatic discovery checks passed`);
})().catch(e=>{console.error(e);process.exitCode=1});
