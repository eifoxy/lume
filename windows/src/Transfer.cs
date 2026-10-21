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
  if(originals.Count==0||originals.Count>MaximumProfiles||firstSlot<0||firstSlot+originals.Count>MaximumProfiles)throw new InvalidDataException(I18n.T("Envie entre 1 e 16 perfis, dentro dos n\u00FAmeros 1 a 16. Nenhum perfil foi apagado."));
  if(clear&&firstSlot!=0)throw new InvalidDataException(I18n.T("A substitui\u00E7\u00E3o completa precisa come\u00E7ar pelo perfil 1."));
  var items=new List<UploadItem>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var original in originals){
   if(original is null)throw new InvalidDataException(I18n.T("H\u00E1 um perfil vazio na biblioteca."));original.Validate();var snapshot=original.Clone();
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
  int completed=0;bool eraseAttempted=false,erased=false;string phase=I18n.T("iniciar o envio");
  try{
   token.ThrowIfCancellationRequested();
   if(plan.ClearExisting){
    phase=I18n.T("limpar os perfis antigos");progress.Report(new(0,0,plan.Items.Count,I18n.T("Apagando os perfis da controladora\u2026")));eraseAttempted=true;
    await send(classic?Protocol.ClassicFrame(3):new byte[]{5,0xfe},classic?(byte)1:(byte)0x82,token);erased=true;
   }
   foreach(var item in plan.Items){
    token.ThrowIfCancellationRequested();phase=$"{I18n.T("gravar o perfil ")}{item.Slot+1} ({item.LocalName})";
    progress.Report(new(completed*100/plan.Items.Count,completed,plan.Items.Count,$"{I18n.T("Perfil ")}{completed+1}/{plan.Items.Count} · {item.LocalName}{I18n.T(" \u00B7 iniciando")}"));
    await send(classic?Protocol.ClassicFrame(0x12,[(byte)item.Slot,48,..Protocol.Name(item.Snapshot)]):Protocol.Header(item.Snapshot,item.Slot),classic?(byte)1:(byte)0x80,token);
    for(int step=0;step<48;step++){
     token.ThrowIfCancellationRequested();
     var packet=classic?Protocol.ClassicFrame(0x13,[(byte)(step/2),(byte)(step%2*30),..item.Snapshot.Values[step].Select(v=>(byte)v),0]):Protocol.Step(item.Snapshot,step);
     await send(packet,classic?(byte)1:(byte)0x81,token);
     int percent=(completed*48+step+1)*99/(plan.Items.Count*48);
     progress.Report(new(percent,completed,plan.Items.Count,$"{I18n.T("Perfil ")}{completed+1}/{plan.Items.Count} · {item.LocalName}{I18n.T(" \u00B7 hor\u00E1rio ")}{step+1}/48"));
    }
    if(classic)await send(Protocol.ClassicFrame(1),0,token);
    completed++;
   }
   if(classic){phase=I18n.T("finalizar a grava\u00E7\u00E3o");await send(Protocol.ClassicFrame(2),1,token);}
   progress.Report(new(100,completed,plan.Items.Count,$"{completed}/{plan.Items.Count}{I18n.T(" perfil(is) recebido(s) pela controladora.")}"));
  }catch(Exception ex){
   string state=erased?I18n.T("A limpeza dos perfis antigos foi confirmada. A mem\u00F3ria pode estar incompleta."):eraseAttempted?I18n.T("O comando de limpeza foi enviado, mas n\u00E3o foi confirmado. A mem\u00F3ria pode ter sido apagada."):I18n.T("O envio foi interrompido; um perfil em grava\u00E7\u00E3o pode estar incompleto.");
   throw new TransferException($"{I18n.T("Falha ao ")}{phase}.\n{completed}/{plan.Items.Count}{I18n.T(" perfis tiveram os hor\u00E1rios recebidos.\n")}{state}{I18n.T("\nReconecte e reenvie o conjunto completo antes de us\u00E1-lo.\n\n")}"+ex.Message,ex);
  }
 }
}
