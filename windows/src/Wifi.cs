using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
namespace Lume;

public interface IControllerSession : IDisposable
{
 Task Upload(UploadPlan plan,IProgress<TransferProgress> progress,CancellationToken token);
 Task Manual(Profile p,byte[] values,CancellationToken token);
 Task Resume(CancellationToken token);
 Task Sync(DateTime time,CancellationToken token);
}
public record WifiDevice(string Identifier,IPAddress Address,string Adapter)
{
 public string Label=>"KITELIGHT · "+Identifier[^6..]+" · "+Address;
 public string Display=>$"{Label}\nID {Identifier} · {Adapter}";
 public override string ToString()=>Display;
}
public record LocalRoute(string Adapter,IPAddress Address,IPAddress Mask,IPAddress Broadcast)
{
 public static IPAddress CalculateBroadcast(IPAddress address,IPAddress mask)
 {var a=address.GetAddressBytes();var m=mask.GetAddressBytes();if(a.Length!=4||m.Length!=4)throw new ArgumentException("Use uma rede IPv4.");return new IPAddress(a.Select((v,i)=>(byte)(v|~m[i])).ToArray());}
 public bool Contains(IPAddress address){var a=Address.GetAddressBytes();var m=Mask.GetAddressBytes();var b=address.GetAddressBytes();return b.Length==4&&a.Select((v,i)=>(v&m[i])==(b[i]&m[i])).All(v=>v);}
 public override string ToString()=>Adapter+" · "+Address;
 public static List<LocalRoute> List()
 {
  var routes=new List<LocalRoute>();
  foreach(var network in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)){
   foreach(var unicast in network.GetIPProperties().UnicastAddresses.Where(a=>a.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a.Address))){var mask=unicast.IPv4Mask;if(mask.Equals(IPAddress.Any))continue;routes.Add(new(network.Name,unicast.Address,mask,CalculateBroadcast(unicast.Address,mask)));}
  }return routes;
 }
}
public static class WifiProtocol
{
 public const int Port=5000;
 public static bool IsLocalAddress(IPAddress address)
 {
  var b=address.GetAddressBytes();return b.Length==4&&(b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31||b[0]==169&&b[1]==254);
 }
 public static string ValidateId(string id)
 {if(id.Length!=12||id.Any(c=>!Uri.IsHexDigit(c))||id.All(c=>c=='0')||id.All(c=>char.ToUpperInvariant(c)=='F'))throw new InvalidDataException("A identificação da controladora precisa conter 12 dígitos hexadecimais válidos.");return id;}
 public static byte[] Discovery()
 {var b=new byte[64];new byte[]{0x55,0xaa,0x17,0,0,3,0xdd,0xee,0xff}.CopyTo(b,0);b[61]=0xe3;return b;}
 public static byte[] Frame(byte[] payload,string identifier)
 {if(payload.Length<1||payload.Length>47)throw new InvalidDataException("Comando Wi-Fi fora do formato do Pled V2.2.");var b=new byte[64];b[0]=0x55;b[1]=0xaa;payload.CopyTo(b,2);Encoding.ASCII.GetBytes(ValidateId(identifier)).CopyTo(b,49);b[61]=unchecked((byte)(0xff+payload.Sum(v=>(int)v)));return b;}
 public static WifiDevice? ParseDiscovery(byte[] b,IPEndPoint sender,IReadOnlyList<LocalRoute> routes,bool allowLoopback=false)
 {
  if(b.Length<16||b.Length>64||b[0]!=0x55||b[1]!=0xaa||b[2]!=0x17||sender.Port!=Port||!IsLocalAddress(sender.Address)&&!(allowLoopback&&IPAddress.IsLoopback(sender.Address)))return null;
  string id=Encoding.ASCII.GetString(b,4,12);try{id=ValidateId(id);}catch(InvalidDataException){return null;}
  var route=routes.FirstOrDefault(r=>r.Contains(sender.Address));if(route is null&&!allowLoopback)return null;
  return new(id,sender.Address,route?.Adapter??"Teste local");
 }
 public static bool Accepts(byte[] response,byte command,byte expected)=>response.Length>=5&&response.Length<=64&&response[0]==0x55&&response[1]==0xaa&&response[2]==command&&response[4]==expected;
 public static byte[][] Provisioning(string id,string ssid,string password,bool wpa2=true,bool aes=true)
 {
  ValidateId(id);
  if(string.IsNullOrWhiteSpace(ssid)||ssid.Length>32||ssid.Any(c=>c<32||c>126))throw new InvalidDataException("Para este protocolo, use um nome de rede de 1 a 32 caracteres ASCII (sem acentos).");
  if(password.Length<8||password.Length>32||password.Any(c=>c<32||c>126))throw new InvalidDataException("Este modelo aceita senha de 8 a 32 caracteres ASCII. A senha não será cortada ou alterada.");
  byte[] first=new byte[64],second=new byte[64];first[0]=second[0]=0x55;first[1]=second[1]=0xaa;first[2]=second[2]=0x18;
  first[5]=(byte)ssid.Length;first[6]=1;second[5]=(byte)password.Length;second[6]=2;
  Encoding.ASCII.GetBytes(ssid).CopyTo(first,7);Encoding.ASCII.GetBytes(password).CopyTo(second,7);first[39]=wpa2?(byte)2:(byte)1;first[40]=aes?(byte)1:(byte)2;
  foreach(var packet in new[]{first,second}){Encoding.ASCII.GetBytes(id).CopyTo(packet,49);packet[61]=unchecked((byte)packet.Take(49).Sum(v=>(int)v));}
  return [first,second];
 }
}

