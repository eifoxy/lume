package br.com.lume;
import java.net.*;
import java.io.*;
/** One UDP socket for sending and receiving; mutating packets are never retried. */
public final class PacketIO {
 public interface Guard { void check() throws Exception; }
 public static void exchange(DatagramSocket socket,InetAddress target,int port,byte[] frame,int command,int[] accepted,int timeoutMs,Guard guard)throws Exception{
  exchange(socket,target,port,frame,command,accepted,timeoutMs,guard,null);
 }
 public static void exchange(DatagramSocket socket,InetAddress target,int port,byte[] frame,int command,int[] accepted,int timeoutMs,Guard guard,String identifier)throws Exception{
  guard.check();socket.send(new DatagramPacket(frame,frame.length,target,port));
  long deadline=System.nanoTime()+timeoutMs*1000000L;
  while(System.nanoTime()<deadline){guard.check();socket.setSoTimeout((int)Math.max(1,Math.min(200,(deadline-System.nanoTime())/1000000L)));byte[] bytes=new byte[65];DatagramPacket response=new DatagramPacket(bytes,bytes.length);
   try{socket.receive(response);}catch(SocketTimeoutException e){continue;}
   if(response.getPort()!=port||!response.getAddress().equals(target))continue;
   for(int expected:accepted)if(Protocol.accepts(bytes,response.getLength(),command,expected,identifier))return;
  }
  throw new IOException("Sem confirmação do aparelho em 2,5 segundos. Confira a rede e se a controladora é compatível com o Wi-Fi do Pled V2.2.");
 }
}
