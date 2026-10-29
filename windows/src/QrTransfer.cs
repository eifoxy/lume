using System.IO;
using System.Text;
namespace Lume;

public static class QrTransfer
{
 public const int ChunkSize=360;
 public static byte[] Encode(Library library)
 {
  library.Validate();using var stream=new MemoryStream();using var writer=new BinaryWriter(stream,new UTF8Encoding(false,true),true);
  writer.Write(new byte[]{76,85,77,69,1});writer.Write((ushort)library.Profiles.Count);writer.Write((ushort)library.Selected);
  void Text(string value){var bytes=new UTF8Encoding(false,true).GetBytes(value);writer.Write((ushort)bytes.Length);writer.Write(bytes);}
  foreach(var p in library.Profiles){Text(p.Name);foreach(var channel in p.Channels)Text(channel);foreach(var row in p.Values)foreach(var value in row)writer.Write((byte)value);}
  writer.Flush();var data=stream.ToArray();if(data.Length>131072)throw new InvalidDataException(I18n.T("Biblioteca grande demais para QR."));return data;
 }
 public static uint Crc(ReadOnlySpan<byte> bytes){uint crc=0xffffffff;foreach(var b in bytes){crc^=b;for(int i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return ~crc;}
 public static string[] Frames(Library library,string? transferId=null)
 {
  var bytes=Encode(library);int count=(bytes.Length+ChunkSize-1)/ChunkSize;var id=transferId??Guid.NewGuid().ToString("N")[..16];
  if(id.Length!=16||id.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("Invalid transfer ID");
  return Enumerable.Range(0,count).Select(i=>$"LUMEQR1:{id.ToLowerInvariant()}:{i+1}:{count}:{Crc(bytes):x8}:"+Convert.ToBase64String(bytes.Skip(i*ChunkSize).Take(ChunkSize).ToArray()).TrimEnd('=').Replace('+','-').Replace('/','_')).ToArray();
 }
}