public sealed class WifiSession : IControllerSession
{
 readonly WifiDevice device;readonly UdpClient socket;readonly IPEndPoint endpoint;bool disposed;
 public WifiSession(WifiDevice device){if(!WifiProtocol.IsLocalAddress(device.Address))throw new InvalidDataException("Selecione uma controladora IPv4 da rede local.");WifiProtocol.ValidateId(device.Identifier);this.device=device;endpoint=new(device.Address,WifiProtocol.Port);socket=OpenSocket();}
 internal WifiSession(WifiDevice device,UdpClient testSocket,IPEndPoint testEndpoint){this.device=device;socket=testSocket;endpoint=testEndpoint;}
 internal static UdpClient OpenSocket(IPAddress? local=null,int port=WifiProtocol.Port)
 {
  var client=new UdpClient(AddressFamily.InterNetwork);try{client.ExclusiveAddressUse=true;client.EnableBroadcast=true;client.Client.Bind(new IPEndPoint(local??IPAddress.Any,port));return client;}
  catch(SocketException ex){client.Dispose();throw new IOException(ex.SocketErrorCode==SocketError.AddressAlreadyInUse?"A porta UDP 5000 está ocupada. Feche o Pled ou outra janela de busca e tente novamente.":"O Windows não permitiu abrir a comunicação UDP local. Verifique a rede e as permissões do aplicativo.",ex);}
 }
 public async Task Send(byte[] payload,byte expected,CancellationToken token)
 {var frame=WifiProtocol.Frame(payload,device.Identifier);await Exchange(frame,payload[0],[expected],token);}
 async Task Exchange(byte[] frame,byte command,byte[] accepted,CancellationToken token)
 {
  ObjectDisposedException.ThrowIf(disposed,this);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(2500);
  try{
   await socket.SendAsync(frame,endpoint,timeout.Token);
   while(true){var reply=await socket.ReceiveAsync(timeout.Token);if(!reply.RemoteEndPoint.Address.Equals(endpoint.Address)||reply.RemoteEndPoint.Port!=endpoint.Port)continue;if(accepted.Any(expected=>WifiProtocol.Accepts(reply.Buffer,command,expected)))return;}
  }catch(OperationCanceledException)when(!token.IsCancellationRequested){throw new IOException("A controladora não confirmou o comando em 2,5 segundos. Confira se o PC e o aparelho estão na mesma rede e feche o Pled.");}
 }
 public Task Upload(UploadPlan plan,IProgress<TransferProgress> progress,CancellationToken token)=>Transfer.Run(plan,false,Send,progress,token);
 public Task Resume(CancellationToken token)=>Send([3,8],0x83,token);
 public Task Sync(DateTime time,CancellationToken token)=>Send(Protocol.Clock(time),0x85,token);
 public Task Manual(Profile p,byte[] values,CancellationToken token)=>throw new NotSupportedException("O ajuste instantâneo desta versão está disponível via USB. Perfis e relógio podem ser enviados por Wi-Fi.");
 public async Task Configure(string ssid,string password,bool wpa2,bool aes,CancellationToken token)
 {
  var frames=WifiProtocol.Provisioning(device.Identifier,ssid,password,wpa2,aes);
  try{await Exchange(frames[0],0x18,[0x70,0x74],token);await Exchange(frames[1],0x18,[0x72],token);}
  finally{foreach(var frame in frames)CryptographicOperations.ZeroMemory(frame);}
 }
 public void Dispose(){if(disposed)return;disposed=true;socket.Dispose();}
}

