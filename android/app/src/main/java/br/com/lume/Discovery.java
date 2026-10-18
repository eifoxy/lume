package br.com.lume;

import java.net.*;
import java.util.*;

/** Read-only discovery. The vendor receiver does not require source port 5000. */
public final class Discovery {
 public static List<InetAddress> targets(InetAddress local,int prefix,List<InetAddress> gateways,InetAddress direct)throws Exception {
  if(!(local instanceof Inet4Address)||prefix<1||prefix>32)throw new IllegalArgumentException("Rede IPv4 inválida.");
  LinkedHashSet<InetAddress> result=new LinkedHashSet<>();
  if(direct!=null){if(!host(local,prefix,direct))throw new IllegalArgumentException("O IP precisa ser de outro aparelho na mesma rede Wi-Fi.");result.add(direct);return new ArrayList<>(result);}
  for(InetAddress gateway:gateways)if(host(local,prefix,gateway))result.add(gateway);
  if(prefix<=30)result.add(InetAddress.getByAddress(Protocol.broadcast(local.getAddress(),prefix)));
  result.add(InetAddress.getByAddress(new byte[]{-1,-1,-1,-1}));
  return new ArrayList<>(result);
 }
 public static boolean host(InetAddress local,int prefix,InetAddress remote){
  if(!(remote instanceof Inet4Address)||remote.isAnyLocalAddress()||remote.isLoopbackAddress()||remote.isMulticastAddress()||remote.equals(local)||!Protocol.sameSubnet(local.getAddress(),remote.getAddress(),prefix))return false;
  if(prefix<=30){byte[] broadcast=Protocol.broadcast(local.getAddress(),prefix);if(Arrays.equals(broadcast,remote.getAddress()))return false;byte[] network=new byte[4];for(int i=0;i<4;i++){int bits=Math.max(0,Math.min(8,prefix-i*8)),mask=(255<<(8-bits))&255;network[i]=(byte)(local.getAddress()[i]&mask);}if(Arrays.equals(network,remote.getAddress()))return false;}
  return true;
 }
 public static String device(DatagramPacket packet,InetAddress local,int prefix){
  if(!host(local,prefix,packet.getAddress()))return null;
  return Protocol.discovered(packet.getData(),packet.getLength());
 }
}
