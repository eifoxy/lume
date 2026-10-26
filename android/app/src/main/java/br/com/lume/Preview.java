package br.com.lume;

import java.io.IOException;
import java.util.Arrays;

/** One owner, latest-value mailbox, and bounded pacing. No profile/clock/erase commands. */
public final class Preview {
 public interface Clock {long millis();}
 public interface Wait {void pause(long millis)throws Exception;}
 public interface Wire {void arm()throws Exception;void send(byte[] payload)throws Exception;void resume()throws Exception;}
 public interface Guard {void check()throws Exception;}
 public interface Listener {void running();void sent(int minute,int[] levels);void stopping();}
 public static final class Sample {public final int minute;public final int[] levels;Sample(int minute,int[] values){this.minute=minute;levels=values.clone();}}
 public static final class Result {public boolean attempted,restored;public int headersSent,framesSent,lastMinute=-1;public Exception failure,restoreFailure;}
 private Sample latest;private long heartbeat;private volatile boolean stop;
 public synchronized void update(int minute,int[] levels,long now){if(minute<0||minute>1439)throw new IllegalArgumentException("Horário inválido");Protocol.previewLevels(levels);latest=new Sample(minute,levels);heartbeat=now;}
 public void stop(){stop=true;}
 public boolean stopping(){return stop;}
 private synchronized Sample sample(){return latest==null?null:new Sample(latest.minute,latest.levels);}
 private synchronized long age(long now){return now-heartbeat;}
 public Result run(String alias,Wire wire,Guard guard,Clock clock,Wait wait,Listener listener){
  Result result=new Result();
  try{
   byte[] header=Protocol.previewStart(alias);if(sample()==null)throw new IOException("Intensidades inválidas.");guard.check();if(stop)return result;if(age(clock.millis())>3000)throw new IOException("A prévia parou de receber atualizações do celular.");
   // Persist recovery information before any command which may enter manual mode.
   wire.arm();result.attempted=true;wire.send(header.clone());result.headersSent++;wait.pause(300);guard.check();
   if(!stop){wire.send(header.clone());result.headersSent++;listener.running();}
   int[] previous=null;
   while(!stop){guard.check();if(age(clock.millis())>3000)throw new IOException("A prévia parou de receber atualizações do celular.");Sample value=sample();
    if(!Arrays.equals(previous,value.levels)){wire.send(Protocol.previewLevels(value.levels));result.framesSent++;result.lastMinute=value.minute;previous=value.levels.clone();listener.sent(value.minute,value.levels.clone());}
    wait.pause(200);
   }
  }catch(Exception e){result.failure=e;}
  finally{if(result.attempted){try{listener.stopping();wire.resume();result.restored=true;}catch(Exception e){result.restoreFailure=e;}}}
  return result;
 }
}
