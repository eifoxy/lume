const fs=require('fs'),vm=require('vm'),assert=require('assert');
const elements=new Map(),element=()=>({value:'',textContent:'',hidden:false,disabled:false,style:{},classList:{toggle(){},contains(){return false}},querySelectorAll(){return[]},setAttribute(){},addEventListener(){},showModal(){this.open=true},close(){this.open=false}});
const document={hidden:false,getElementById(id){if(!elements.has(id))elements.set(id,element());return elements.get(id)},querySelectorAll(){return[]},addEventListener(){}};
const context=vm.createContext({document,window:{matchMedia(){return{matches:true}},addEventListener(){}},localStorage:{setItem(){},getItem(){return null}},setTimeout(){return 1},clearTimeout(){},requestAnimationFrame(){},console,performance:{now(){return 0}}});
let source=fs.readFileSync(__dirname+'/../app/src/main/assets/app.js','utf8').replace(/boot\(\);\s*$/,'');vm.runInContext(source,context);
let checks=0;function run(code){return vm.runInContext(code,context)}function ok(code){assert(run(code),code);checks++;}
run('library={Version:1,Profiles:[fresh()],Selected:0}');ok('validate(library).Selected===0');ok('profile().Values.length===48 && profile().Values.every(r=>r.length===5)');
run('profile().Values[47][0]=100; profile().Values[0][0]=0');ok('interpolated(1425)[0]===50');run('profile().Values[12][1]=20;profile().Values[13][1]=80');ok('interpolated(375)[1]===50');ok('time(1439)==="23:59" && time(0)==="00:00"');
for(const change of ['p.Values.pop()','p.Values[0][0]=101','p.Values[0][0]=3.5','p.Values[0][0]="5"','p.Channels.pop()','p.Name=" "']){run(`{let p=fresh();${change};try{validate({Profiles:[p]});throw Error('accepted')}catch(e){if(e.message==='accepted')throw e}}`);checks++;}
ok('JSON.stringify(aliases(["Áquário longo","Aquario longo","!!!"]))===JSON.stringify(["Aquario","Aquari2","PERFIL"])');
run('checkpoint();profile().Name="Editado";library=JSON.parse(history.pop())');ok('profile().Name==="Meu aquário"');run('dirty=true;storageBroken=true');ok('flush()===false && dirty===true');run('storageBroken=false');ok('flush()===true && dirty===false');ok('esc("<img onerror=1>")==="&lt;img onerror=1&gt;"');
// Distinct curves at every time point catch partial transfers and accidental overwrites.
run('storageBroken=false;library={Version:1,Selected:0,Profiles:[fresh("A"),fresh("B")]};for(const p of library.Profiles){p.Channels=["Azul","Cooler","C","D","E"];p.Values=p.Values.map((_,r)=>[r,100-r,7,8,9]);}history=[];let assertRejected=false;const original=JSON.stringify(library)');
run('applyChannelTransfer(0,1,false,false)');
ok('profile().Channels[0]==="Cooler" && profile().Channels[1]==="Azul"');
ok('profile().Values.every((r,i)=>r[0]===100-i&&r[1]===i&&r[2]===7&&r[3]===8&&r[4]===9)');
ok('library.Profiles[1].Values.every((r,i)=>r[0]===i&&r[1]===100-i)');
run('$("undo").onclick()');ok('JSON.stringify(library)===original');
run('applyChannelTransfer(0,1,false,true)');ok('library.Profiles.every(p=>p.Channels[1]==="Azul"&&p.Values.every((r,i)=>r[0]===100-i&&r[1]===i))');
run('$("undo").onclick()');ok('JSON.stringify(library)===original');
run('applyChannelTransfer(0,1,true,true)');ok('library.Profiles.every(p=>p.Channels[0]==="Azul"&&p.Channels[1]==="Azul"&&p.Values.every((r,i)=>r[0]===i&&r[1]===i&&r[2]===7))');
run('$("undo").onclick()');ok('JSON.stringify(library)===original');
for(const args of ['0,0,false,false','-1,2,false,true','0,5,true,false','0.5,2,false,false']){run(`assertRejected=false;try{applyChannelTransfer(${args})}catch(e){assertRejected=true}`);ok('assertRejected && JSON.stringify(library)===original && history.length===0');}
run('busy=true;assertRejected=false;try{applyChannelTransfer(0,1,false,true)}catch(e){assertRejected=true}busy=false');ok('assertRejected&&JSON.stringify(library)===original');
run('liveLocked=()=>true;assertRejected=false;try{applyChannelTransfer(0,1,false,true)}catch(e){assertRejected=true}');ok('assertRejected&&JSON.stringify(library)===original');
console.log(`${checks} editor checks passed`);
