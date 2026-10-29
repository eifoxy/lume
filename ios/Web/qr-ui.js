'use strict';
window.LumeQrUI=(()=>{
 let receiver=null,timer=null;
 function end(){receiver=null;clearTimeout(timer);if(native&&native.stopQr)native.stopQr();scheduleAuto(2000);}
 $('scanQr').onclick=()=>{
  if(busy||searching||liveLocked()||$('modal').open){toast(tr('Aguarde a operação atual.'),true);return;}
  if(!native||!native.scanQr){toast(tr('Instale a versão atual do Lume para ler QR codes.'),true);return;}
  if(!flush())return;stopAuto();receiver=new LumeQr.Receiver();
  timer=setTimeout(()=>{end();toast(tr('A leitura expirou. Comece novamente.'),true);},900000);
  try{native.scanQr(tr('Ler QR do PC'),tr('Aponte para o QR no Lume do computador. Aguarde todas as partes.'),tr('Cancelar'));}catch(e){end();toast(e.message,true);}
 };
 const original=window.nativeEvent;
 window.nativeEvent=(type,data)=>{
  if(type==='qrCancelled'){end();return;}
  if(type==='qr'){
   if(!receiver)return;
   try{const result=receiver.add(data);if(result.library){const library=result.library;end();original('import',library);}else native.qrProgress(tr('Partes recebidas: ')+result.received+' / '+result.total+tr('. Mantenha a câmera apontada para a tela.'));}
   catch(error){native.qrProgress(tr(error.message));}
   return;
  }
  return original(type,data);
 };
 return{get active(){return receiver!==null;}};
})();
