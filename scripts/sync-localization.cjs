const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'),rows=JSON.parse(fs.readFileSync(path.join(root,'localization/strings.json'),'utf8'));
const seen=new Set();for(const row of rows){if(!row.pt||!row.en||!row.es||seen.has(row.pt))throw Error('Invalid or duplicate translation: '+row.pt);seen.add(row.pt);}
const target=path.join(root,'android/app/src/main/assets/strings.js'),expected='window.LUME_MESSAGES='+JSON.stringify(rows)+';\n';
if(process.argv.includes('--check')){if(fs.readFileSync(target,'utf8')!==expected)throw Error('Run node scripts/sync-localization.cjs');}else fs.writeFileSync(target,expected);
console.log(rows.length+' shared translations verified');
