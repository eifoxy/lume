package br.com.lume;

import java.nio.charset.StandardCharsets;
import java.text.Normalizer;
import java.util.*;

/** Pled V2.2 Wi-Fi packets. No USB transport. */
public final class Protocol {
 /** Device identity is 12 opaque bytes in the vendor code, not necessarily ASCII hex. */
 public static byte[] identity(String id) {
  byte[] bytes;
  if(id!=null&&id.matches("raw:[a-fA-F0-9]{24}")){bytes=new byte[12];for(int i=0;i<12;i++)bytes[i]=(byte)Integer.parseInt(id.substring(4+i*2,6+i*2),16);}
  else if(id!=null&&id.matches("[a-fA-F0-9]{12}"))bytes=id.getBytes(StandardCharsets.US_ASCII);
  else throw new IllegalArgumentException("Identificação inválida da controladora.");
  boolean zero=true,ff=true,asciiZero=true,asciiF=true;for(byte value:bytes){zero&=value==0;ff&=(value&255)==255;asciiZero&=value=='0';asciiF&=value=='f'||value=='F';}
  if(zero||ff||asciiZero||asciiF)throw new IllegalArgumentException("Identificação vazia da controladora.");return bytes;
 }
 public static void id(String id) {identity(id);}
 public static String identityText(byte[] bytes){StringBuilder s=new StringBuilder("raw:");for(byte b:bytes)s.append(String.format(Locale.ROOT,"%02X",b&255));return s.toString();}
 public static byte[] discovery(){byte[] b=new byte[64]; int[] x={0x55,0xaa,0x17,0,0,3,0xdd,0xee,0xff};for(int i=0;i<x.length;i++)b[i]=(byte)x[i];b[61]=(byte)0xe3;return b;}
 public static byte[] frame(byte[] p,String identifier){id(identifier);if(p.length<1||p.length>47)throw new IllegalArgumentException("Comando inválido");byte[] b=new byte[64];b[0]=0x55;b[1]=(byte)0xaa;System.arraycopy(p,0,b,2,p.length);System.arraycopy(identity(identifier),0,b,49,12);int sum=255;for(byte v:p)sum+=v&255;b[61]=(byte)sum;return b;}
 public static byte[][] provision(String identifier,String ssid,String pass,boolean wpa2,boolean aes){id(identifier);if(!ssid.matches("[ -~]{1,32}")||ssid.trim().isEmpty())throw new IllegalArgumentException("Rede: use 1 a 32 caracteres sem acentos.");if(!pass.matches("[ -~]{8,32}"))throw new IllegalArgumentException("Senha: este aparelho aceita 8 a 32 caracteres sem acentos.");byte[][] out={new byte[64],new byte[64]};for(int i=0;i<2;i++){byte[] b=out[i];b[0]=0x55;b[1]=(byte)0xaa;b[2]=0x18;byte[] v=(i==0?ssid:pass).getBytes(StandardCharsets.US_ASCII);b[5]=(byte)v.length;b[6]=(byte)(i+1);System.arraycopy(v,0,b,7,v.length);if(i==0){b[39]=(byte)(wpa2?2:1);b[40]=(byte)(aes?1:2);}System.arraycopy(identity(identifier),0,b,49,12);int sum=0;for(int j=0;j<49;j++)sum+=b[j]&255;b[61]=(byte)sum;}return out;}
 public static boolean accepts(byte[] b,int len,int command,int expected){return len>=5&&len<=64&&(b[0]&255)==0x55&&(b[1]&255)==0xaa&&(b[2]&255)==command&&(b[4]&255)==expected;}
 public static boolean smartReply(byte[] b,int len){return b!=null&&len==64&&b.length>=len&&(b[0]&255)==0x88&&b[1]==0;}
 public static boolean accepts(byte[] b,int len,int command,int expected,String identifier){
  if(smartReply(b,len)){if(identifier==null||(b[4]&255)!=expected)return false;try{return Arrays.equals(Arrays.copyOfRange(b,49,61),identity(identifier));}catch(IllegalArgumentException e){return false;}}
  return b!=null&&len<=b.length&&accepts(b,len,command,expected);
 }
 public static String discoveryRejection(byte[] b,int len){
  if(b==null||len<16||len>64||len>b.length)return "tamanho fora do formato (16–64 bytes)";
  if(!smartReply(b,len)&&((b[0]&255)!=0x55||(b[1]&255)!=0xaa))return String.format(Locale.ROOT,"cabeçalho diferente: %02X %02X",b[0]&255,b[1]&255);
  if(b[2]!=0x17)return "comando diferente de descoberta: "+(b[2]&255);
  if(Arrays.equals(Arrays.copyOfRange(b,4,16),Arrays.copyOfRange(discovery(),4,16)))return "eco da consulta de descoberta";
  try{identity(identityText(Arrays.copyOfRange(b,4,16)));return null;}catch(IllegalArgumentException e){return "identificação vazia";}
 }
 public static String discovered(byte[] b,int len){if(discoveryRejection(b,len)!=null)return null;String text=new String(b,4,12,StandardCharsets.US_ASCII);if(text.matches("[a-fA-F0-9]{12}"))return text;return identityText(Arrays.copyOfRange(b,4,16));}
 public static byte[] header(int slot,String alias){if(slot<0||slot>15||!alias.matches("[A-Za-z0-9]{1,7}"))throw new IllegalArgumentException("Perfil inválido");byte[] p={1,1,0,10,(byte)slot,0,0,0,0,0,0,0,0,48};Arrays.fill(p,5,12,(byte)32);System.arraycopy(alias.getBytes(StandardCharsets.US_ASCII),0,p,5,alias.length());return p;}
 public static byte[] smartHeader(int slot,String alias){if(slot<0||slot>15||!alias.matches("[A-Za-z0-9]{1,7}"))throw new IllegalArgumentException("Perfil inválido");byte[] p=new byte[5+alias.length()];p[0]=1;p[1]=1;p[3]=(byte)alias.length();p[4]=(byte)slot;System.arraycopy(alias.getBytes(StandardCharsets.US_ASCII),0,p,5,alias.length());return p;}
 public static byte[] step(int row,int[] values){if(row<0||row>=48||values.length!=5)throw new IllegalArgumentException("Horário inválido");byte[] p={1,2,0,8,(byte)row,(byte)(row/2),(byte)(row%2*30),0,0,0,0,0};for(int c=0;c<5;c++){if(values[c]<0||values[c]>100)throw new IllegalArgumentException("Luz fora de 0–100%");p[7+c]=(byte)values[c];}return p;}
 public static byte[] clock(Calendar c){int y=c.get(Calendar.YEAR);return new byte[]{2,0,0,7,(byte)(y>>8),(byte)y,(byte)(c.get(Calendar.MONTH)+1),(byte)c.get(Calendar.DAY_OF_MONTH),(byte)c.get(Calendar.HOUR_OF_DAY),(byte)c.get(Calendar.MINUTE),(byte)c.get(Calendar.SECOND)};}
 public static List<String> aliases(List<String> names){Set<String> used=new HashSet<>();List<String> out=new ArrayList<>();for(String n:names){String base=Normalizer.normalize(n,Normalizer.Form.NFD).replaceAll("[^a-zA-Z0-9]","");if(base.isEmpty())base="PERFIL";base=base.substring(0,Math.min(7,base.length()));String a=base;int k=2;while(!used.add(a.toUpperCase(Locale.ROOT))){String tail=""+k++;a=base.substring(0,Math.min(base.length(),7-tail.length()))+tail;}out.add(a);}return out;}
 public static boolean sameSubnet(byte[] a,byte[] b,int prefix){if(a.length!=4||b.length!=4||prefix<1||prefix>32)return false;for(int i=0;i<4;i++){int bits=Math.max(0,Math.min(8,prefix-i*8)),mask=(255<<(8-bits))&255;if((a[i]&mask)!=(b[i]&mask))return false;}return true;}
 public static byte[] broadcast(byte[] a,int prefix){byte[] b=a.clone();for(int i=0;i<4;i++){int bits=Math.max(0,Math.min(8,prefix-i*8)),mask=(255<<(8-bits))&255;b[i]=(byte)((a[i]&255)|(~mask&255));}return b;}
}
