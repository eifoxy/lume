using System.IO;
using System.Text;
using System.Text.Json;
using QRCoder;
namespace Lume;
public static class QrTests
{
 public static int Run()
 {
  int checks=0;void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
  Check(QrTransfer.Crc(Encoding.ASCII.GetBytes("123456789"))==0xcbf43926,"Standard CRC-32 check vector");
  var p=Profile.Day("Recife 🌊 · manhã");p.Channels=["Azul","Branco","Violeta","Canal 4","Canal 5"];p.Values[0]=[0,1,50,99,100];
  var library=new Library{Profiles=[p,p.Clone()],Selected=1};library.Profiles[1].Name="Noche";var bytes=QrTransfer.Encode(library);
  Check(bytes[..9].SequenceEqual(new byte[]{76,85,77,69,1,2,0,1,0}),"QR wire header is versioned and little endian");
  var frames=QrTransfer.Frames(library,"0123456789abcdef");Check(frames.Length>=2,"Library split across multiple QR frames");
  var decoded=frames.SelectMany(frame=>{var fields=frame.Split(':');Check(fields.Length==6&&fields[0]=="LUMEQR1"&&fields[1]=="0123456789abcdef"&&int.Parse(fields[3])==frames.Length&&fields[4]==QrTransfer.Crc(bytes).ToString("x8"),"QR envelope fields");string payload=fields[5].Replace('-','+').Replace('_','/');return Convert.FromBase64String(payload.PadRight((payload.Length+3)/4*4,'='));}).ToArray();Check(decoded.SequenceEqual(bytes),"QR frames preserve all binary bytes");
  var maximum=new Library{Profiles=Enumerable.Range(0,100).Select(_=>new Profile{Name=new string('界',80),Channels=Enumerable.Repeat(new string('界',40),5).ToArray()}).ToList(),Selected=99};var maxFrames=QrTransfer.Frames(maximum,"abcdef0123456789");Check(maxFrames.Length<=384&&maxFrames.All(f=>f.Length<600),"Maximum library fits bounded QR protocol");
  var invalid=p.Clone();invalid.Values[2][1]=101;try{QrTransfer.Encode(new Library{Profiles=[invalid]});throw new Exception("Invalid profile exported");}catch(InvalidDataException){checks++;}
  var fixture=JsonSerializer.Serialize(new{library,frames});File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"qr-fixture.json"),fixture,new UTF8Encoding(false));
  using var generator=new QRCodeGenerator();using var data=generator.CreateQrCode(frames[0],QRCodeGenerator.ECCLevel.M);using var png=new PngByteQRCode(data);File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory,"qr-fixture.png"),png.GetGraphic(6));Check(png.GetGraphic(6).Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"Actual QR PNG is generated");return checks;
 }
}
