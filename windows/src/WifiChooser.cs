using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Lume;

public sealed class WifiChooser : UserControl,IDisposable
{
 public WifiDevice? SelectedDevice {get;private set;}
 public bool IsConfiguring {get;private set;}
 public event EventHandler? Connected;
 readonly ObservableCollection<WifiDevice> devices=[];
 readonly ListBox list=new(){MinHeight=100,MaxHeight=150,Background=Paint("#102027"),Foreground=Paint("#DEEBE6"),BorderThickness=new Thickness(0),DisplayMemberPath="Display"};
 readonly ComboBox networks=new(){MinWidth=150};readonly TextBox ip=new(){ToolTip=I18n.T("Opcional: IP da controladora na mesma rede do PC.")};
 readonly Button search=new(){Content=I18n.T("Buscar na rede")},cancel=new(){Content=I18n.T("Parar busca"),Visibility=Visibility.Collapsed},connect=new(){Content=I18n.T("Usar esta controladora"),IsEnabled=false,Background=Paint("#77DFBF"),Foreground=Paint("#102820")};
 readonly TextBlock status=new(){Text=I18n.T("Conecte o PC \u00E0 rede do aparelho ou ao mesmo roteador e inicie a busca."),TextWrapping=TextWrapping.Wrap,FontSize=12,Foreground=Paint("#BBD2CC"),Margin=new Thickness(0,10,0,10)};
 readonly ProgressBar progress=new(){Height=4,Maximum=100,Margin=new Thickness(0,0,0,12)};
 readonly TextBox ssid=new(){MaxLength=32};readonly PasswordBox password=new(){MaxLength=32,Padding=new Thickness(9,7,9,7),Background=Paint("#142024"),Foreground=Paint("#E4ECEB"),BorderBrush=Paint("#344447")};
 readonly ComboBox security=new(){ItemsSource=new[]{"WPA2-PSK / AES","WPA-PSK / AES","WPA2-PSK / TKIP","WPA-PSK / TKIP"},SelectedIndex=0};
 readonly Button provision=new(){Content=I18n.T("Enviar rede \u00E0 controladora"),IsEnabled=false};
 readonly List<LocalRoute> routes=[];CancellationTokenSource? scan;bool disposed;
 static SolidColorBrush Paint(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
 public WifiChooser(bool demo=false)
 {
  var panel=new StackPanel{Margin=new Thickness(24)};
  panel.Children.Add(new TextBlock{Text=I18n.T("Encontre seu aqu\u00E1rio"),FontSize=23,FontWeight=FontWeights.SemiBold});
  panel.Children.Add(new TextBlock{Text=I18n.T("Wi-Fi \u00B7 controladoras Pled V2.2 \u00B7 sem cabo USB"),FontSize=12,Foreground=Paint("#A3B9B1"),Margin=new Thickness(0,7,0,15)});
  var guide=new TextBlock{Text=I18n.T("1. No Windows, conecte-se \u00E0 rede Wi-Fi criada pela controladora (por exemplo, KITELIGHT-WIFI).\n2. Volte aqui e clique em Buscar na rede.\n3. Selecione o aparelho e informe o Wi-Fi do seu roteador abaixo.\n4. Depois de enviar, reconecte o PC ao roteador e fa\u00E7a uma nova busca."),TextWrapping=TextWrapping.Wrap,LineHeight=20,FontSize=12,Margin=new Thickness(0,12,0,0)};
  panel.Children.Add(new Expander{Header=I18n.T("Primeiro acesso \u00B7 ainda n\u00E3o configurei o Wi-Fi"),IsExpanded=true,Foreground=Paint("#C9DDD5"),Content=guide,Margin=new Thickness(0,0,0,16)});
  Label(panel,I18n.T("Buscar por qual rede?"));panel.Children.Add(networks);
  var direct=new StackPanel();Label(direct,I18n.T("IP conhecido (opcional)"));direct.Children.Add(ip);panel.Children.Add(new Expander{Header=I18n.T("Busca por IP"),Foreground=Paint("#C9DDD5"),Content=direct,Margin=new Thickness(0,10,0,12)});
  var row=new WrapPanel();row.Children.Add(search);row.Children.Add(cancel);panel.Children.Add(row);panel.Children.Add(status);panel.Children.Add(progress);list.ItemsSource=devices;panel.Children.Add(list);connect.Margin=new Thickness(0,12,0,0);panel.Children.Add(connect);
  var setup=new StackPanel();Label(setup,I18n.T("Nome do Wi-Fi do roteador (SSID)"));setup.Children.Add(ssid);Label(setup,I18n.T("Senha da rede"));setup.Children.Add(password);setup.Children.Add(new TextBlock{Text=I18n.T("Compatibilidade do aparelho: nome at\u00E9 32 e senha de 8 a 32 caracteres, sem acentos. A senha n\u00E3o \u00E9 salva."),Foreground=Paint("#A3B9B1"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});Label(setup,I18n.T("Seguran\u00E7a da rede"));setup.Children.Add(security);provision.Margin=new Thickness(0,12,0,0);setup.Children.Add(provision);
  panel.Children.Add(new Expander{Header=I18n.T("Conectar o aparelho ao roteador"),IsExpanded=true,Foreground=Paint("#C9DDD5"),Content=setup,Margin=new Thickness(0,18,0,0)});
  panel.Children.Add(new TextBlock{Text=I18n.T("A busca dura at\u00E9 5 segundos. Se n\u00E3o encontrar, confirme que o PC est\u00E1 na rede do aparelho, feche o Pled e tente novamente. Redes de convidados podem isolar os dispositivos."),FontSize=11,TextWrapping=TextWrapping.Wrap,Foreground=Paint("#8CA6A0"),Margin=new Thickness(0,18,0,0)});
  Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
  if(!demo)try{routes.AddRange(LocalRoute.List());}catch(Exception){status.Text=I18n.T("N\u00E3o foi poss\u00EDvel listar as redes do Windows. Verifique a conex\u00E3o e reabra esta janela.");}
  networks.Items.Add(I18n.T("Todas as redes locais"));foreach(var route in routes)networks.Items.Add(route);networks.SelectedIndex=0;
  search.Click+=Search;cancel.Click+=(_,_)=>scan?.Cancel();list.SelectionChanged+=(_,_)=>EnableActions();
  connect.Click+=(_,_)=>{if(list.SelectedItem is WifiDevice device){SelectedDevice=device;Connected?.Invoke(this,EventArgs.Empty);}};provision.Click+=Configure;
 }
 static void Label(Panel panel,string text)=>panel.Children.Add(new TextBlock{Text=text,FontSize=12,Margin=new Thickness(0,10,0,6)});
 void EnableActions(){bool ready=scan is null&&!IsConfiguring&&list.SelectedItem is WifiDevice;connect.IsEnabled=provision.IsEnabled=ready;search.IsEnabled=!IsConfiguring&&scan is null;networks.IsEnabled=ip.IsEnabled=!IsConfiguring&&scan is null;list.IsEnabled=!IsConfiguring;}
 async void Search(object sender,RoutedEventArgs e)
 {
  if(scan is not null||IsConfiguring)return;IPAddress? target=null;
  if(ip.Text.Trim().Length>0&&(!IPAddress.TryParse(ip.Text.Trim(),out target)||target.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork)){status.Text=I18n.T("Informe um endere\u00E7o IPv4, como 192.168.1.50.");return;}
  devices.Clear();SelectedDevice=null;scan=new();cancel.Visibility=Visibility.Visible;EnableActions();progress.Value=0;
  var observer=new Progress<DiscoveryUpdate>(update=>{if(disposed)return;progress.Value=update.Percent;status.Text=update.Message;if(update.Device is WifiDevice device){var old=devices.FirstOrDefault(d=>d.Identifier==device.Identifier);if(old is not null)devices.Remove(old);devices.Add(device);}});
  try{IReadOnlyList<LocalRoute> selected=networks.SelectedItem is LocalRoute route?new[]{route}:routes;var found=await WifiDiscovery.Search(selected,target,observer,scan.Token);if(!disposed){devices.Clear();foreach(var device in found)devices.Add(device);if(devices.Count==1)list.SelectedIndex=0;}}
  catch(OperationCanceledException){if(!disposed)status.Text=I18n.T("Busca interrompida. Os aparelhos j\u00E1 encontrados continuam na lista.");}
  catch(Exception ex){if(!disposed)status.Text=ex.Message;}
  finally{scan.Dispose();scan=null;if(!disposed){cancel.Visibility=Visibility.Collapsed;EnableActions();}}
 }
 async void Configure(object sender,RoutedEventArgs e)
 {
  if(IsConfiguring||scan is not null||list.SelectedItem is not WifiDevice device)return;string network=ssid.Text;int option=security.SelectedIndex;
  byte[][] validation;
  try{validation=WifiProtocol.Provisioning(device.Identifier,network,password.Password,option is 0 or 2,option is 0 or 1);foreach(var frame in validation)System.Security.Cryptography.CryptographicOperations.ZeroMemory(frame);}
  catch(Exception ex){status.Text=ex.Message;return;}
  if(MessageBox.Show(Window.GetWindow(this),$"{I18n.T("Enviar a rede \u201C")}{network}{I18n.T("\u201D para ")}{device.Label}?\n\nO aparelho pode sair da rede atual. Depois, reconecte o PC ao roteador e busque a controladora novamente.",I18n.T("Configurar Wi-Fi do aparelho"),MessageBoxButton.OKCancel,MessageBoxImage.Question)!=MessageBoxResult.OK)return;
  IsConfiguring=true;EnableActions();status.Text=I18n.T("Enviando os dados da rede ao aparelho\u2026");progress.IsIndeterminate=true;
  try{using var session=new WifiSession(device);await session.Configure(network,password.Password,option is 0 or 2,option is 0 or 1,CancellationToken.None);status.Text=I18n.T("Aparelho confirmou os dados da rede. Reconecte o PC ao roteador, aguarde a controladora conectar e fa\u00E7a uma nova busca.");devices.Clear();SelectedDevice=null;}
  catch(Exception ex){status.Text=I18n.T("A configura\u00E7\u00E3o n\u00E3o foi confirmada. O aparelho pode j\u00E1 ter trocado de rede. Verifique as redes antes de tentar de novo. ")+ex.Message;}
  finally{password.Clear();IsConfiguring=false;progress.IsIndeterminate=false;EnableActions();}
 }
 internal void DemoDevice(WifiDevice device){devices.Add(device);list.SelectedIndex=0;status.Text=I18n.T("1 controladora encontrada \u00B7 exemplo de interface");progress.Value=100;}
 public void Dispose(){disposed=true;scan?.Cancel();password.Clear();}
}
