package br.com.lume;
import java.io.*;
import java.net.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;
public final class PreviewTest {
 static int count;static final String ID="A1B2C3D4E5F6";
 static void ok(boolean value,String reason){count++;if(!value)throw new AssertionError(reason);}
 static class Rig implements Preview.Wire,Preview.Clock,Preview.Wait,Preview.Listener {
  Preview session=new Preview();long now;int arms,resumes,frames;boolean failSend,failRestore,failArm,stopAfterTwo,burst;List<byte[]> sent=new ArrayList<>();List<Long> times=new ArrayList<>();
  Rig(){session.update(360,new int[]{10,20,30,40,50},0);}
  public long millis(){return now;}
  public void pause(long n){now+=n;if(burst&&n==200){session.update(500,new int[]{1,2,3,4,5},now);session.update(600,new int[]{42,43,44,45,46},now);}}
  public void arm()throws Exception{arms++;if(failArm)throw new IOException("disk full");}
  public void send(byte[] p)throws Exception{ok(arms==1,"recovery marker before all writes");if(failSend)throw new IOException("lost Wi-Fi");sent.add(p.clone());if(p[1]==2)times.add(now);}
  public void resume()throws Exception{resumes++;if(failRestore)throw new IOException("no ACK");}
  public void running(){}
  public void sent(int minute,int[] values){frames++;values[0]=99;if(stopAfterTwo&&frames==2)session.stop();}
  public void stopping(){}
  Preview.Result run(){return session.run("DIARIO",this,()->{},this,this,this);}
 }
 public static void main(String[] args)throws Exception{
  byte[] h=Protocol.frame(Protocol.previewStart("DIARIO"),ID);ok(h.length==64&&h[0]==0x55&&(h[1]&255)==0xaa&&h[2]==6&&h[3]==1&&h[5]==6&&new String(h,6,6,"US-ASCII").equals("DIARIO")&&h[12]==0,"vendor preview header has name at byte 6 and no profile slot");
  byte[] levels=Protocol.frame(Protocol.previewLevels(new int[]{0,1,50,99,100}),ID);ok(levels[2]==6&&levels[3]==2&&levels[5]==5&&levels[6]==0&&levels[7]==1&&levels[8]==50&&levels[9]==99&&levels[10]==100,"vendor preview intensities occupy bytes 6..10");
  for(byte[] frame:new byte[][]{h,levels}){int sum=0;for(int i=0;i<49;i++)sum+=frame[i]&255;ok((sum&255)==(frame[61]&255)&&new String(frame,49,12,"US-ASCII").equals(ID),"identity and checksum remain exact");}
  for(int[] bad:new int[][]{{0,0,0,0},{101,0,0,0,0},{-1,0,0,0,0}}){boolean rejected=false;try{Protocol.previewLevels(bad);}catch(IllegalArgumentException e){rejected=true;}ok(rejected,"reject invalid live intensities");}
  Rig r=new Rig();r.burst=true;r.stopAfterTwo=true;Preview.Result done=r.run();ok(done.attempted&&done.restored&&done.failure==null&&r.arms==1&&r.resumes==1,"one preview lifecycle and one resume");ok(r.sent.size()==4&&Arrays.equals(r.sent.get(0),r.sent.get(1)),"two identical start headers like vendor");ok(r.sent.get(3)[4]==42&&r.times.get(1)-r.times.get(0)==200,"coalesce bursts to latest at most 5 Hz");ok(r.sent.stream().allMatch(p->p[0]==6),"preview writes no erase, profile, clock or provisioning commands");
  Rig idle=new Rig();Preview.Result expired=idle.run();ok(expired.failure!=null&&expired.restored&&idle.frames==1,"missing UI heartbeat stops and resumes without repeated unchanged frames");
  Rig stopped=new Rig();stopped.session.stop();Preview.Result noStart=stopped.run();ok(!noStart.attempted&&stopped.arms==0&&stopped.sent.isEmpty()&&stopped.resumes==0,"cancel before start sends nothing");
  Rig disk=new Rig();disk.failArm=true;Preview.Result noMarker=disk.run();ok(!noMarker.attempted&&disk.sent.isEmpty()&&disk.resumes==0,"no light change when durable recovery information cannot be saved");
  Rig lost=new Rig();lost.failSend=true;Preview.Result failure=lost.run();ok(failure.attempted&&failure.restored&&lost.resumes==1,"attempt resume even after uncertain start send");
  Rig unconfirmed=new Rig();unconfirmed.failRestore=true;Preview.Result uncertain=unconfirmed.run();ok(uncertain.attempted&&!uncertain.restored&&uncertain.restoreFailure!=null&&unconfirmed.resumes==1,"no success claim or automatic retry on missing resume ACK");
  Rig old=new Rig();old.now=4000;Preview.Result stale=old.run();ok(!stale.attempted&&old.sent.isEmpty(),"reject stale initial sample before mutation");
  Rig copied=new Rig();int[] source={1,2,3,4,5};copied.session.update(10,source,0);source[0]=80;copied.run();ok(copied.sent.get(2)[4]==1,"mailbox and listener snapshots cannot mutate queued values");
  AtomicInteger guards=new AtomicInteger();Rig guarded=new Rig();Preview.Result paused=guarded.session.run("DIARIO",guarded,()->{if(guards.incrementAndGet()==2)throw new IOException("app paused");},guarded,guarded,guarded);ok(paused.restored&&guarded.sent.size()==1&&guarded.resumes==1,"backgrounding during startup skips further frames and still restores");
  udp();strictAck();System.out.println(count+" live preview checks passed (vendor fixtures, mailbox, lifecycle and loopback UDP)");
 }
 static void udp()throws Exception{
  InetAddress local=InetAddress.getLoopbackAddress();ExecutorService worker=Executors.newSingleThreadExecutor();
  try(DatagramSocket device=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){
   device.setSoTimeout(4000);List<Integer> commands=new ArrayList<>();Future<?> receiver=worker.submit(()->{try{for(;;){byte[] frame=new byte[65];DatagramPacket packet=new DatagramPacket(frame,65);device.receive(packet);if(packet.getLength()!=64)throw new AssertionError("wrong frame size");commands.add((frame[2]&255)*256+(frame[3]&255));if(frame[2]==3){byte[] ack=new byte[64];ack[0]=(byte)0x88;ack[4]=(byte)0x83;System.arraycopy(ID.getBytes("US-ASCII"),0,ack,49,12);device.send(new DatagramPacket(ack,64,packet.getAddress(),packet.getPort()));return;}}}catch(Exception e){throw new RuntimeException(e);}});
   Preview session=new Preview();session.update(720,new int[]{10,20,30,40,50},System.nanoTime()/1000000L);AtomicInteger sent=new AtomicInteger();
   Preview.Result result=session.run("DIARIO",new Preview.Wire(){public void arm(){}public void send(byte[] p)throws Exception{byte[] f=Protocol.frame(p,ID);client.send(new DatagramPacket(f,64,local,device.getLocalPort()));}public void resume()throws Exception{PacketIO.exchange(client,local,device.getLocalPort(),Protocol.frame(new byte[]{3,8},ID),3,new int[]{0x83},600,()->{},ID,true);}},()->{},()->System.nanoTime()/1000000L,n->Thread.sleep(n),new Preview.Listener(){public void running(){}public void sent(int m,int[]v){sent.incrementAndGet();session.stop();}public void stopping(){}});
   receiver.get(5,TimeUnit.SECONDS);ok(result.restored&&sent.get()==1&&commands.equals(Arrays.asList(0x0601,0x0601,0x0602,0x0308)),"real UDP preview sequence finishes only on Smart resume ACK");
  }finally{worker.shutdownNow();}
 }
 static void strictAck()throws Exception{
  InetAddress local=InetAddress.getLoopbackAddress();ExecutorService worker=Executors.newSingleThreadExecutor();
  try(DatagramSocket device=new DatagramSocket(0,local);DatagramSocket client=new DatagramSocket(0,local)){
   device.setSoTimeout(1000);Future<?> receive=worker.submit(()->{try{DatagramPacket packet=new DatagramPacket(new byte[64],64);device.receive(packet);byte[] legacy=new byte[64];legacy[0]=0x55;legacy[1]=(byte)0xaa;legacy[2]=3;legacy[4]=(byte)0x83;device.send(new DatagramPacket(legacy,64,packet.getAddress(),packet.getPort()));}catch(Exception e){throw new RuntimeException(e);}});
   boolean timedOut=false;try{PacketIO.exchange(client,local,device.getLocalPort(),Protocol.frame(new byte[]{3,8},ID),3,new int[]{0x83},80,()->{},ID,true);}catch(PacketIO.AckTimeout e){timedOut=true;}receive.get(2,TimeUnit.SECONDS);ok(timedOut,"legacy header cannot falsely confirm Smart preview restoration");
  }finally{worker.shutdownNow();}
 }
}
