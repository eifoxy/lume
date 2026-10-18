using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Lume;

static class WifiTests
{
 public static int Run()
 {
  int checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
  void Reject(Action action,string message){bool caught=false;try{action();}catch(InvalidDataException){caught=true;}Check(caught,message);}
  const string id="A1B2C3D4E5F6";var ip=IPAddress.Parse("192.168.1.42");var local=new LocalRoute("Wi-Fi",IPAddress.Parse("192.168.1.10"),IPAddress.Parse("255.255.255.0"),IPAddress.Parse("192.168.1.255"));
  var discovery=WifiProtocol.Discovery();Check(discovery.Length==64&&discovery[61]==0xe3&&discovery.Take(9).SequenceEqual(new byte[]{0x55,0xaa,0x17,0,0,3,0xdd,0xee,0xff}),"Discovery fixture from Pled binary");
  Check(LocalRoute.CalculateBroadcast(IPAddress.Parse("10.2.3.4"),IPAddress.Parse("255.255.0.0")).Equals(IPAddress.Parse("10.2.255.255")),"Broadcast respects the interface mask");
  Check(local.Contains(ip)&&!local.Contains(IPAddress.Parse("192.168.2.42")),"Network route containment");
  Check(WifiProtocol.IsLocalAddress(ip)&&!WifiProtocol.IsLocalAddress(IPAddress.Parse("8.8.8.8"))&&!WifiProtocol.IsLocalAddress(IPAddress.IPv6Loopback),"Discovery only accepts private IPv4 addresses");
  byte[] response=new byte[64];response[0]=0x55;response[1]=0xaa;response[2]=0x17;Encoding.ASCII.GetBytes(id).CopyTo(response,4);
  var parsed=WifiProtocol.ParseDiscovery(response,new IPEndPoint(ip,5000),[local]);Check(parsed?.Identifier==id&&parsed.Adapter=="Wi-Fi","Valid discovery response parsed");
  Check(WifiProtocol.ParseDiscovery(response[..15],new IPEndPoint(ip,5000),[local]) is null,"Short packet rejected");Check(WifiProtocol.ParseDiscovery(response,new IPEndPoint(ip,1234),[local]) is null,"Wrong response port rejected");
  Check(WifiProtocol.ParseDiscovery(discovery,new IPEndPoint(ip,5000),[local]) is null,"Own broadcast request cannot be mistaken for a device");
  response[0]=0;Check(WifiProtocol.ParseDiscovery(response,new IPEndPoint(ip,5000),[local]) is null,"Invalid magic rejected");response[0]=0x55;
  Check(WifiProtocol.ParseDiscovery(response,new IPEndPoint(IPAddress.Parse("192.168.2.42"),5000),[local]) is null,"Off-subnet discovery response ignored");
  var command=WifiProtocol.Frame([3,8],id);Check(command.Length==64&&command[0]==0x55&&command[2]==3&&command[3]==8&&command[61]==10&&Encoding.ASCII.GetString(command,49,12)==id,"Wi-Fi command checksum and ID position");
  Check(WifiProtocol.Frame([3,8],id.ToLowerInvariant()).Skip(49).Take(12).SequenceEqual(Encoding.ASCII.GetBytes(id.ToLowerInvariant())),"Device ID bytes retain the received case");
  Reject(()=>WifiProtocol.Frame(new byte[48],id),"MAC metadata cannot be overwritten by oversized commands");Reject(()=>WifiProtocol.ValidateId("000000000000"),"Empty device identity rejected");
  var frames=WifiProtocol.Provisioning(id,"AQUARIO","dummy-password");
  Check(frames[0][2]==0x18&&frames[0][5]==7&&frames[0][6]==1&&frames[0][39]==2&&frames[0][40]==1&&Encoding.ASCII.GetString(frames[0],7,7)=="AQUARIO","SSID provisioning layout and WPA2 AES defaults");
  Check(frames[1][5]==14&&frames[1][6]==2&&Encoding.ASCII.GetString(frames[1],7,14)=="dummy-password","Password is sent in its own frame");
  Check(frames.All(p=>p[61]==unchecked((byte)p.Take(49).Sum(b=>(int)b))&&Encoding.ASCII.GetString(p,49,12)==id),"Provisioning checksum excludes the identity field");
  var legacy=WifiProtocol.Provisioning(id,"AQUARIO","dummy-password",false,false);Check(legacy[0][39]==1&&legacy[0][40]==2,"Legacy WPA and TKIP flags");
  Reject(()=>WifiProtocol.Provisioning(id,"", "dummy-password"),"Empty SSID rejected");Reject(()=>WifiProtocol.Provisioning(id,"Aquário","dummy-password"),"Non ASCII SSID not silently changed");Reject(()=>WifiProtocol.Provisioning(id,"AQUARIO",new string('x',33)),"Long passwords are not silently truncated");Reject(()=>WifiProtocol.Provisioning(id,"AQUARIO","short"),"Short password rejected");
  byte[] ack=[0x55,0xaa,2,0,0x85];Check(WifiProtocol.Accepts(ack,2,0x85)&&!WifiProtocol.Accepts(ack,1,0x85)&&!WifiProtocol.Accepts(ack,2,0x80),"Command responses require matching command and status");
  using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();bool stopped=false;try{WifiDiscovery.Search([local],null,new Progress<DiscoveryUpdate>(_=>{}),cancelled.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){stopped=true;}Check(stopped,"Cancelled scan does not open a network socket");}
  Reject(()=>new WifiSession(new WifiDevice(id,IPAddress.Parse("8.8.8.8"),"invalid")),"Control commands cannot target public IPs");
  Task.Run(Loopback).GetAwaiter().GetResult();Check(true,"Real local UDP sockets exchange discovery bytes without device access");
  Task.Run(ControllerLoopback).GetAwaiter().GetResult();Check(true,"Clock, both provisioning stages, batch replacement and resume exchange matching ACKs over loopback UDP");
  return checks;
 }
 static async Task Loopback()
 {
  using var receiver=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));using var sender=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(2));
  var endpoint=(IPEndPoint)receiver.Client.LocalEndPoint!;await sender.SendAsync(WifiProtocol.Discovery(),endpoint,limit.Token);var received=await receiver.ReceiveAsync(limit.Token);if(!received.Buffer.SequenceEqual(WifiProtocol.Discovery()))throw new Exception("UDP discovery bytes were altered");
 }
 static async Task ControllerLoopback()
 {
  using var fakeDevice=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));var client=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(8));
  using var session=new WifiSession(new WifiDevice("A1B2C3D4E5F6",IPAddress.Loopback,"Teste"),client,(IPEndPoint)fakeDevice.Client.LocalEndPoint!);
  int receivedCount=0;bool sawSsid=false,sawPassword=false,sawErase=false;
  var server=Task.Run(async()=>{
   for(int i=0;i<54;i++){
    var received=await fakeDevice.ReceiveAsync(limit.Token);receivedCount++;var b=received.Buffer;
    if(b.Length!=64||b[0]!=0x55||b[1]!=0xaa||Encoding.ASCII.GetString(b,49,12)!="A1B2C3D4E5F6")throw new Exception("Unexpected controller frame in UDP integration test");
    byte expected=b[2] switch{2=>0x85,3=>0x83,5=>0x82,0x18=>b[6]==1?(byte)0x74:(byte)0x72,1=>b[3]==1?(byte)0x80:(byte)0x81,_=>throw new Exception("Unexpected Wi-Fi command")};
    if(b[2]==0x18){if(b[6]==1)sawSsid=Encoding.ASCII.GetString(b,7,7)=="AQUARIO";else sawPassword=Encoding.ASCII.GetString(b,7,14)=="dummy-password";}
    if(b[2]==5)sawErase=true;
    if(i==0)await fakeDevice.SendAsync(new byte[]{0x55,0xaa,2,0,0},received.RemoteEndPoint,limit.Token);
    await fakeDevice.SendAsync(new byte[]{0x55,0xaa,b[2],0,expected},received.RemoteEndPoint,limit.Token);
   }
  },limit.Token);
  await session.Sync(new DateTime(2026,10,18,19,30,0),limit.Token);await session.Configure("AQUARIO","dummy-password",true,true,limit.Token);
  await session.Upload(UploadPlan.Create([Profile.Day("Teste")],true),new Progress<TransferProgress>(_=>{}),limit.Token);await session.Resume(limit.Token);await server;
  if(receivedCount!=54||!sawSsid||!sawPassword||!sawErase)throw new Exception("Incomplete Wi-Fi integration sequence");
 }
}
