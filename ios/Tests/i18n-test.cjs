const fs=require('fs'),vm=require('vm'),assert=require('assert/strict');
let preference='auto';const handlers={},selector={options:[{}],addEventListener(k,v){handlers[k]=v}};
const window={Lume:{languagePreference:()=>preference,deviceLanguage:()=>'es-MX',saveLanguage:v=>{preference=v;return'';}}};
const sandbox={window,navigator:{language:'en'},document:{body:{},documentElement:{},getElementById:()=>selector,createTreeWalker:()=>({nextNode:()=>false}),querySelectorAll:()=>[]},NodeFilter:{SHOW_TEXT:4}};
const context=vm.createContext(sandbox);vm.runInContext(fs.readFileSync('ios/Web/strings.js','utf8'),context);vm.runInContext(fs.readFileSync('ios/Web/i18n.js','utf8'),context);
const api=window.LumeI18n;assert.equal(api.language,'es');let checks=1;
for(const code of ['pt-BR','en','es']){api.set(code);for(const row of window.LUME_MESSAGES){assert.equal(api.text(row.pt),code==='pt-BR'?row.pt:row[code]);checks++;}assert.equal(api.text('192.168.4.1 raw:010203040506070809101112'),'192.168.4.1 raw:010203040506070809101112');checks++;}
assert.equal(api.resolve('auto','fr-FR'),'en');assert.equal(preference,'es');console.log(checks+' iOS localization checks passed');
