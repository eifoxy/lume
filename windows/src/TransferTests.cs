using System.IO;
namespace Lume;
static class TransferTests
{
 public static int Run()
 {
  int checks=0;void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
  void Reject(Action action,string message){bool failed=false;try{action();}catch(InvalidDataException){failed=true;}Check(failed,message);}
  var profiles=new[]{Profile.Day("Iluminacao azul"),Profile.Day("Iluminacao branca")};
  var plan=UploadPlan.Create(profiles,true);
  Check(plan.Items.Count==2&&plan.Items[0].Slot==0&&plan.Items[1].Slot==1,"Batch retains sidebar order");
  Check(plan.Items.Select(i=>i.DeviceName).Distinct(StringComparer.OrdinalIgnoreCase).Count()==2,"Truncated device names must be unique");
  Check(profiles[0].Name=="Iluminacao azul"&&profiles[1].Name=="Iluminacao branca","Local names must not be renamed");
  profiles[0].Values[24][0]=7;Check(plan.Items[0].Snapshot.Values[24][0]==65,"Transfer must use a deep snapshot");
  var emptyName=UploadPlan.Create(new[]{Profile.Day("🌊")},true);Check(emptyName.Items[0].DeviceName=="PERFIL","Non ASCII device names receive a usable alias");
  Reject(()=>UploadPlan.Create([],true),"Empty batch rejected");Reject(()=>UploadPlan.Create(Enumerable.Range(0,17).Select(i=>Profile.Day()),true),"Oversized batch rejected");
  Reject(()=>UploadPlan.Create(profiles,true,1),"Replacing all must start at bank zero");
  var invalid=profiles[1].Clone();invalid.Values[3][2]=200;Reject(()=>UploadPlan.Create(new[]{profiles[0],invalid},true),"Invalid later profile rejected before erase");
  foreach(bool classic in new[]{false,true}){
   var sent=new List<(byte[] Packet,byte Ack)>();var updates=new List<TransferProgress>();
   Task Record(byte[] packet,byte ack,CancellationToken token){sent.Add((packet.ToArray(),ack));return Task.CompletedTask;}
   Transfer.Run(plan,classic,Record,new InlineProgress(updates.Add),CancellationToken.None).GetAwaiter().GetResult();
   Check(sent.Count==(classic?102:99),"Exact command count for two profiles");
   Check(sent[0].Packet.SequenceEqual(classic?Protocol.ClassicFrame(3):new byte[]{5,0xfe})&&sent[0].Ack==(classic?1:0x82),"Clear command and confirmation per protocol");
   Check(sent.Count(x=>classic?x.Packet[3]==3:x.Packet.SequenceEqual(new byte[]{5,0xfe}))==1,"Memory erased exactly once per batch");
   Check(updates[^1].Percent==100&&updates[^1].Completed==2,"Completion only reported after every command succeeds");
   Check(updates.Zip(updates.Skip(1)).All(p=>p.First.Percent<=p.Second.Percent),"Overall progress must be monotonic");
   if(classic){Check(sent[^1].Packet.SequenceEqual(Protocol.ClassicFrame(2)),"Classic finalization sent after all profiles");Check(sent[1].Packet[6]==0&&sent[51].Packet[6]==1,"Classic profiles use distinct consecutive banks");}
   else Check(sent[1].Packet[4]==0&&sent[50].Packet[4]==1,"Pled profiles use distinct consecutive banks");
  }
  var single=UploadPlan.Create([profiles[0]],false,7);var singlePackets=new List<byte[]>();
  Transfer.Run(single,false,(p,a,t)=>{singlePackets.Add(p);return Task.CompletedTask;},new InlineProgress(_=>{}),CancellationToken.None).GetAwaiter().GetResult();
  Check(singlePackets.Count==49&&singlePackets[0][4]==7&&!singlePackets.Any(p=>p.SequenceEqual(new byte[]{5,0xfe})),"Single-profile upload never clears all memory");
  int attempted=0;
  try {Transfer.Run(plan,false,(p,a,t)=>{attempted++;if(attempted==51)throw new IOException("disconnected");return Task.CompletedTask;},new InlineProgress(_=>{}),CancellationToken.None).GetAwaiter().GetResult();throw new Exception("Failure swallowed");}
  catch(TransferException ex){Check(attempted==51&&ex.Message.Contains("1/2")&&ex.Message.Contains("limpeza dos perfis antigos foi confirmada"),"Mid batch failure stops immediately and reports partial memory");}
  attempted=0;
  try{Transfer.Run(plan,false,(p,a,t)=>{attempted++;throw new IOException("timeout");},new InlineProgress(_=>{}),CancellationToken.None).GetAwaiter().GetResult();throw new Exception("Clear failure swallowed");}
  catch(TransferException ex){Check(attempted==1&&ex.Message.Contains("não foi confirmado"),"Unconfirmed erase prevents uploads and reports uncertain state");}
  attempted=0;var cancelled=new CancellationToken(true);
  try{Transfer.Run(plan,false,(p,a,t)=>{attempted++;return Task.CompletedTask;},new InlineProgress(_=>{}),cancelled).GetAwaiter().GetResult();throw new Exception("Cancellation ignored");}
  catch(TransferException){Check(attempted==0,"Cancellation before start sends no erase");}
  var tampered=UploadPlan.Create(profiles,true);tampered.Items[1].Snapshot.Values[5][0]=101;attempted=0;
  Reject(()=>Transfer.Run(tampered,false,(p,a,t)=>{attempted++;return Task.CompletedTask;},new InlineProgress(_=>{}),CancellationToken.None).GetAwaiter().GetResult(),"Every snapshot revalidated immediately before USB");Check(attempted==0,"Invalid second snapshot sends no command");
  return checks;
 }
 sealed class InlineProgress(Action<TransferProgress> action) : IProgress<TransferProgress> {public void Report(TransferProgress p)=>action(p);}
}
