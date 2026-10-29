const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'),source=path.join(root,'android/app/src/main/assets'),target=path.join(root,'ios/Web');
const check=process.argv.includes('--check');
if(!check)fs.mkdirSync(target,{recursive:true});
const rows=JSON.parse(fs.readFileSync(path.join(root,'localization/strings.json'),'utf8'));
const replacements=[
 ['Abra o Wi-Fi do Android e conecte à rede criada pela controladora. Se aparecer “sem internet”, escolha continuar conectado.','Abra Ajustes > Wi-Fi e conecte à rede da controladora, mesmo sem internet. Volte ao Lume e permita Rede Local.'],
 ['Abrir redes Wi-Fi','Como conectar no iPhone'],['Abrir Wi-Fi','Como conectar no iPhone'],
 ['Diagnóstico da última comunicação · v1.4.0','Diagnóstico da última comunicação · iOS 0.3.0'],
 ['Compatível com o protocolo Wi-Fi do Pled V2.2. A TC420 comum, sem módulo Wi-Fi, não pode ganhar conexão sem fio pelo app. A confirmação no seu modelo ainda precisa de teste no aparelho.','Prévia iOS em desenvolvimento. Busca por consultas diretas na rede local, sem broadcast. Se necessário, tente o IP conhecido. TC420 sem Wi-Fi não é compatível. O teste no iPhone real ainda está pendente.']
];
const extra=[
 {pt:replacements[0][1],en:'Open Settings > Wi-Fi and join the controller network, even without internet. Return to Lume and allow Local Network.',es:'Abre Ajustes > Wi-Fi y conecta con la red del aparato, incluso sin internet. Vuelve a Lume y permite Red local.'},
 {pt:'Como conectar no iPhone',en:'How to connect on iPhone',es:'Cómo conectar en iPhone'},
 {pt:replacements[3][1],en:'Last communication diagnostics · iOS 0.3.0',es:'Diagnóstico de la última comunicación · iOS 0.3.0'},
 {pt:replacements[4][1],en:'iOS development preview. Discovery uses direct local network queries, without broadcast. Try a known IP if needed. TC420 without Wi-Fi is unsupported. Real iPhone testing is pending.',es:'Versión iOS en desarrollo. Búsqueda mediante consultas directas en la red local, sin broadcast. Prueba una IP conocida si es necesario. TC420 sin Wi-Fi no es compatible. Falta probar en un iPhone real.'}
];
extra.push(...JSON.parse(fs.readFileSync(path.join(root,'ios/Resources/messages.json'),'utf8')));
const combined=[...rows,...extra],seen=new Set();for(const row of combined){if(!row.pt||!row.en||!row.es||seen.has(row.pt))throw Error('Missing or duplicate iOS translation: '+row.pt);seen.add(row.pt);}
const files=['index.html','app.js','app.css','i18n.js','live.js','qr.js','qr-ui.js','library.json','strings.js','ios.js'];
for(const file of files){let data;
 if(file==='strings.js')data=Buffer.from('window.LUME_MESSAGES='+JSON.stringify(combined)+';\n');
 else if(file==='ios.js')data=fs.readFileSync(path.join(root,'ios/Bridge/ios.js'));
 else if(file==='index.html'){let html=fs.readFileSync(path.join(source,file),'utf8');for(const [from,to]of replacements){if(!html.includes(from))throw Error('Shared HTML changed: '+from);html=html.replaceAll(from,to);}data=Buffer.from(html.replace('<script src="strings.js">','<script src="ios.js"></script><script src="strings.js">'));}
 else data=fs.readFileSync(path.join(source,file));
 const dest=path.join(target,file);if(check){if(!fs.existsSync(dest)||!fs.readFileSync(dest).equals(data))throw Error('Run node scripts/sync-ios-assets.cjs: '+file);}else fs.writeFileSync(dest,data);
}
console.log('iOS offline assets verified, including three languages');
