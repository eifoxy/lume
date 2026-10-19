package br.com.lume;

import java.io.IOException;
import java.util.*;

/** Validated, immutable transfer plan. Smart WiFi Timer finalization differs from Pled. */
public final class Upload {
 public interface Sender {void send(byte[] payload,int expected)throws Exception;}
 public interface Progress {void update(int percent,String phase);}
 public interface Pacer {void pause()throws Exception;}
 public static final class Item {
  final int slot;final String alias;final int[][] values;
  public Item(int slot,String alias,int[][] rows){
   Protocol.smartHeader(slot,alias);if(rows==null||rows.length!=48)throw new IllegalArgumentException("O perfil precisa de 48 horários.");
   this.slot=slot;this.alias=alias;this.values=new int[48][];
   for(int r=0;r<48;r++){if(rows[r]==null)throw new IllegalArgumentException("Horário vazio.");Protocol.step(r,rows[r]);values[r]=rows[r].clone();}
  }
 }
 public static void run(List<Item> source,boolean clear,boolean smart,Sender send,PacketIO.Guard guard,Progress progress,Pacer pacer)throws Exception {
  if(source==null||source.isEmpty()||source.size()>16)throw new IllegalArgumentException("Envie entre 1 e 16 perfis.");
  List<Item> items=new ArrayList<>();Set<Integer> slots=new HashSet<>();Set<String> names=new HashSet<>();
  for(Item item:source){if(item==null)throw new IllegalArgumentException("Perfil vazio.");Item copy=new Item(item.slot,item.alias,item.values);if(!slots.add(copy.slot)||!names.add(copy.alias.toUpperCase(Locale.ROOT)))throw new IllegalArgumentException("Posições ou nomes repetidos.");if(clear&&copy.slot!=items.size())throw new IllegalArgumentException("A substituição deve começar na posição 1, em ordem.");items.add(copy);}
  int completed=0;boolean eraseAttempted=false;String phase="iniciar";
  try{
   guard.check();
   if(clear){phase="limpar perfis antigos";progress.update(0,"Limpando os perfis antigos…");eraseAttempted=true;send.send(new byte[]{5,(byte)0xfe},0x82);}
   for(int i=0;i<items.size();i++){
    Item item=items.get(i);guard.check();phase="iniciar perfil "+(i+1);send.send(smart?Protocol.smartHeader(item.slot,item.alias):Protocol.header(item.slot,item.alias),0x80);
    for(int r=0;r<48;r++){
     guard.check();phase="gravar perfil "+(i+1)+", horário "+(r+1);byte[] point=Protocol.step(r,item.values[r]);int attempts=smart?3:1;
     for(int attempt=1;;attempt++){
      guard.check();
      try{send.send(point.clone(),0x81);break;}
      catch(PacketIO.AckTimeout e){if(attempt>=attempts||e.deviceReplied)throw e;progress.update((i*48+r)*98/(items.size()*48),"Reconfirmando perfil "+(i+1)+", horário "+(r+1)+" · tentativa "+(attempt+1)+"/"+attempts);pacer.pause();}
     }
     if(smart)pacer.pause();progress.update((i*48+r+1)*98/(items.size()*48),"Perfil "+(i+1)+"/"+items.size()+" · horário "+(r+1)+"/48");
    }
    if(smart){guard.check();phase="encerrar perfil "+(i+1);send.send(new byte[]{3,8},0x83);}completed++;
   }
   if(smart){guard.check();phase="finalizar o conjunto";progress.update(99,"Finalizando a transferência…");send.send(new byte[]{4,10},0x91);}
   progress.update(100,completed+" perfil(is) confirmado(s). Confira no aparelho o perfil ativo.");
  }catch(Exception e){throw new IOException("Falha ao "+phase+". "+completed+"/"+items.size()+" perfis tiveram os horários recebidos. "+(eraseAttempted?"A memória pode ter sido apagada ou estar incompleta. ":"A gravação/finalização pode estar incompleta. ")+"Reconecte e reenvie. "+e.getMessage(),e);}
 }
}
