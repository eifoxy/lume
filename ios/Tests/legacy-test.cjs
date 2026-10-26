// Compare the iOS 15 implementation with the original boundary semantics in
// Node, including overlapping keys and Unicode letters outside the BMP.
const fs=require('fs'),vm=require('vm'),assert=require('assert/strict');
const source=fs.readFileSync('ios/Web/i18n.js','utf8');
const escape=s=>Array.from(s,c=>'\\^$.*+?()[]{}|'.includes(c)?'\\'+c:c).join('');
let checks=0;
function verify(rows,samples){
 const pattern=new RegExp('(?<![\\p{L}\\p{N}_])(?:'+rows.map(r=>r.pt).sort((a,b)=>b.length-a.length).map(escape).join('|')+')(?![\\p{L}\\p{N}_])','gu'),byText=new Map(rows.map(r=>[r.pt,r]));
 const selector={options:[{}],addEventListener(){}};const window={LUME_MESSAGES:rows,Lume:{languagePreference:()=>'en',deviceLanguage:()=>'en',saveLanguage:()=>''}};
 const sandbox=vm.createContext({window,navigator:{language:'en'},document:{body:{},documentElement:{},getElementById:()=>selector,createTreeWalker:()=>({nextNode:()=>false}),querySelectorAll:()=>[]},NodeFilter:{SHOW_TEXT:4}});
 vm.runInContext(`const NativeRegExp=RegExp;RegExp=function(pattern,flags){if(String(pattern).includes('(?<'))throw Error('Lookbehind unavailable on iOS 15');return new NativeRegExp(pattern,flags);};Object.hasOwn=undefined;`,sandbox);
 vm.runInContext(source,sandbox);
 for(const lang of ['en','es']){window.LumeI18n.set(lang);for(const text of samples){const expected=text.replace(pattern,key=>byText.get(key)[lang]);assert.equal(window.LumeI18n.text(text),expected,lang+': '+text);checks++;}}
}
const data={window:{}};vm.runInNewContext(fs.readFileSync('ios/Web/strings.js','utf8'),data);const rows=data.window.LUME_MESSAGES;
verify(rows,rows.flatMap(r=>[r.pt,'x'+r.pt,'𐐀'+r.pt,'𝟘'+r.pt,r.pt+'Z','('+r.pt+')',r.pt+r.pt,'_ '+r.pt+' · '+r.pt]));
verify([{pt:'Foo Bar',en:'long',es:'larga'},{pt:'Foo ',en:'prefix ',es:'prefijo '},{pt:'Bar',en:'short',es:'corta'}],['XFoo Bar','Foo Bar','Foo BarBar','!Foo Bar. Bar','𐐀Foo Bar','𝟘Bar Bar','Foo  Bar']);
const bridge=vm.createContext({window:{prompt:()=>'{"value":"OK"}'}});vm.runInContext('Object.hasOwn=undefined;',bridge);vm.runInContext(fs.readFileSync('ios/Web/ios.js','utf8'),bridge);assert.equal(bridge.window.Lume.loadLibrary(),'OK');checks++;
console.log(checks+' legacy iOS JavaScript compatibility checks passed');
