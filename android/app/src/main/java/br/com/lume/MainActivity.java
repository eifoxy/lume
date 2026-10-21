package br.com.lume;

import android.app.*;
import android.os.*;
import android.content.*;
import android.net.*;
import android.net.wifi.WifiManager;
import android.provider.Settings;
import android.view.*;
import android.webkit.*;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.*;

public final class MainActivity extends Activity {
 private WebView web;
 private final ExecutorService worker=Executors.newSingleThreadExecutor();
 private volatile boolean busy,cancelled,destroyed;
 private volatile DatagramSocket activeSocket;
 private final Object storageLock=new Object();
 private String pendingExport;
 private volatile String lastDiagnostic="Ainda não foi feita uma busca nesta sessão.";
 private static final String HOME="file:///android_asset/index.html";
 private AtomicFile library(){return new AtomicFile(new File(getFilesDir(),"library.json"));}
 @Override public void onCreate(Bundle state){super.onCreate(state);getWindow().setStatusBarColor(0xff101c23);getWindow().setNavigationBarColor(0xff101c23);web=new WebView(this);web.setBackgroundColor(0xff101c23);android.widget.FrameLayout rootView=new android.widget.FrameLayout(this);rootView.setBackgroundColor(0xff101c23);rootView.addView(web,new android.widget.FrameLayout.LayoutParams(-1,-1));setContentView(rootView);if(Build.VERSION.SDK_INT>=30){getWindow().setDecorFitsSystemWindows(false);rootView.setOnApplyWindowInsetsListener((v,insets)->{android.graphics.Insets bars=insets.getInsets(WindowInsets.Type.systemBars()|WindowInsets.Type.displayCutout()|WindowInsets.Type.ime());v.setPadding(bars.left,bars.top,bars.right,bars.bottom);return insets;});}WebSettings s=web.getSettings();s.setJavaScriptEnabled(true);s.setAllowFileAccess(false);s.setAllowContentAccess(false);s.setAllowFileAccessFromFileURLs(false);s.setAllowUniversalAccessFromFileURLs(false);s.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);web.addJavascriptInterface(new Bridge(),"Lume");web.setWebViewClient(new WebViewClient(){@Override public boolean shouldOverrideUrlLoading(WebView v,WebResourceRequest r){return true;}@Override public WebResourceResponse shouldInterceptRequest(WebView v,WebResourceRequest r){String u=r.getUrl().toString();if(!u.startsWith("file:///android_asset/"))return new WebResourceResponse("text/plain","UTF-8",new ByteArrayInputStream(new byte[0]));return null;}});web.loadUrl(HOME);}
 private void event(String type,Object data){if(destroyed)return;String encoded=new JSONArray().put(data).toString();final String value=encoded.substring(1,encoded.length()-1);runOnUiThread(()->{if(!destroyed)web.evaluateJavascript("window.nativeEvent("+JSONObject.quote(type)+","+value+")",null);});}
 private void progress(String text,int percent){try{event("progress",new JSONObject().put("message",text).put("percent",percent));}catch(JSONException ignored){}}
 private static String message(Throwable e){return e.getMessage()==null?"Não foi possível concluir a operação.":e.getMessage();}
 interface Job{void run()throws Exception;}
 private void job(Job action){job(action,false);}
 private synchronized void job(Job action,boolean quiet){if(busy){if(quiet)event("searching",false);else event("error","Aguarde a operação atual.");return;}busy=true;cancelled=false;if(!quiet)runOnUiThread(()->getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON));event(quiet?"searching":"busy",true);worker.execute(()->{try{action.run();}catch(Exception e){if(!quiet)event("error",message(e));}finally{DatagramSocket s=activeSocket;if(s!=null)s.close();activeSocket=null;busy=false;event(quiet?"searching":"busy",false);if(!quiet)runOnUiThread(()->getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON));}});}
 private void check()throws IOException{if(cancelled||destroyed)throw new IOException("Operação interrompida. Se estava gravando, a memória pode estar incompleta.");}
 public static JSONObject validate(JSONObject source)throws JSONException{JSONObject root=new JSONObject(source.toString());JSONArray profiles=root.getJSONArray("Profiles");if(profiles.length()<1||profiles.length()>100)throw new IllegalArgumentException("A biblioteca precisa conter de 1 a 100 perfis.");for(int p=0;p<profiles.length();p++){JSONObject profile=profiles.getJSONObject(p);String name=profile.getString("Name");if(name.trim().isEmpty()||name.length()>80)throw new IllegalArgumentException("Nome de perfil inválido.");JSONArray channels=profile.getJSONArray("Channels"),rows=profile.getJSONArray("Values");if(channels.length()!=5||rows.length()!=48)throw new IllegalArgumentException("Cada perfil precisa de 5 canais e 48 horários.");for(int c=0;c<5;c++){String channel=channels.getString(c);if(channel.trim().isEmpty()||channel.length()>40)throw new IllegalArgumentException("Nome de canal inválido.");}for(int r=0;r<48;r++){JSONArray values=rows.getJSONArray(r);if(values.length()!=5)throw new IllegalArgumentException("Horário com canais incompletos.");for(int c=0;c<5;c++){Object value=values.get(c);if(!(value instanceof Number)||((Number)value).doubleValue()!=values.getInt(c)||values.getInt(c)<0||values.getInt(c)>100)throw new IllegalArgumentException("Intensidades devem ser inteiros de 0 a 100.");}}}root.put("Version",1);root.put("Selected",Math.max(0,Math.min(profiles.length()-1,root.optInt("Selected",0))));return root;}
 private void store(String text)throws Exception{byte[] data=validate(new JSONObject(text)).toString().getBytes(StandardCharsets.UTF_8);synchronized(storageLock){AtomicFile f=library();FileOutputStream stream=null;try{stream=f.startWrite();stream.write(data);f.finishWrite(stream);}catch(Exception e){if(stream!=null)f.failWrite(stream);throw e;}}}
 private byte[] readLimited(InputStream in,int limit)throws IOException{try(InputStream s=in;ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] b=new byte[8192];int count;while((count=s.read(b))!=-1){if(out.size()+count>limit)throw new IOException("Arquivo muito grande.");out.write(b,0,count);}return out.toByteArray();}}
 private String load(){synchronized(storageLock){try{AtomicFile file=library();if(file.getBaseFile().exists()||new File(file.getBaseFile()+".bak").exists())return validate(new JSONObject(new String(readLimited(file.openRead(),2000000),StandardCharsets.UTF_8))).toString();return validate(new JSONObject(new String(readLimited(getAssets().open("library.json"),2000000),StandardCharsets.UTF_8))).toString();}catch(Exception e){return "{\"error\":"+JSONObject.quote("Biblioteca não carregada. O arquivo original foi preservado: "+message(e))+"}";}}}
 private File backup(String json)throws Exception{File directory=new File(getFilesDir(),"backups");if(!directory.exists()&&!directory.mkdirs())throw new IOException("Não foi possível criar o backup local.");File f=File.createTempFile("antes-envio-",".json",directory);try(FileOutputStream out=new FileOutputStream(f)){out.write(json.getBytes(StandardCharsets.UTF_8));out.getFD().sync();}return f;}
 private class Route implements AutoCloseable{
  final Network network;final LinkAddress address;final DatagramSocket socket;final List<InetAddress> gateways=new ArrayList<>();
  Route()throws Exception{ConnectivityManager cm=(ConnectivityManager)getSystemService(CONNECTIVITY_SERVICE);Network found=null;LinkAddress chosen=null;for(Network n:cm.getAllNetworks()){NetworkCapabilities cap=cm.getNetworkCapabilities(n);if(cap==null||!cap.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)||cap.hasTransport(NetworkCapabilities.TRANSPORT_VPN))continue;LinkProperties lp=cm.getLinkProperties(n);if(lp==null)continue;for(LinkAddress a:lp.getLinkAddresses())if(a.getAddress() instanceof Inet4Address&&a.getPrefixLength()>0){found=n;chosen=a;break;}if(found!=null)break;}if(found==null)throw new IOException("Conecte o celular ao Wi-Fi do aparelho ou da sua casa. Se a rede estiver sem internet, escolha permanecer conectado.");network=found;address=chosen;LinkProperties selectedProperties=cm.getLinkProperties(network);if(selectedProperties!=null)for(RouteInfo r:selectedProperties.getRoutes()){InetAddress gateway=r.getGateway();if(r.hasGateway()&&Discovery.host(address.getAddress(),address.getPrefixLength(),gateway)&&!gateways.contains(gateway))gateways.add(gateway);}DatagramSocket opened=new DatagramSocket(null);try{opened.setReuseAddress(false);opened.setBroadcast(true);network.bindSocket(opened);opened.bind(new InetSocketAddress(5000));opened.setSoTimeout(200);socket=opened;activeSocket=socket;}catch(Exception e){opened.close();throw new IOException("Não foi possível abrir a porta Wi-Fi 5000. Feche outros apps de controle e confira as permissões de rede local.",e);}}
  void checkRoute()throws Exception{check();ConnectivityManager cm=(ConnectivityManager)getSystemService(CONNECTIVITY_SERVICE);NetworkCapabilities caps=cm.getNetworkCapabilities(network);if(caps==null||!caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI))throw new IOException("O Wi-Fi mudou ou desconectou durante a operação.");}
  boolean local(InetAddress ip){return Protocol.sameSubnet(address.getAddress().getAddress(),ip.getAddress(),address.getPrefixLength());}
  public void close(){socket.close();if(activeSocket==socket)activeSocket=null;}
  void exchange(byte[] b,String identifier,InetAddress target,int command,int... accepted)throws Exception{checkRoute();if(!local(target)||target.isLoopbackAddress()||target.isMulticastAddress()||target.equals(address.getAddress()))throw new IOException("Selecione um dispositivo da mesma rede Wi-Fi.");PacketIO.exchange(socket,target,5000,b,command,accepted,2500,()->checkRoute(),identifier);}
  void send(byte[] p,String id,InetAddress target,int ack)throws Exception{exchange(Protocol.frame(p,id),id,target,p[0]&255,ack);}
 }
 public final class Bridge{
  @JavascriptInterface public String languagePreference(){return getSharedPreferences("connection",MODE_PRIVATE).getString("language","auto");}
  @JavascriptInterface public String deviceLanguage(){return Locale.getDefault().toLanguageTag();}
  @JavascriptInterface public String saveLanguage(String value){if(!Arrays.asList("auto","pt-BR","en","es").contains(value))return "Invalid language";return getSharedPreferences("connection",MODE_PRIVATE).edit().putString("language",value).commit()?"":"Could not save language";}
  @JavascriptInterface public String loadLibrary(){return load();}
  @JavascriptInterface public String saveLibrary(String text){try{store(text);return "";}catch(Exception e){return message(e);}}
  @JavascriptInterface public void wifi(){runOnUiThread(()->{try{startActivity(new Intent(Build.VERSION.SDK_INT>=29?Settings.Panel.ACTION_WIFI:Settings.ACTION_WIFI_SETTINGS));}catch(Exception e){startActivity(new Intent(Settings.ACTION_WIFI_SETTINGS));}});}
  @JavascriptInterface public void cancel(){cancelled=true;DatagramSocket s=activeSocket;if(s!=null)s.close();}
  @JavascriptInterface public void copyDiagnostic(String shown){final String report=shown!=null&&shown.length()<=40000?shown:lastDiagnostic;runOnUiThread(()->{android.content.ClipboardManager clipboard=(android.content.ClipboardManager)getSystemService(CLIPBOARD_SERVICE);clipboard.setPrimaryClip(ClipData.newPlainText("Lume",report));event("success","Diagnóstico copiado. Ele não contém senha nem nome da rede.");});}
  @JavascriptInterface public String preferredDevice(){return getSharedPreferences("connection",MODE_PRIVATE).getString("device","");}
  @JavascriptInterface public void rememberDevice(String id){if(id.length()<=40)getSharedPreferences("connection",MODE_PRIVATE).edit().putString("device",id).apply();}
  @JavascriptInterface public void discover(String direct){discoverNow(direct,false);}
  @JavascriptInterface public void discoverAutomatic(){discoverNow("",true);}
  private void discoverNow(String direct,boolean automatic){job(()->{
   StringBuilder report=new StringBuilder("Lume Android 1.1.0 · API "+Build.VERSION.SDK_INT+" · "+Build.MANUFACTURER+" "+Build.MODEL+"\n");
   WifiManager.MulticastLock lock=null;int received=0,rejected=0,sent=0;Map<String,JSONObject> found=new LinkedHashMap<>();
   try{
    WifiManager wm=(WifiManager)getApplicationContext().getSystemService(WIFI_SERVICE);lock=wm.createMulticastLock("lume-discovery");lock.acquire();
    try(Route route=new Route()){
     InetAddress directAddress=null;if(!direct.trim().isEmpty()){if(!direct.matches("[0-9]{1,3}(\\.[0-9]{1,3}){3}"))throw new IOException("Informe somente um endereço IPv4.");directAddress=InetAddress.getByName(direct);}
     List<InetAddress> targets=Discovery.targets(route.address.getAddress(),route.address.getPrefixLength(),route.gateways,directAddress);
     report.append("Wi-Fi IPv4: ").append(route.address).append("\nGateway(s): ").append(route.gateways).append("\nDestinos: ").append(targets).append("\nPorta de escuta: UDP 5000\n");
     long start=SystemClock.elapsedRealtime();int next=0;long duration=automatic?3000:8000;
     while(SystemClock.elapsedRealtime()-start<duration){
      route.checkRoute();long elapsed=SystemClock.elapsedRealtime()-start;
      if(next<targets.size()&&elapsed>=next*(automatic?250L:1400L)){byte[] query=Protocol.discovery();InetAddress target=targets.get(next++);route.socket.send(new DatagramPacket(query,query.length,target,5000));sent++;}
      byte[] bytes=new byte[65];DatagramPacket packet=new DatagramPacket(bytes,bytes.length);
      try{
       route.socket.receive(packet);received++;String id=Discovery.device(packet,route.address.getAddress(),route.address.getPrefixLength());
       if(id!=null){JSONObject d=new JSONObject().put("id",id).put("ip",packet.getAddress().getHostAddress()).put("protocol",Protocol.smartReply(bytes,packet.getLength())?"smart":"pled");found.put(id.toUpperCase(Locale.ROOT),d);if(received<=8)report.append("Resposta reconhecida de ").append(packet.getAddress().getHostAddress()).append(":").append(packet.getPort()).append("\n");}
       else{rejected++;if(rejected<=6){report.append("Resposta não reconhecida: ").append(packet.getLength()).append(" bytes, porta ").append(packet.getPort());if(!Discovery.host(route.address.getAddress(),route.address.getPrefixLength(),packet.getAddress()))report.append(" · origem própria/fora da rede");else report.append(" · ").append(Protocol.discoveryRejection(bytes,packet.getLength()));report.append("\n");}}
      }catch(SocketTimeoutException ignored){}
      if(!automatic)progress("Buscando no Wi-Fi · "+found.size()+" encontrada(s)",(int)Math.min(99,elapsed*100/duration));
     }
     if(!automatic){event("devices",new JSONArray(found.values()));
     event(found.isEmpty()?"error":"success",found.isEmpty()?(received==0?"Nenhuma resposta chegou ao celular. Confira a permanência no Wi-Fi sem internet, teste sem VPN/dados móveis e consulte o diagnóstico abaixo.":"Chegaram respostas, mas nenhuma foi reconhecida como controladora. Copie o diagnóstico da busca para verificarmos a compatibilidade."):"Busca concluída.");}
    }
   }catch(Exception e){report.append("Falha: ").append(message(e)).append("\n");throw e;}
   finally{if(lock!=null&&lock.isHeld())lock.release();report.append("Consultas enviadas: ").append(sent).append("\nDatagramas recebidos: ").append(received).append("\nNão reconhecidos: ").append(rejected).append("\nControladoras: ").append(found.size()).append("\nSomente descoberta; nenhuma configuração ou programação enviada.");if(automatic){event("autoDevices",new JSONArray(found.values()));}else{lastDiagnostic=report.toString();event("diagnostic",lastDiagnostic);}}
  },automatic);}
  @JavascriptInterface public void configure(String deviceJson,String ssid,String password,boolean wpa2,boolean aes){job(()->{JSONObject device=new JSONObject(deviceJson);String id=device.getString("id");byte[][] frames=Protocol.provision(id,ssid,password,wpa2,aes);try(Route route=new Route()){InetAddress ip=InetAddress.getByName(device.getString("ip"));progress("Configurando o roteador…",20);route.exchange(frames[0],id,ip,0x18,device.optString("protocol").equals("smart")?new int[]{0x70}:new int[]{0x70,0x74});progress("Enviando credenciais…",60);try{route.exchange(frames[1],id,ip,0x18,0x72);}catch(Exception e){throw new IOException("Credenciais enviadas, mas sem confirmação final. O aparelho pode já ter trocado de rede. Conecte o celular ao Wi-Fi de casa e faça uma nova busca antes de tentar configurar novamente.");}event("configured","Configuração confirmada. Agora conecte o celular à rede de casa e busque a controladora novamente.");}finally{for(byte[] b:frames)Arrays.fill(b,(byte)0);}});}
  @JavascriptInterface public void command(String deviceJson,String command){job(()->{JSONObject d=new JSONObject(deviceJson);String id=d.getString("id");Protocol.id(id);try(Route route=new Route()){InetAddress ip=InetAddress.getByName(d.getString("ip"));if(command.equals("clock")){route.send(Protocol.clock(Calendar.getInstance()),id,ip,0x85);event("success","Relógio sincronizado com a data e a hora local do celular.");}else if(command.equals("resume")){route.send(new byte[]{3,8},id,ip,0x83);event("success","Retomada do programa confirmada.");}else throw new IOException("Comando desconhecido.");}});}
  @JavascriptInterface public void upload(String deviceJson,String libraryJson,boolean replace,int selected,int slot){job(()->{
   JSONObject root=validate(new JSONObject(libraryJson)),d=new JSONObject(deviceJson);String id=d.getString("id");Protocol.id(id);boolean smart=d.optString("protocol").equals("smart");
   int firstSlot=replace?0:slot;JSONArray all=root.getJSONArray("Profiles");List<JSONObject> profiles=new ArrayList<>();
   if(replace){if(all.length()>16)throw new IOException("A controladora aceita até 16 perfis por conjunto.");for(int i=0;i<all.length();i++)profiles.add(all.getJSONObject(i));}
   else{if(selected<0||selected>=all.length()||slot<0||slot>=16)throw new IOException("Perfil ou posição inválida.");profiles.add(all.getJSONObject(selected));}
   List<String> names=new ArrayList<>();for(JSONObject p:profiles)names.add(p.getString("Name"));List<String> aliases=Protocol.aliases(names);List<Upload.Item> items=new ArrayList<>();
   for(int i=0;i<profiles.size();i++){JSONArray rows=profiles.get(i).getJSONArray("Values");int[][] values=new int[48][5];for(int r=0;r<48;r++)for(int c=0;c<5;c++)values[r][c]=rows.getJSONArray(r).getInt(c);items.add(new Upload.Item(firstSlot+i,aliases.get(i),values));}
   backup(root.toString());
   try(Route route=new Route()){InetAddress ip=InetAddress.getByName(d.getString("ip"));Upload.run(items,replace,smart,(payload,ack)->route.send(payload,id,ip,ack),()->route.checkRoute(),(percent,phase)->progress(phase,percent),()->{check();Thread.sleep(150);});}catch(Exception e){lastDiagnostic="Lume Android 1.1.0 · envio de perfis\n"+message(e);event("diagnostic",lastDiagnostic);throw e;}
   event("success",profiles.size()+" perfil(is) confirmado(s). Confira no aparelho o perfil ativo.");
  });}
  @JavascriptInterface public void exportLibrary(String json){try{String validated=validate(new JSONObject(json)).toString(2);runOnUiThread(()->{pendingExport=validated;Intent i=new Intent(Intent.ACTION_CREATE_DOCUMENT).setType("application/json").addCategory(Intent.CATEGORY_OPENABLE).putExtra(Intent.EXTRA_TITLE,"Lume-perfis.json");startActivityForResult(i,10);});}catch(Exception e){event("error",message(e));}}
  @JavascriptInterface public void importLibrary(){runOnUiThread(()->startActivityForResult(new Intent(Intent.ACTION_OPEN_DOCUMENT).setType("*/*").addCategory(Intent.CATEGORY_OPENABLE),11));}
 }
 @Override protected void onActivityResult(int request,int result,Intent data){super.onActivityResult(request,result,data);if(result!=RESULT_OK||data==null){pendingExport=null;return;}Uri uri=data.getData();if(uri==null)return;if(request==10){String content=pendingExport;pendingExport=null;if(content!=null)worker.execute(()->{try(OutputStream out=getContentResolver().openOutputStream(uri,"wt")){if(out==null)throw new IOException("Arquivo indisponível");out.write(content.getBytes(StandardCharsets.UTF_8));event("success","Biblioteca exportada.");}catch(Exception e){event("error",message(e));}});}else if(request==11)worker.execute(()->{try{byte[] b=readLimited(getContentResolver().openInputStream(uri),2000000);JSONObject imported;if(b.length==960){JSONArray rows=new JSONArray();for(int r=0;r<48;r++){JSONArray row=new JSONArray();for(int c=0;c<5;c++){int n=(r*5+c)*4;if(b[n+1]!=0||b[n+2]!=0||b[n+3]!=0)throw new IOException("Arquivo LMF inválido.");int raw=b[n]&255,value=((raw&15)<<4)|(raw>>4);if(value>100)throw new IOException("Intensidade inválida no LMF.");row.put(value);}rows.put(row);}JSONObject p=new JSONObject().put("Name","Perfil importado").put("Channels",new JSONArray(Arrays.asList("Canal 1","Canal 2","Canal 3","Canal 4","Canal 5"))).put("Values",rows);imported=new JSONObject().put("Profiles",new JSONArray().put(p)).put("Selected",0);}else imported=new JSONObject(new String(b,StandardCharsets.UTF_8));event("import",validate(imported));}catch(Exception e){event("error","Não foi possível importar. "+message(e));}});}
 @Override protected void onResume(){super.onResume();if(web!=null)event("foreground",true);}
 @Override protected void onPause(){if(web!=null)event("foreground",false);super.onPause();}
 @Override protected void onStop(){super.onStop();if(busy){cancelled=true;DatagramSocket s=activeSocket;if(s!=null)s.close();}}
 @Override protected void onDestroy(){destroyed=true;cancelled=true;if(activeSocket!=null)activeSocket.close();worker.shutdownNow();web.removeJavascriptInterface("Lume");web.destroy();super.onDestroy();}
 @Override public void onBackPressed(){web.evaluateJavascript("window.back()",null);}
}
