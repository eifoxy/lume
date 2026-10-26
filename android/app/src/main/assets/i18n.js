'use strict';
window.LumeI18n=(()=>{
 const rows=window.LUME_MESSAGES,byText=new Map(rows.map(r=>[r.pt,r]));
 const escape=s=>Array.from(s,c=>'\\^$.*+?()[]{}|'.includes(c)?'\\'+c:c).join('');
 // Check the left boundary explicitly: RegExp lookbehind is unavailable on iOS 15.
 const pattern=new RegExp('(?:'+rows.map(r=>r.pt).sort((a,b)=>b.length-a.length).map(escape).join('|')+')(?![\\p{L}\\p{N}_])','gu');
 const endsInWord=/[\p{L}\p{N}_]$/u;
 const bridge=window.Lume,selector=document.getElementById('languageSelect');
 const resolve=(choice,system)=>{const language=(choice==='auto'?system:choice).toLowerCase();return language.startsWith('pt')?'pt-BR':language.startsWith('es')?'es':'en';};
 let preference='auto',system=navigator.language||'en',language;
 try{if(bridge){preference=bridge.languagePreference();system=bridge.deviceLanguage();}else preference=localStorage.getItem('lume-language')||'auto';}catch{}
 if(!['auto','pt-BR','en','es'].includes(preference))preference='auto';
 language=resolve(preference,system);
 function text(value){
  if(language==='pt-BR')return value;
  const source=String(value);let result='',end=0,match;pattern.lastIndex=0;
  while((match=pattern.exec(source))){
   if(endsInWord.test(source.slice(0,match.index))){pattern.lastIndex=match.index+(source.codePointAt(match.index)>65535?2:1);continue;}
   result+=source.slice(end,match.index)+byText.get(match[0])[language];end=pattern.lastIndex;
  }
  return result+source.slice(end);
 }
 const nodes=[],attrs=[];
 const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
 while(walker.nextNode()){const node=walker.currentNode;if(node.parentElement.closest('script,style,[data-no-i18n]'))continue;if(node.textContent.trim())nodes.push([node,node.textContent]);}
 for(const element of document.querySelectorAll('[aria-label],[placeholder],[title]'))for(const name of ['aria-label','placeholder','title'])if(element.hasAttribute(name))attrs.push([element,name,element.getAttribute(name)]);
 function apply(){document.documentElement.lang=language;for(const[node,source]of nodes)if(node.isConnected)node.textContent=text(source);for(const[element,name,source]of attrs)if(element.isConnected)element.setAttribute(name,text(source));selector.value=preference;selector.options[0].textContent=text('Automático (dispositivo)');}
 function set(choice){if(!['auto','pt-BR','en','es'].includes(choice))throw Error('Invalid language');if(bridge){const error=bridge.saveLanguage(choice);if(error)throw Error(error);}else localStorage.setItem('lume-language',choice);preference=choice;language=resolve(choice,system);apply();if(window.refreshLanguage)window.refreshLanguage();}
 selector.addEventListener('change',()=>{if(window.canChangeLanguage&&!window.canChangeLanguage()){selector.value=preference;return;}try{set(selector.value);}catch{selector.value=preference;}});
 apply();return{text,set,resolve,get language(){return language;},get preference(){return preference;}};
})();
