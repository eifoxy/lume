'use strict';
window.LumeQr=(()=>{
 const fail=message=>{throw Error(message);};
 function crc(bytes){let c=0xffffffff;for(const b of bytes){c^=b;for(let i=0;i<8;i++)c=(c>>>1)^((c&1)?0xedb88320:0);}return((c^0xffffffff)>>>0).toString(16).padStart(8,'0');}
 function decode(bytes){
  if(bytes.length>131072||bytes.length<9||String.fromCharCode(...bytes.slice(0,5))!=='LUME\x01')fail('QR do Lume inválido.');
  let at=5;const word=()=>{if(at+2>bytes.length)fail('QR do Lume incompleto.');const v=bytes[at]|bytes[at+1]<<8;at+=2;return v;};
  const count=word(),selected=word();if(count<1||count>100||selected>=count)fail('QR do Lume inválido.');
  const text=max=>{const length=word();if(length<1||length>max*3||at+length>bytes.length)fail('QR do Lume inválido.');let result;try{result=new TextDecoder('utf-8',{fatal:true}).decode(bytes.slice(at,at+length));}catch{fail('Texto inválido no QR.');}at+=length;if(!result.trim()||result.length>max)fail('Texto inválido no QR.');return result;};
  const Profiles=[];for(let i=0;i<count;i++){const Name=text(80),Channels=Array.from({length:5},()=>text(40)),Values=[];for(let row=0;row<48;row++){if(at+5>bytes.length)fail('QR do Lume incompleto.');const values=Array.from(bytes.slice(at,at+5));if(values.some(v=>v>100))fail('Intensidade inválida no QR.');at+=5;Values.push(values);}Profiles.push({Name,Channels,Values});}
  if(at!==bytes.length)fail('Dados extras no QR.');return{Version:1,Selected:selected,Profiles};
 }
 class Receiver{
  constructor(){this.started=Date.now();this.parts=new Map();this.id='';this.total=0;this.checksum='';}
  add(frame){
   if(Date.now()-this.started>600000)fail('A leitura expirou. Comece novamente.');
   if(typeof frame!=='string'||frame.length>600)fail('QR do Lume inválido.');
   const match=/^LUMEQR1:([a-fA-F0-9]{16}):([1-9][0-9]{0,2}):([1-9][0-9]{0,2}):([a-fA-F0-9]{8}):([A-Za-z0-9_-]+)$/.exec(frame);if(!match)fail('Este QR não é uma transferência do Lume.');
   const [,id,part,total,sum,payload]=match,index=Number(part),count=Number(total);if(count>384||index>count)fail('QR do Lume inválido.');
   let data;try{const b=payload.replace(/-/g,'+').replace(/_/g,'/');data=Uint8Array.from(atob(b+'='.repeat((4-b.length%4)%4)),c=>c.charCodeAt(0));}catch{fail('QR do Lume inválido.');}
   if(data.length>360||data.length<1||(index<count&&data.length!==360))fail('QR do Lume incompleto.');
   const key=id.toLowerCase(),checksum=sum.toLowerCase();if(this.id&&(key!==this.id||count!==this.total||checksum!==this.checksum))fail('Outro envio foi detectado. Termine este ou cancele para recomeçar.');
   const previous=this.parts.get(index);if(previous&&(!previous.every((v,i)=>v===data[i])||previous.length!==data.length))fail('Partes conflitantes no QR.');
   this.id=key;this.total=count;this.checksum=checksum;this.parts.set(index,data);
   if(this.parts.size!==count)return{received:this.parts.size,total:count};
   const length=Array.from(this.parts.values()).reduce((n,p)=>n+p.length,0);if(length>131072)fail('Biblioteca grande demais para QR.');
   const bytes=new Uint8Array(length);let offset=0;for(let i=1;i<=count;i++){bytes.set(this.parts.get(i),offset);offset+=this.parts.get(i).length;}if(crc(bytes)!==checksum)fail('A verificação do QR falhou. Comece novamente.');
   return{received:count,total:count,library:decode(bytes)};
  }
 }
 return{Receiver,decode,crc};
})();
