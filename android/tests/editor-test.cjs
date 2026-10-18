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
console.log(`${checks} editor checks passed`);
