using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
namespace Lume;
public static class Protocol
{
 public static byte[] Frame(byte[] payload,int length=65)
 {
  if(payload.Length>60||length!=65)throw new InvalidDataException("Relatório HID incompatível: esperado 65 bytes.");
  byte[] b=new byte[length];b[1]=0x55;b[2]=0xaa;payload.CopyTo(b,3);b[64]=unchecked((byte)(0xff+payload.Sum(x=>(int)x)));return b;
 }
 public static byte[] Header(Profile p,int slot) {p.Validate();if(slot<0||slot>15)throw new ArgumentOutOfRangeException(nameof(slot));var b=new byte[14];b[0]=1;b[1]=1;b[3]=10;b[4]=(byte)slot;Name(p).CopyTo(b,5);b[13]=48;return b;}
 public static byte[] Name(Profile p)=>Encoding.ASCII.GetBytes(new string(p.Name.Normalize(NormalizationForm.FormD).Where(c=>c<128&&char.IsLetterOrDigit(c)).Take(7).ToArray()).PadRight(7,' '));
 public static byte[] Step(Profile p,int i)=>[1,2,0,8,(byte)i,(byte)(i/2),(byte)(i%2*30),..p.Values[i].Select(x=>(byte)x)];
 public static byte[] Play(Profile p,int slot)=>[6,1,0,10,(byte)slot,..Name(p)];
 public static byte[] Clock(DateTime t)=>[2,0,0,7,(byte)(t.Year>>8),(byte)t.Year,(byte)t.Month,(byte)t.Day,(byte)t.Hour,(byte)t.Minute,(byte)t.Second];
 // Classic TC420 uses a different frame from the supplied Pled V2.2 binary.
 public static byte[] ClassicFrame(byte command,params byte[] data)
 {
  if(data.Length>56)throw new InvalidDataException("Pacote clássico grande demais.");var b=new byte[65];b[1]=0x55;b[2]=0xaa;b[3]=command;b[5]=(byte)data.Length;data.CopyTo(b,6);b[62]=unchecked((byte)b.Take(62).Sum(v=>(int)v));b[63]=13;b[64]=10;return b;
 }
}
public record Device(string Path,int InputLength,int OutputLength)
{
 public bool Classic=>Path.Contains("vid_0888&pid_4000",StringComparison.OrdinalIgnoreCase);
 public string Label=>Classic?"TC420 clássica":"Controladora Pled V2.2";
}
public sealed class Usb : IControllerSession
{
 readonly FileStream read,write;
 readonly bool classic;
 public Usb(Device d)
 {
  classic=d.Classic;
  if(d.InputLength!=65||d.OutputLength!=65)throw new IOException("A controladora encontrada tem um formato HID diferente do Pled V2.2.");
  var r=Native.CreateFile(d.Path,0x80000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero);
  if(r.IsInvalid){r.Dispose();throw new IOException("Não foi possível abrir a TC420. Feche o Pled e tente novamente.");}
  try {read=new FileStream(r,FileAccess.Read,65,true);var w=Native.CreateFile(d.Path,0x40000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero);if(w.IsInvalid){w.Dispose();throw new IOException("Não foi possível abrir a saída USB.");}write=new FileStream(w,FileAccess.Write,65,true);}
  catch {r.Dispose();throw;}
 }
 public async Task Send(byte[] payload,byte expected,CancellationToken token)
 {
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(2200);
  try {
   var b=classic?payload:Protocol.Frame(payload);await write.WriteAsync(b,timeout.Token);await write.FlushAsync(timeout.Token);
   if(expected==0)return;
   var response=new byte[65];int n=await read.ReadAsync(response,timeout.Token);
   bool accepted=classic?n>=7&&response[4]==0&&response[5]==1&&response[6]==0:n>=3&&response[0]==0&&response[2]==expected;
   if(!accepted)throw new IOException("Resposta USB inesperada. A operação foi interrompida.");
  } catch(OperationCanceledException) when(!token.IsCancellationRequested) {throw new IOException("A TC420 não respondeu em 2,2 segundos. Verifique o cabo e feche o Pled.");}
 }
 public Task Upload(UploadPlan plan,IProgress<TransferProgress> progress,CancellationToken token)=>Transfer.Run(plan,classic,Send,progress,token);
 public async Task Manual(Profile p,byte[] values,CancellationToken token)
 {
  if(values.Length!=5||values.Any(v=>v>100))throw new InvalidDataException("Intensidades inválidas.");
  if(classic){await Send(Protocol.ClassicFrame(0x15,[..Protocol.Name(p),0,0x7f]),1,token);await Send(Protocol.ClassicFrame(0x16,[0xf5,..values,0]),1,token);}
  else{await Send(Protocol.Play(p,0),0x84,token);await Send([6,2,0,5,..values],0x84,token);}
 }
 public Task Resume(CancellationToken token)=>classic?Send(Protocol.ClassicFrame(2),1,token):Send([3,8],0x83,token);
 public Task Sync(DateTime t,CancellationToken token)=>classic?Send(Protocol.ClassicFrame(0x11,Protocol.Clock(t)[4..]),1,token):Send(Protocol.Clock(t),0x85,token);
 public void Dispose(){read.Dispose();write.Dispose();}
 public static List<Device> Find()
 {
  var devices=new List<Device>();Native.HidD_GetHidGuid(out var guid);var set=Native.SetupDiGetClassDevs(ref guid,IntPtr.Zero,IntPtr.Zero,0x12);
  if(set==new IntPtr(-1))throw new IOException("Falha ao consultar os dispositivos USB do Windows.");
  try {
   for(uint i=0;;i++){
    var data=new Native.Interface {Size=Marshal.SizeOf<Native.Interface>()};if(!Native.SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref data)){if(Marshal.GetLastWin32Error()!=259)throw new IOException("Falha ao enumerar interfaces HID.");break;}
    Native.SetupDiGetDeviceInterfaceDetail(set,ref data,IntPtr.Zero,0,out uint size,IntPtr.Zero);if(size<6||size>32768)continue;
    var detail=Marshal.AllocHGlobal((int)size);
    try {Marshal.WriteInt32(detail,IntPtr.Size==8?8:6);if(!Native.SetupDiGetDeviceInterfaceDetail(set,ref data,detail,size,out _,IntPtr.Zero))continue;
     var path=Marshal.PtrToStringUni(detail+4)!;if(!path.Contains("vid_0483&pid_5750",StringComparison.OrdinalIgnoreCase)&&!path.Contains("vid_0888&pid_4000",StringComparison.OrdinalIgnoreCase))continue;
     using var h=Native.CreateFile(path,0,3,IntPtr.Zero,3,0,IntPtr.Zero);if(h.IsInvalid)continue;
     if(Native.HidD_GetPreparsedData(h,out var prep))try {var caps=new Native.Caps {Reserved=new ushort[17]};if(Native.HidP_GetCaps(prep,ref caps)>=0)devices.Add(new Device(path,caps.Input,caps.Output));}finally{Native.HidD_FreePreparsedData(prep);}
    }finally{Marshal.FreeHGlobal(detail);}
   }
  }finally{Native.SetupDiDestroyDeviceInfoList(set);}return devices;
 }
}
static class Native
{
 [StructLayout(LayoutKind.Sequential)]public struct Interface {public int Size;public Guid ClassGuid;public int Flags;public IntPtr Reserved;}
 [StructLayout(LayoutKind.Sequential)]public struct Caps {public ushort Usage,UsagePage,Input,Output,Feature;[MarshalAs(UnmanagedType.ByValArray,SizeConst=17)]public ushort[] Reserved;public ushort LinkNodes,InputButtons,InputValues,InputIndices,OutputButtons,OutputValues,OutputIndices,FeatureButtons,FeatureValues,FeatureIndices;}
 [DllImport("hid.dll")]public static extern void HidD_GetHidGuid(out Guid g);
 [DllImport("hid.dll")]public static extern bool HidD_GetPreparsedData(SafeFileHandle h,out IntPtr p);
 [DllImport("hid.dll")]public static extern bool HidD_FreePreparsedData(IntPtr p);
 [DllImport("hid.dll")]public static extern int HidP_GetCaps(IntPtr p,ref Caps c);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern IntPtr SetupDiGetClassDevs(ref Guid g,IntPtr e,IntPtr w,uint flags);
 [DllImport("setupapi.dll",SetLastError=true)]public static extern bool SetupDiEnumDeviceInterfaces(IntPtr set,IntPtr dev,ref Guid g,uint index,ref Interface data);
 [DllImport("setupapi.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set,ref Interface data,IntPtr detail,uint size,out uint needed,IntPtr dev);
 [DllImport("setupapi.dll")]public static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]public static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr sa,uint creation,uint flags,IntPtr template);
}
