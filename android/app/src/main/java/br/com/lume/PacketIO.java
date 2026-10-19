package br.com.lume;
import java.net.*;
import java.io.*;
/** One UDP socket for send/receive. Indexed-point retry policy lives in Upload. */
public final class PacketIO {
 public interface Guard { void check() throws Exception; }
 public static final class AckTimeout extends IOException {
  public final boolean deviceReplied;
  public AckTimeout(boolean deviceReplied,int received,String detail){super("Sem confirmação após o prazo de espera. Datagramas recebidos: "+received+". "+detail);this.deviceReplied=deviceReplied;}
 }
 public static void exchange(DatagramSocket socket,InetAddress target,int port,byte[] frame,int command,int[] accepted,int timeoutMs,Guard guard)throws Exception{
  exchange(socket,target,port,frame,command,accepted,timeoutMs,guard,null);
 }
 public static void exchange(DatagramSocket socket,InetAddress target,int port,byte[] frame,int command,int[] accepted,int timeoutMs,Guard guard,String identifier)throws Exception{
  guard.check();
  // Discard queued replies from the preceding command; they cannot confirm this one.
  socket.setSoTimeout(1);for(int i=0;i<32;i++){guard.check();try{socket.receive(new DatagramPacket(new byte[65],65));}catch(SocketTimeoutException empty){break;}}
  socket.send(new DatagramPacket(frame,frame.length,target,port));
  long deadline=System.nanoTime()+timeoutMs*1000000L;
  int received=0;boolean deviceReplied=false;String detail="Nenhuma resposta recebida para este comando.";
  while(System.nanoTime()<deadline){guard.check();socket.setSoTimeout((int)Math.max(1,Math.min(200,(deadline-System.nanoTime())/1000000L)));byte[] bytes=new byte[65];DatagramPacket response=new DatagramPacket(bytes,bytes.length);
   try{socket.receive(response);}catch(SocketTimeoutException e){continue;}
   received++;
   if(response.getPort()!=port||!response.getAddress().equals(target)){detail="Resposta ignorada: origem/porta diferente do destino.";continue;}
   for(int expected:accepted)if(Protocol.accepts(bytes,response.getLength(),command,expected,identifier))return;
   boolean own=false;if(identifier!=null&&Protocol.smartReply(bytes,response.getLength()))try{own=java.util.Arrays.equals(java.util.Arrays.copyOfRange(bytes,49,61),Protocol.identity(identifier));}catch(IllegalArgumentException ignored){}
   deviceReplied|=own;
   detail="Resposta não aceita: "+response.getLength()+" bytes";
   if(response.getLength()>=2)detail+=String.format(java.util.Locale.ROOT,", cabeçalho %02X %02X",bytes[0]&255,bytes[1]&255);
   if(response.getLength()>=5)detail+=String.format(java.util.Locale.ROOT,", código %02X (esperado %02X)",bytes[4]&255,accepted[0]);
   if(Protocol.smartReply(bytes,response.getLength()))detail+=own?", identificação confere.":", identificação não confere.";
  }
  throw new AckTimeout(deviceReplied,received,detail);
 }
}
