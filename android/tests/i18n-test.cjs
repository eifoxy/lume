const fs=require('fs'),vm=require('vm'),assert=require('assert');
const rows=JSON.parse(fs.readFileSync(__dirname+'/../../localization/strings.json','utf8'));
let stored='auto',saved=0;const handlers={},selector={value:'',options:[{}],addEventListener(n,f){handlers[n]=f}};
const context=vm.createContext({window:{LUME_MESSAGES:rows,Lume:{languagePreference(){return stored},deviceLanguage(){return 'es-MX'},saveLanguage(v){stored=v;saved++;return ''}}},navigator:{language:'de-DE'},document:{body:{},documentElement:{},getElementById(){return selector},createTreeWalker(){return{nextNode(){return false}}},querySelectorAll(){return[]}},NodeFilter:{SHOW_TEXT:4}});
const source=fs.readFileSync(__dirname+'/../app/src/main/assets/i18n.js','utf8');vm.runInContext(source,context);const api=context.window.LumeI18n;let checks=0;
for(const [system,expected]of [['es-MX','es'],['es-ES','es'],['en-GB','en'],['pt-PT','pt-BR'],['pt-BR','pt-BR'],['fr-FR','en']]){assert.equal(api.resolve('auto',system),expected);checks++}
assert.equal(api.language,'es');checks++;
for(const language of ['en','es','pt-BR']){api.set(language);for(const row of rows){assert.equal(api.text(row.pt),language==='pt-BR'?row.pt:row[language],row.pt);checks++;}assert.equal(api.text('ReefLab 192.168.4.1 CH3 raw:010203040506070809101112'),'ReefLab 192.168.4.1 CH3 raw:010203040506070809101112');checks++;}
api.set('es');vm.runInContext(source,context);assert.equal(context.window.LumeI18n.language,'es');checks++;
assert.throws(()=>api.set('bad'));checks++;
context.window.canChangeLanguage=()=>false;selector.value='en';handlers.change();assert.equal(stored,'es');checks++;
context.window.canChangeLanguage=()=>true;selector.value='en';handlers.change();assert.equal(stored,'en');checks++;
api.set('auto');assert.equal(api.language,'es');checks++;
assert(saved>0);console.log(checks+' localization checks passed');
