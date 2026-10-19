package br.com.lume;

import java.net.*;
import java.io.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;

/** Fixtures derived from protocol fields, independently of the packet encoder. */
public final class SmartTimerTest {
 static int count;static final String ID="A1B2C3D4E5F6";
 static void ok(boolean b,String why){if(!b)throw new AssertionError(why);count++;}
 static byte[] ack(int code,String id)throws Exception{byte[] b=new byte[64];b[0]=(byte)0x88;b[4]=(byte)code;System.arraycopy(id.getBytes("US-ASCII"),0,b,49,12);return b;}
 static int u(byte b){return b&255;}
 public static void main(String[] args)throws Exception{
  byte[] found=new byte[64];found[0]=(byte)0x88;found[2]=0x17;System.arraycopy(ID.getBytes("US-ASCII"),0,found,4,12);
  ok(ID.equals(Protocol.discovered(found,64)),"88 00 discovery matches actual device header");
  found[1]=1;ok(Protocol.discovered(found,64)==null,"88 01 is not the known reply header");found[1]=0;
  ok(Protocol.discovered(found,63)==null,"Smart replies must be 64 bytes");
  byte[] reply=ack(0x85,ID);ok(Protocol.accepts(reply,64,2,0x85,ID),"Smart ACK uses code and device identity, not request magic");
  ok(!Protocol.accepts(reply,64,2,0x85,"B1B2C3D4E5F6"),"other controller ACK rejected");ok(!Protocol.accepts(reply,64,2,0x83,ID),"wrong status rejected");ok(!Protocol.accepts(reply,63,2,0x85,ID),"short Smart ACK rejected");ok(!Protocol.accepts(reply,64,2,0x85,null),"unknown identity cannot confirm a Smart command");
  byte[] h=Protocol.frame(Protocol.smartHeader(3,"DIARIO"),ID);ok(h[2]==1&&h[3]==1&&h[4]==0&&h[5]==6&&h[6]==3&&new String(h,7,6,"US-ASCII").equals("DIARIO")&&h[13]==0&&h[15]==0,"Smart name length and no spurious count byte");
  int sum=0;for(int i=0;i<49;i++)sum+=u(h[i]);ok(u(h[61])==(sum&255),"checksum sums bytes 0..48");
  byte[][] setup=Protocol.provision(ID,"TEST","password123",true,true);ok(setup[0][2]==24&&setup[0][5]==4&&setup[0][6]==1&&setup[0][39]==2&&setup[0][40]==1&&setup[1][6]==2,"protected Wi-Fi packet fields");
  for(byte[] b:setup){int checksum=0;for(int i=0;i<49;i++)checksum+=u(b[i]);ok(u(b[61])==(checksum&255),"SSID/password checksum");Arrays.fill(b,(byte)0);}
  int[][] rows=new int[48][5];for(int r=0;r<48;r++)rows[r]=new int[]{r%101,20,40,60,80};Upload.Item item=new Upload.Item(0,"DIARIO",rows);rows[0][0]=100;ok(item.values[0][0]==0,"plan snapshots values before writes");
  List<String> order=new ArrayList<>();AtomicInteger percent=new AtomicInteger();AtomicInteger pauses=new AtomicInteger();
  Upload.run(Arrays.asList(item),true,true,(p,code)->order.add((p[0]&255)+":"+(p.length>1?p[1]&255:0)+":"+code),()->{},(v,t)->percent.set(v),()->pauses.incrementAndGet());
  ok(order.size()==52&&order.get(0).equals("5:254:130")&&order.get(1).equals("1:1:128")&&order.get(50).equals("3:8:131")&&order.get(51).equals("4:10:145"),"clear, header, 48 points, per-profile finish and all-profile finish");ok(percent.get()==100&&pauses.get()==62,"success only after finalize, 48 pacing points plus post-erase settling");
  order.clear();Upload.run(Arrays.asList(item),false,false,(p,code)->order.add(""+(p[0]&255)),()->{},(v,t)->{},()->{throw new AssertionError("legacy must not pace");});ok(order.size()==49,"legacy transfer retains existing flow");
  AtomicInteger sent=new AtomicInteger();boolean failed=false;try{Upload.run(Arrays.asList(item),true,true,(p,code)->{sent.incrementAndGet();if(code==0x81)throw new IOException("simulated missing ACK");},()->{},(v,t)->{},()->{});}catch(IOException e){failed=e.getMessage().contains("0/1")&&e.getMessage().contains("limpeza foi confirmado");}ok(failed&&sent.get()==3,"first step failure stops immediately without retry/finalize");
  sent.set(0);failed=false;try{Upload.run(Arrays.asList(item),true,true,(p,code)->sent.incrementAndGet(),()->{throw new IOException("cancelled");},(v,t)->{},()->{});}catch(IOException e){failed=true;}ok(failed&&sent.get()==0,"cancel before clear does not write");
  sent.set(0);failed=false;try{Upload.run(Arrays.asList(item,new Upload.Item(0,"OUTRO",rows)),true,true,(p,code)->sent.incrementAndGet(),()->{},(v,t)->{},()->{});}catch(IllegalArgumentException e){failed=true;}ok(failed&&sent.get()==0,"invalid complete plan rejected before clear");
  percent.set(0);failed=false;try{Upload.run(Arrays.asList(item),false,true,(p,code)->{if(code==0x91)throw new IOException("final ACK lost");},()->{},(v,t)->percent.set(v),()->{});}catch(IOException e){failed=e.getMessage().contains("finalizar");}ok(failed&&percent.get()<100,"missing final ACK cannot report success");
  retryPolicy(item);postErase(item);integration(item);lossRecovery(item);postEraseUdp(item);System.out.println(count+" Smart WiFi Timer checks passed, including complete loopback transfers, sixth-point ACK loss and first-header loss after erase");
 }
 static void postErase(Upload.Item item)throws Exception{
  AtomicInteger ticks=new AtomicInteger(),clear=new AtomicInteger(),headers=new AtomicInteger(),points=new AtomicInteger();List<byte[]> copies=new ArrayList<>();
  Upload.run(Arrays.asList(item),true,true,(p,code)->{if(code==0x82)clear.incrementAndGet();if(code==0x80){ok(ticks.get()>=14,"settling completes before header");copies.add(p.clone());if(headers.incrementAndGet()==1)throw new PacketIO.AckTimeout(false,0,"first header ignored");}if(code==0x81)points.incrementAndGet();},()->{},(v,t)->{},()->ticks.incrementAndGet());
  ok(clear.get()==1&&headers.get()==2&&points.get()==48&&Arrays.equals(copies.get(0),copies.get(1)),"one clear, identical header recovery, all 48 points");
  clear.set(0);headers.set(0);ticks.set(0);boolean cancelled=false;
  try{Upload.run(Arrays.asList(item),true,true,(p,code)->{if(code==0x82)clear.incrementAndGet();if(code==0x80)headers.incrementAndGet();},()->{if(ticks.get()==3)throw new IOException("cancelled during settling");},(v,t)->{},()->ticks.incrementAndGet());}catch(IOException e){cancelled=e.getMessage().contains("aguardar a memória")&&e.getMessage().contains("limpeza foi confirmado");}
  ok(cancelled&&clear.get()==1&&headers.get()==0,"cancellation during settling sends no header or additional clear");
  headers.set(0);boolean failed=false;try{Upload.run(Arrays.asList(item),false,true,(p,code)->{if(code==0x80){headers.incrementAndGet();throw new PacketIO.AckTimeout(true,1,"unexpected status");}},()->{},(v,t)->{},()->{});}catch(IOException e){failed=true;}
  ok(failed&&headers.get()==1,"unexpected header status is not retried");
 }
 static void postEraseUdp(Upload.Item item)throws Exception{
  ExecutorService executor=Executors.newSingleThreadExecutor();InetAddress local=InetAddress.getLoopbackAddress();
  try(DatagramSocket controller=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){
   controller.setSoTimeout(5000);AtomicInteger cleared=new AtomicInteger(),headers=new AtomicInteger(),points=new AtomicInteger();
   Future<?> server=executor.submit(()->{try{boolean done=false;byte[] first=null;while(!done){DatagramPacket q=new DatagramPacket(new byte[65],65);controller.receive(q);byte[] b=q.getData();int code;
    if(b[2]==5){if(cleared.incrementAndGet()!=1)throw new AssertionError("erase repeated");code=0x82;}
    else if(b[2]==1&&b[3]==1){if(cleared.get()!=1)throw new AssertionError("header before erase");headers.incrementAndGet();if(first==null){first=Arrays.copyOf(b,64);continue;}if(!Arrays.equals(first,Arrays.copyOf(b,64)))throw new AssertionError("header changed");code=0x80;}
    else if(b[2]==1&&b[3]==2){if(headers.get()!=2||u(b[6])!=points.getAndIncrement())throw new AssertionError("points before header ACK/out of order");code=0x81;}
    else if(b[2]==3)code=0x83;else if(b[2]==4){code=0x91;done=true;}else throw new AssertionError("unexpected command");
    byte[] response=ack(code,ID);controller.send(new DatagramPacket(response,64,q.getAddress(),q.getPort()));
   }}catch(Exception e){throw new RuntimeException(e);}});
   Upload.Sender sender=(p,code)->PacketIO.exchange(client,local,controller.getLocalPort(),Protocol.frame(p,ID),p[0]&255,new int[]{code},250,()->{},ID);
   Upload.run(Arrays.asList(item),true,true,sender,()->{},(v,t)->{},()->{});server.get(5,TimeUnit.SECONDS);
   ok(cleared.get()==1&&headers.get()==2&&points.get()==48,"real UDP recovers missing first-header ACK after clear without erasing twice");
  }finally{executor.shutdownNow();}
 }
 static void retryPolicy(Upload.Item item)throws Exception{
  AtomicInteger attempts=new AtomicInteger(),sent=new AtomicInteger();List<byte[]> sixth=new ArrayList<>();
  Upload.run(Arrays.asList(item),false,true,(p,code)->{sent.incrementAndGet();if(code==0x81&&p[4]==5){sixth.add(p.clone());if(attempts.incrementAndGet()==1)throw new PacketIO.AckTimeout(false,0,"simulated lost ACK");}},()->{},(v,t)->{},()->{});
  ok(attempts.get()==2&&sixth.size()==2&&Arrays.equals(sixth.get(0),sixth.get(1))&&sent.get()==52,"sixth indexed point recovered using identical bytes, no restart");
  attempts.set(0);sent.set(0);boolean failed=false;
  try{Upload.run(Arrays.asList(item),false,true,(p,code)->{sent.incrementAndGet();if(code==0x81&&p[4]==5){attempts.incrementAndGet();throw new PacketIO.AckTimeout(false,0,"silent");}},()->{},(v,t)->{},()->{});}catch(IOException e){failed=e.getMessage().contains("horário 6");}
  ok(failed&&attempts.get()==3&&sent.get()==9,"three attempts maximum, no seventh point or finalization after failure");
  attempts.set(0);failed=false;try{Upload.run(Arrays.asList(item),false,true,(p,code)->{if(code==0x81){attempts.incrementAndGet();throw new PacketIO.AckTimeout(true,1,"device returned other status");}},()->{},(v,t)->{},()->{});}catch(IOException e){failed=true;}
  ok(failed&&attempts.get()==1,"device reply with unexpected status is not retried");
  for(boolean clear:new boolean[]{false,true}){sent.set(0);failed=false;try{Upload.run(Arrays.asList(item),clear,true,(p,code)->{sent.incrementAndGet();throw new PacketIO.AckTimeout(false,0,"timeout");},()->{},(v,t)->{},()->{});}catch(IOException e){failed=true;}ok(failed&&sent.get()==(clear?1:3),"clear sent once, initial header at most three attempts");}
 }
 static void lossRecovery(Upload.Item item)throws Exception{
  ExecutorService executor=Executors.newSingleThreadExecutor();InetAddress local=InetAddress.getLoopbackAddress();
  try(DatagramSocket controller=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){
   controller.setSoTimeout(5000);AtomicInteger rowSix=new AtomicInteger(),seen=new AtomicInteger();int[] rows=new int[48];
   Future<?> server=executor.submit(()->{try{boolean done=false;byte[] firstSixth=null;while(!done){DatagramPacket q=new DatagramPacket(new byte[65],65);controller.receive(q);seen.incrementAndGet();byte[] b=q.getData();int code;
    if(b[2]==1&&b[3]==1)code=0x80;
    else if(b[2]==1&&b[3]==2){int r=u(b[6]);if(r>=48)throw new AssertionError("row out of range");rows[r]++;code=0x81;if(r==5){rowSix.incrementAndGet();if(firstSixth==null){firstSixth=Arrays.copyOf(b,64);continue;}if(!Arrays.equals(firstSixth,Arrays.copyOf(b,64)))throw new AssertionError("retry changed point");}}
    else if(b[2]==3&&b[3]==8)code=0x83;
    else if(b[2]==4&&b[3]==10){code=0x91;done=true;}
    else throw new AssertionError("unexpected mutation");
    byte[] answer=ack(code,ID);controller.send(new DatagramPacket(answer,64,q.getAddress(),q.getPort()));
   }}catch(Exception e){throw new RuntimeException(e);}});
   Upload.Sender sender=(p,code)->PacketIO.exchange(client,local,controller.getLocalPort(),Protocol.frame(p,ID),p[0]&255,new int[]{code},250,()->{},ID);
   Upload.run(Arrays.asList(item),false,true,sender,()->{},(v,t)->{},()->{});server.get(5,TimeUnit.SECONDS);
   ok(seen.get()==52&&rowSix.get()==2&&rows[5]==2&&java.util.stream.IntStream.range(0,48).allMatch(r->rows[r]==(r==5?2:1)),"real UDP sixth-point ACK loss recovers and finalizes without reuploading other rows");
  }finally{executor.shutdownNow();}
 }
 static void integration(Upload.Item item)throws Exception{
  InetAddress local=InetAddress.getLoopbackAddress();ExecutorService executor=Executors.newSingleThreadExecutor();
  try(DatagramSocket controller=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){
   controller.setSoTimeout(3000);AtomicInteger seen=new AtomicInteger();
   Future<?> server=executor.submit(()->{try{
    for(int i=0;i<54;i++){
     DatagramPacket q=new DatagramPacket(new byte[65],65);controller.receive(q);byte[] b=q.getData();if(q.getLength()!=64||u(b[0])!=85||u(b[1])!=170||!new String(b,49,12,"US-ASCII").equals(ID))throw new AssertionError("frame metadata");
     int total=0;for(int j=0;j<49;j++)total+=u(b[j]);if(u(b[61])!=(total&255))throw new AssertionError("frame checksum");
     int code;if(i==0){if(b[2]!=2||b[5]!=7)throw new AssertionError("clock layout");code=0x85;}
     else if(i==1){if(b[2]!=24||b[6]!=1||b[39]!=2||b[40]!=1)throw new AssertionError("SSID layout");code=0x70;}
     else if(i==2){if(b[2]!=24||b[6]!=2)throw new AssertionError("password layout");code=0x72;}
     else if(i==3){if(b[2]!=1||b[3]!=1||b[5]!=6||b[6]!=0||b[15]!=0)throw new AssertionError("header layout");code=0x80;}
     else if(i<52){int r=i-4;if(b[2]!=1||b[3]!=2||b[5]!=8||u(b[6])!=r||u(b[7])!=r/2||u(b[8])!=r%2*30||u(b[9])!=r)throw new AssertionError("step order/data");code=0x81;}
     else if(i==52){if(b[2]!=3||b[3]!=8)throw new AssertionError("profile finish");code=0x83;}
     else{if(b[2]!=4||b[3]!=10)throw new AssertionError("all finish");code=0x91;}
     if(i==0){byte[] wrong=ack(code,"B1B2C3D4E5F6");controller.send(new DatagramPacket(wrong,64,q.getAddress(),q.getPort()));}
     byte[] answer=ack(code,ID);controller.send(new DatagramPacket(answer,64,q.getAddress(),q.getPort()));seen.incrementAndGet();
    }
   }catch(Exception e){throw new RuntimeException(e);}});
   Upload.Sender sender=(p,code)->PacketIO.exchange(client,local,controller.getLocalPort(),Protocol.frame(p,ID),p[0]&255,new int[]{code},1000,()->{},ID);
   sender.send(Protocol.clock(new GregorianCalendar(2026,9,19,1,2,3)),0x85);byte[][] setup=Protocol.provision(ID,"TEST","password123",true,true);try{PacketIO.exchange(client,local,controller.getLocalPort(),setup[0],24,new int[]{0x70},1000,()->{},ID);PacketIO.exchange(client,local,controller.getLocalPort(),setup[1],24,new int[]{0x72},1000,()->{},ID);}finally{for(byte[] b:setup)Arrays.fill(b,(byte)0);}
   Upload.run(Arrays.asList(item),false,true,sender,()->{},(v,t)->{},()->{});server.get(5,TimeUnit.SECONDS);ok(seen.get()==54,"clock, provisioning and 48-point upload exchange real 88 00 ACKs with ID validation");
  }finally{executor.shutdownNow();}
 }
}
