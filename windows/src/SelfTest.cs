using System.IO;
namespace Lume;
public static class SelfTest
{
 public static void Run()
 {
  int count=0;void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
  void Reject(Action action,string message){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,message);}
  var p=Profile.Day();p.Values[0]=[0,1,50,99,100];
  var raw=Legacy.Encode(p);Check(raw.Length==960,"LMF length");Check(raw[4]==0x10&&raw[8]==0x23&&raw[12]==0x36&&raw[16]==0x46,"Nibble encoding fixture");
  string temp=Path.Combine(AppContext.BaseDirectory,"selftest-"+Guid.NewGuid());Directory.CreateDirectory(temp);
  try{
   string file=Path.Combine(temp,"test.lmf");File.WriteAllBytes(file,raw);var back=Legacy.Read(file);Check(back.Values.Zip(p.Values).All(x=>x.First.SequenceEqual(x.Second)),"LMF roundtrip all channels");
   File.WriteAllBytes(file,[1,2,3]);Reject(()=>Legacy.Read(file),"Reject truncated LMF");raw[3]=1;File.WriteAllBytes(file,raw);Reject(()=>Legacy.Read(file),"Reject malformed integer");
   var lib=new Library{Profiles=[p]};string path=Path.Combine(temp,"library.json");Store.Save(lib,path);lib.Profiles[0].Name="Alterado";Store.Save(lib,path);Check(Store.Load(path+".bak").Profiles[0].Name=="Ciclo suave","Atomic backup prior state");Check(Store.Load(path).Profiles[0].Name=="Alterado","Atomic current state");
   File.WriteAllText(path,"{ invalid");try{Store.Load(path);throw new Exception("Invalid JSON accepted");}catch(System.Text.Json.JsonException){count++;}
   var bad=p.Clone();bad.Values[0][0]=101;Reject(bad.Validate,"Reject out of range");bad.Values[0][0]=-1;Reject(bad.Validate,"Reject negative value");bad.Values=[];Reject(bad.Validate,"Reject missing slots");Reject(()=>Profile.Day(start:40,end:16),"Reject reversed time");Reject(()=>Profile.Day(start:16,end:20,ramp:4),"Reject overlapping ramps");
   var day=Profile.Day(start:16,end:40,ramp:4,peaks:[50,60,70,80,90]);Check(day.Values[16][0]==0&&day.Values[20][0]==50&&day.Values[38][0]==25&&day.Values[40][0]==0,"Ramp boundaries");Check(day.At(47.5,0)==0,"Midnight continuity");
   Check(Protocol.Frame([3,8]).SequenceEqual(new byte[]{0,0x55,0xaa,3,8}.Concat(new byte[59]).Concat(new byte[]{0x0a})),"Resume frame fixture");
   Check(Protocol.Header(p,0).Length==14&&Protocol.Header(p,0)[13]==48,"Upload header");Check(Protocol.Step(p,47).SequenceEqual(new byte[]{1,2,0,8,47,23,30}.Concat(p.Values[47].Select(v=>(byte)v))),"Last step fixture");
   Check(Protocol.Clock(new DateTime(2026,10,18,18,30,45)).SequenceEqual(new byte[]{2,0,0,7,7,234,10,18,18,30,45}),"Clock fixture");
   Check(Protocol.Name(p).Length==7,"Device name length");
   var classic=Protocol.ClassicFrame(2);Check(classic.Length==65&&classic[1]==0x55&&classic[2]==0xaa&&classic[3]==2&&classic[5]==0&&classic[62]==1&&classic[63]==13&&classic[64]==10,"Classic frame fixture");
   var classicClock=Protocol.ClassicFrame(0x11,Protocol.Clock(new DateTime(2026,10,18,18,30,45))[4..]);Check(classicClock[5]==7&&classicClock[6]==7&&classicClock[7]==234&&classicClock[12]==45,"Classic clock payload");
   count+=TransferTests.Run();
   count+=WifiTests.Run();
   count+=LanguageTests.Run();
   count+=QrTests.Run();
   File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"test-results.txt"),$"{count} checks passed. {DateTime.Now:O}\nHardware was not exercised. USB transfers tested in memory; Wi-Fi command, provisioning, and ACK handling tested on local UDP sockets.\n");
  }finally {Directory.Delete(temp,true);}
 }
}
