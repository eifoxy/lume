using System.IO;
using System.Text;
namespace Lume;

public record UploadItem(int Slot,string LocalName,string DeviceName,Profile Snapshot);
public sealed class UploadPlan
{
 public const int MaximumProfiles=16;
 public bool ClearExisting {get;}
 public IReadOnlyList<UploadItem> Items {get;}
 UploadPlan(bool clear,List<UploadItem> items){ClearExisting=clear;Items=items.AsReadOnly();}
 public static UploadPlan Create(IEnumerable<Profile> source,bool clear,int firstSlot=0)
 {
  var originals=source.ToList();
  if(originals.Count==0||originals.Count>MaximumProfiles||firstSlot<0||firstSlot+originals.Count>MaximumProfiles)throw new InvalidDataException("Envie entre 1 e 16 perfis, dentro dos números 1 a 16. Nenhum perfil foi apagado.");
  if(clear&&firstSlot!=0)throw new InvalidDataException("A substituição completa precisa começar pelo perfil 1.");
  var items=new List<UploadItem>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var original in originals){
   if(original is null)throw new InvalidDataException("Há um perfil vazio na biblioteca.");original.Validate();var snapshot=original.Clone();
   string stem=Encoding.ASCII.GetString(Protocol.Name(snapshot)).Trim();if(stem.Length==0)stem="PERFIL";string name=stem;
   for(int suffix=2;!names.Add(name);suffix++){string tail=suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);name=stem[..Math.Min(stem.Length,7-tail.Length)]+tail;}
   snapshot.Name=name;items.Add(new UploadItem(firstSlot+items.Count,original.Name,name,snapshot));
  }
  return new UploadPlan(clear,items);
 }
}
public record TransferProgress(int Percent,int Completed,int Total,string Message);
public sealed class TransferException(string message,Exception inner) : IOException(message,inner);

public static class Transfer
{
 public static async Task Run(UploadPlan plan,bool classic,Func<byte[],byte,CancellationToken,Task> send,IProgress<TransferProgress> progress,CancellationToken token)
 {
  // Validate every snapshot before the first command, particularly before erasing.
  foreach(var item in plan.Items)item.Snapshot.Validate();
  int completed=0;bool eraseAttempted=false,erased=false;string phase="iniciar o envio";
  try{
   token.ThrowIfCancellationRequested();
   if(plan.ClearExisting){
    phase="limpar os perfis antigos";progress.Report(new(0,0,plan.Items.Count,"Apagando os perfis da controladora…"));eraseAttempted=true;
    await send(classic?Protocol.ClassicFrame(3):new byte[]{5,0xfe},classic?(byte)1:(byte)0x82,token);erased=true;
   }
   foreach(var item in plan.Items){
    token.ThrowIfCancellationRequested();phase=$"gravar o perfil {item.Slot+1} ({item.LocalName})";
    progress.Report(new(completed*100/plan.Items.Count,completed,plan.Items.Count,$"Perfil {completed+1}/{plan.Items.Count} · {item.LocalName} · iniciando"));
    await send(classic?Protocol.ClassicFrame(0x12,[(byte)item.Slot,48,..Protocol.Name(item.Snapshot)]):Protocol.Header(item.Snapshot,item.Slot),classic?(byte)1:(byte)0x80,token);
    for(int step=0;step<48;step++){
     token.ThrowIfCancellationRequested();
     var packet=classic?Protocol.ClassicFrame(0x13,[(byte)(step/2),(byte)(step%2*30),..item.Snapshot.Values[step].Select(v=>(byte)v),0]):Protocol.Step(item.Snapshot,step);
     await send(packet,classic?(byte)1:(byte)0x81,token);
     int percent=(completed*48+step+1)*99/(plan.Items.Count*48);
     progress.Report(new(percent,completed,plan.Items.Count,$"Perfil {completed+1}/{plan.Items.Count} · {item.LocalName} · horário {step+1}/48"));
    }
    if(classic)await send(Protocol.ClassicFrame(1),0,token);
    completed++;
   }
   if(classic){phase="finalizar a gravação";await send(Protocol.ClassicFrame(2),1,token);}
   progress.Report(new(100,completed,plan.Items.Count,$"{completed}/{plan.Items.Count} perfil(is) recebido(s) pela controladora."));
  }catch(Exception ex){
   string state=erased?"A limpeza dos perfis antigos foi confirmada. A memória pode estar incompleta.":eraseAttempted?"O comando de limpeza foi enviado, mas não foi confirmado. A memória pode ter sido apagada.":"O envio foi interrompido; um perfil em gravação pode estar incompleto.";
   throw new TransferException($"Falha ao {phase}.\n{completed}/{plan.Items.Count} perfis tiveram os horários recebidos.\n{state}\nReconecte e reenvie o conjunto completo antes de usá-lo.\n\n"+ex.Message,ex);
  }
 }
}
