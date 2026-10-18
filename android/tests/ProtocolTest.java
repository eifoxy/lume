package br.com.lume;
import java.util.*;
import java.net.*;
import java.io.*;
import java.util.concurrent.*;
public final class ProtocolTest {
 static int count;
 static void ok(boolean v,String message){if(!v)throw new AssertionError(message);count++;}
 static void rejects(Runnable r){boolean rejected=false;try{r.run();}catch(IllegalArgumentException e){rejected=true;}ok(rejected,"invalid input accepted");}
 public static void main(String[] args)throws Exception{
  String id="a1B2c3D4e5F6";byte[] d=Protocol.discovery();ok(d.length==64&&d[2]==0x17&&(d[61]&255)==0xe3,"discovery");
  byte[] f=Protocol.frame(new byte[]{5,(byte)0xfe},id);ok((f[61]&255)==2,"control checksum");ok(new String(f,49,12,"US-ASCII").equals(id),"ID preserves case");
  byte[] response=new byte[64];response[0]=0x55;response[1]=(byte)0xaa;response[2]=0x17;System.arraycopy(id.getBytes("US-ASCII"),0,response,4,12);ok(id.equals(Protocol.discovered(response,64)),"discovery parse");ok(Protocol.discovered(response,65)==null,"oversized discovery");
  rejects(()->Protocol.id("000000000000"));rejects(()->Protocol.id("ffffffffffff"));rejects(()->Protocol.id("hello"));rejects(()->Protocol.frame(new byte[48],id));
  byte[][] setup=Protocol.provision(id,"Aquario","password123",true,true);ok(setup[0][5]==7&&setup[0][6]==1&&setup[0][39]==2&&setup[0][40]==1,"ssid settings");ok(setup[1][5]==11&&setup[1][6]==2&&setup[1][39]==0,"password settings");for(byte[] b:setup){int sum=0;for(int i=0;i<49;i++)sum+=b[i]&255;ok((b[61]&255)==(sum&255),"provision checksum");ok(new String(b,49,12,"US-ASCII").equals(id),"provision identity");}
  rejects(()->Protocol.provision(id,"Água","password123",true,true));rejects(()->Protocol.provision(id,"Casa","123",true,true));rejects(()->Protocol.provision(id,"Casa",String.join("",Collections.nCopies(33,"a")),true,true));
  byte[] h=Protocol.header(15,"DIARIO");ok(h.length==14&&h[4]==15&&h[13]==48&&h[12]==0,"header");
  byte[] row=Protocol.step(47,new int[]{100,0,25,50,75});ok(row[4]==47&&row[5]==23&&row[6]==30&&row[7]==100&&row[11]==75,"last step");rejects(()->Protocol.step(48,new int[5]));rejects(()->Protocol.step(0,new int[]{101,0,0,0,0}));
  List<String> aliases=Protocol.aliases(Arrays.asList("Áquário longo","Aquario longo","!!!","aquario longo"));ok(new HashSet<>(aliases).size()==4&&aliases.get(1).equals("Aquari2")&&aliases.get(2).equals("PERFIL"),"unique aliases");
  Calendar clock=new GregorianCalendar(2026,9,18,23,59,58);byte[] c=Protocol.clock(clock);ok((c[4]&255)==7&&(c[5]&255)==234&&c[6]==10&&c[10]==58,"local clock");
  byte[] ip={(byte)192,(byte)168,4,7},same={(byte)192,(byte)168,4,44},other={(byte)192,(byte)168,5,7};ok(Protocol.sameSubnet(ip,same,24),"subnet match");ok(!Protocol.sameSubnet(ip,other,24),"foreign subnet");ok((Protocol.broadcast(ip,24)[3]&255)==255,"broadcast");
  byte[] ack={0x55,(byte)0xaa,1,0,(byte)0x81};ok(Protocol.accepts(ack,5,1,0x81),"ack");ok(!Protocol.accepts(ack,4,1,0x81)&&!Protocol.accepts(ack,5,2,0x81)&&!Protocol.accepts(ack,5,1,0x80),"invalid ack");
  InetAddress local=InetAddress.getLoopbackAddress();try(DatagramSocket server=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){ExecutorService executor=Executors.newSingleThreadExecutor();try{Future<?> emulated=executor.submit(()->{try{byte[] packet=new byte[64];DatagramPacket q=new DatagramPacket(packet,64);server.receive(q);if(q.getLength()!=64||packet[2]!=5)throw new AssertionError("wrong command");byte[] wrong={0x55,(byte)0xaa,5,0,(byte)0x81},right={0x55,(byte)0xaa,5,0,(byte)0x82};server.send(new DatagramPacket(wrong,wrong.length,q.getAddress(),q.getPort()));server.send(new DatagramPacket(right,right.length,q.getAddress(),q.getPort()));}catch(IOException e){throw new RuntimeException(e);}});PacketIO.exchange(client,local,server.getLocalPort(),f,5,new int[]{0x82},500,()->{});emulated.get(2,TimeUnit.SECONDS);ok(true,"real UDP accepts only matching ack");boolean timedOut=false;try{PacketIO.exchange(client,local,server.getLocalPort(),f,5,new int[]{0x82},100,()->{});}catch(IOException e){timedOut=true;}ok(timedOut,"timeout");server.setSoTimeout(100);DatagramPacket one=new DatagramPacket(new byte[64],64);server.receive(one);boolean retried=false;try{server.receive(one);retried=true;}catch(SocketTimeoutException expected){}ok(!retried,"no mutating retries");}finally{executor.shutdownNow();}}
  InetAddress phone=InetAddress.getByName("192.168.4.2"),gateway=InetAddress.getByName("192.168.4.1"),foreign=InetAddress.getByName("192.168.5.1");
  List<InetAddress> destinations=Discovery.targets(phone,24,Arrays.asList(gateway,gateway,foreign),null);
  ok(destinations.size()==3&&destinations.get(0).equals(gateway)&&destinations.get(1).getHostAddress().equals("192.168.4.255")&&destinations.get(2).getHostAddress().equals("255.255.255.255"),"AP gateway first, deduplicated and bounded broadcasts");
  ok(Discovery.targets(phone,24,Arrays.asList(gateway),gateway).equals(Arrays.asList(gateway)),"direct lookup excludes broadcasts");
  boolean rejectedDirect=false;try{Discovery.targets(phone,24,Arrays.asList(gateway),foreign);}catch(IllegalArgumentException e){rejectedDirect=true;}ok(rejectedDirect,"direct lookup rejects foreign subnet");
  ok(!Discovery.host(phone,24,phone)&&!Discovery.host(phone,24,InetAddress.getByName("192.168.4.255"))&&!Discovery.host(phone,24,InetAddress.getByName("192.168.4.0")),"self and broadcast/network are not devices");
  DatagramPacket discovered=new DatagramPacket(response,response.length,gateway,5000);ok(id.equals(Discovery.device(discovered,phone,24)),"standard discovery source port");
  discovered.setPort(49152);ok(id.equals(Discovery.device(discovered,phone,24)),"discovery supports varying source port like original receiver");
  discovered.setAddress(foreign);ok(Discovery.device(discovered,phone,24)==null,"discovery rejects off-subnet packets");discovered.setAddress(phone);ok(Discovery.device(discovered,phone,24)==null,"discovery rejects own echo");
  DatagramPacket queryEcho=new DatagramPacket(Protocol.discovery(),64,gateway,5000);ok(Discovery.device(queryEcho,phone,24)==null,"query is not a device reply");
  response[2]=1;discovered.setAddress(gateway);ok(Discovery.device(discovered,phone,24)==null,"other command is not discovery");response[2]=0x17;
  response[0]=0;ok(Discovery.device(discovered,phone,24)==null,"bad magic rejected independent of port");response[0]=0x55;
  ok(Discovery.targets(phone,24,Collections.emptyList(),null).size()==2,"no gateway still searches broadcasts");
  byte[] rawId={(byte)0x80,0x01,0x2a,0x00,(byte)0xf1,0x32,0x11,(byte)0xaa,0x7f,0x23,(byte)0xe0,0x09};
  byte[] rawReply=new byte[64];rawReply[0]=0x55;rawReply[1]=(byte)0xaa;rawReply[2]=0x17;System.arraycopy(rawId,0,rawReply,4,12);
  String rawIdentity=Protocol.discovered(rawReply,rawReply.length);ok(rawIdentity!=null&&rawIdentity.startsWith("raw:")&&Arrays.equals(Protocol.identity(rawIdentity),rawId),"opaque binary identity is discovered without ASCII loss");
  ok(Arrays.equals(Arrays.copyOfRange(Protocol.frame(new byte[]{3,8},rawIdentity),49,61),rawId),"control frame preserves exact binary identity bytes");
  byte[][] rawSetup=Protocol.provision(rawIdentity,"TEST-SSID","password123",true,true);ok(Arrays.equals(Arrays.copyOfRange(rawSetup[0],49,61),rawId)&&Arrays.equals(Arrays.copyOfRange(rawSetup[1],49,61),rawId),"both provisioning stages preserve binary identity");
  byte[] textId="KITELIGHT123".getBytes("US-ASCII");System.arraycopy(textId,0,rawReply,4,12);String nonHex=Protocol.discovered(rawReply,64);ok(nonHex!=null&&Arrays.equals(Protocol.identity(nonHex),textId),"non-hex ASCII identity supported exactly");
  Arrays.fill(rawReply,4,16,(byte)0);ok(Protocol.discovered(rawReply,64)==null&&Protocol.discoveryRejection(rawReply,64).contains("vazia"),"zero identity rejected with reason");
  Arrays.fill(rawReply,4,16,(byte)0xff);ok(Protocol.discovered(rawReply,64)==null,"all-FF binary identity rejected");
  rejects(()->Protocol.identity("raw:abcd"));rejects(()->Protocol.identity("raw:000000000000000000000000"));
  ok(Protocol.discovered(null,64)==null&&Protocol.discovered(new byte[4],64)==null,"malformed lengths cannot crash parser");
  byte[] echo=Protocol.discovery();echo[63]=1;ok(Protocol.discovered(echo,64)==null&&Protocol.discoveryRejection(echo,64).contains("eco"),"request identity never becomes discovered device even with different padding");
  System.arraycopy(rawId,0,rawReply,4,12);rawReply[0]=0x12;rawReply[1]=0x34;ok(Protocol.discovered(rawReply,64)==null&&Protocol.discoveryRejection(rawReply,64).contains("12 34"),"unsupported header reported precisely");
  System.out.println(count+" checks passed (including real loopback UDP)");
 }
}
