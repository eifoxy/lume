using System.IO;
using System.Text;
using System.Text.Json;
namespace Lume;
public class Profile
{
 public string Name {get;set;} = "Meu aquário";
 public string[] Channels {get;set;} = ["Canal 1", "Canal 2", "Canal 3", "Canal 4", "Canal 5"];
 public int[][] Values {get;set;} = Enumerable.Range(0,48).Select(_=>new int[5]).ToArray();
 public Profile Clone()=>JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(this))!;
 public void Validate()
 {
  if(string.IsNullOrWhiteSpace(Name)||Name.Length>80) throw new InvalidDataException("O perfil precisa de um nome de até 80 caracteres.");
  if(Channels is null||Channels.Length!=5||Channels.Any(x=>string.IsNullOrWhiteSpace(x)||x.Length>40)) throw new InvalidDataException("Informe os nomes dos cinco canais (até 40 caracteres).");
  if(Values is null||Values.Length!=48||Values.Any(r=>r is null||r.Length!=5||r.Any(v=>v<0||v>100))) throw new InvalidDataException("A programação precisa de 48 horários, com cinco valores entre 0 e 100%.");
 }
 public double At(double slot,int ch) { slot=((slot%48)+48)%48;int i=(int)slot;return Values[i][ch]+(Values[(i+1)%48][ch]-Values[i][ch])*(slot-i); }
 public static Profile Day(string name="Ciclo suave",int start=16,int end=40,int ramp=4,int[]? peaks=null)
 {
  if(start<0||end>47||start>=end||ramp<1||ramp*2>end-start) throw new InvalidDataException("A rampa precisa caber entre o início e o fim do ciclo.");
  var p=new Profile {Name=name};peaks??=[65,65,50,0,0];
  for(int t=0;t<48;t++) for(int c=0;c<5;c++) p.Values[t][c]=(int)Math.Round(peaks[c]*Math.Clamp(Math.Min((t-start)/(double)ramp,(end-t)/(double)ramp),0,1));
  p.Validate();return p;
 }
}
public static class Legacy
{
 static byte Swap(byte b)=>(byte)((b<<4)|(b>>4));
 public static Profile Read(string path)
 {
  var b=File.ReadAllBytes(path);if(b.Length!=960) throw new InvalidDataException("Este .lmf não é do formato Pled V2.2 de 48 horários (960 bytes).");
  var p=new Profile {Name=Path.GetFileNameWithoutExtension(path)};
  for(int i=0;i<240;i++) {int v=BitConverter.ToInt32(b,i*4); if((v&~255)!=0) throw new InvalidDataException("Arquivo .lmf inválido: valor fora do formato esperado.");p.Values[i/5][i%5]=Swap((byte)v);}
  p.Validate();return p;
 }
 public static byte[] Encode(Profile p) {p.Validate();var b=new byte[960];for(int i=0;i<240;i++) BitConverter.GetBytes((int)Swap((byte)p.Values[i/5][i%5])).CopyTo(b,i*4);return b;}
 public static IEnumerable<string> ProjectPaths(string path)
 {
  var bytes=File.ReadAllBytes(path);if(bytes.Length>65536) throw new InvalidDataException("Projeto .lcm grande demais.");
  var decoded=Encoding.Latin1.GetString(bytes.Select(Swap).ToArray());
  return decoded.Split('\0',StringSplitOptions.RemoveEmptyEntries).Select(p=>Path.IsPathRooted(p)?p:Path.Combine(Path.GetDirectoryName(path)!,p)).ToArray();
 }
}
public class Library
{
 public int Version {get;set;}=1;
 public List<Profile> Profiles {get;set;}=[Profile.Day()];
 public int Selected {get;set;}
 public void Validate() {if(Version!=1||Profiles is null||Profiles.Count<1||Profiles.Count>100) throw new InvalidDataException("Biblioteca inválida ou versão não compatível.");foreach(var p in Profiles)p.Validate();Selected=Math.Clamp(Selected,0,Profiles.Count-1);}
}
public static class Store
{
 public static string Root=>Path.Combine(AppContext.BaseDirectory,"data");
 public static string FilePath=>Path.Combine(Root,"biblioteca.json");
 public static JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
 public static Library Load(string path) {if(new FileInfo(path).Length>4_000_000)throw new InvalidDataException("Biblioteca grande demais.");var l=JsonSerializer.Deserialize<Library>(File.ReadAllText(path))??throw new InvalidDataException("Biblioteca vazia.");l.Validate();return l;}
 public static void Save(Library l,string path)
 {
  l.Validate();Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";
  File.WriteAllText(temp,JsonSerializer.Serialize(l,JsonOptions),new UTF8Encoding(false));
  if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
 }
}