public record DiscoveryUpdate(int Percent,WifiDevice? Device,string Message);
public static class WifiDiscovery
{
 public static async Task<List<WifiDevice>> Search(IReadOnlyList<LocalRoute> routes,IPAddress? direct,IProgress<DiscoveryUpdate> progress,CancellationToken token)
 {
  token.ThrowIfCancellationRequested();
  if(routes.Count==0)throw new IOException("Nenhuma rede IPv4 ativa foi encontrada. Conecte o PC ao Wi-Fi do aparelho ou ao roteador e tente novamente.");
  if(direct is not null&&(!WifiProtocol.IsLocalAddress(direct)||!routes.Any(r=>r.Contains(direct))))throw new InvalidDataException("Informe um IPv4 privado da mesma rede do PC.");
  using var socket=WifiSession.OpenSocket();using var scan=CancellationTokenSource.CreateLinkedTokenSource(token);scan.CancelAfter(TimeSpan.FromSeconds(5));
  var found=new Dictionary<string,WifiDevice>(StringComparer.OrdinalIgnoreCase);var query=WifiProtocol.Discovery();
  var targets=direct is null?routes.Select(r=>r.Broadcast).Distinct().ToArray():new[]{direct};
  var timer=System.Diagnostics.Stopwatch.StartNew();int sends=0;Task<UdpReceiveResult>? pending=null;
  try{
   while(timer.Elapsed.TotalSeconds<5){
    scan.Token.ThrowIfCancellationRequested();
    if(sends<3&&timer.Elapsed.TotalSeconds>=sends){foreach(var target in targets)await socket.SendAsync(query,new IPEndPoint(target,WifiProtocol.Port),scan.Token);sends++;}
    pending??=socket.ReceiveAsync(scan.Token).AsTask();var completed=await Task.WhenAny(pending,Task.Delay(180,scan.Token));
    if(completed==pending){var received=await pending;pending=null;var device=WifiProtocol.ParseDiscovery(received.Buffer,received.RemoteEndPoint,routes);if(device is not null){found[device.Identifier]=device;progress.Report(new((int)Math.Min(99,timer.Elapsed.TotalSeconds*20),device,$"{found.Count} controladora(s) encontrada(s)…"));}}
    progress.Report(new((int)Math.Min(99,timer.Elapsed.TotalSeconds*20),null,$"Buscando em {routes.Count} endereço(s) de rede · {found.Count} encontrada(s)…"));
   }
  }catch(OperationCanceledException)when(!token.IsCancellationRequested){}
  finally{scan.Cancel();if(pending is not null)try{await pending;}catch(OperationCanceledException){}catch(SocketException){}catch(ObjectDisposedException){}}
  token.ThrowIfCancellationRequested();progress.Report(new(100,null,$"Busca concluída · {found.Count} controladora(s) encontrada(s)."));return found.Values.OrderBy(d=>d.Identifier).ToList();
 }
}
