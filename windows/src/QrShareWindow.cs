using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QRCoder;
namespace Lume;

public sealed class QrShareWindow:Window
{
 readonly Library source;string[] frames=[];int index;
 readonly Image image=new(){Stretch=Stretch.Uniform,Height=410};
 readonly TextBlock counter=new(){HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,12)};
 readonly CheckBox all=new(){Content=I18n.T("Todos os perfis"),Margin=new Thickness(0,10,0,8)};
 readonly CheckBox play=new(){Content=I18n.T("Alternar QR codes automaticamente"),Margin=new Thickness(0,8,0,8)};
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(2)};
 public QrShareWindow(Library library)
 {
  source=new Library{Profiles=library.Profiles.Select(p=>p.Clone()).ToList(),Selected=library.Selected};
  RenderOptions.SetBitmapScalingMode(image,BitmapScalingMode.NearestNeighbor);
  Title=I18n.T("Enviar ao celular por QR");Width=580;Height=790;MaxHeight=SystemParameters.WorkArea.Height;MinWidth=400;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(17,29,32));Foreground=Brushes.White;
  var panel=new StackPanel{Margin=new Thickness(24)};
  panel.Children.Add(new TextBlock{Text=Title,FontSize=23,FontWeight=FontWeights.SemiBold});
  panel.Children.Add(new TextBlock{Text=I18n.T("No celular, abra Perfis > Ler QR do PC. Aponte para a tela e aguarde todas as partes. Não precisa de internet nem da mesma rede."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,4)});
  panel.Children.Add(all);panel.Children.Add(new Border{Background=Brushes.White,Padding=new Thickness(8),Child=image});panel.Children.Add(counter);
  var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};var previous=new Button{Content=I18n.T("Anterior")};var next=new Button{Content=I18n.T("Próximo")};previous.Click+=(_,_)=>Show(index-1);next.Click+=(_,_)=>Show(index+1);row.Children.Add(previous);row.Children.Add(next);panel.Children.Add(row);panel.Children.Add(play);
  panel.Children.Add(new TextBlock{Text=I18n.T("A importação pede confirmação e preserva os perfis do celular. Nenhum comando é enviado à luminária. Feche esta janela ao terminar."),TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,8,0,8)});
  var close=new Button{Content=I18n.T("Concluir"),IsCancel=true};close.Click+=(_,_)=>Close();panel.Children.Add(close);Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  all.Checked+=(_,_)=>Reset();all.Unchecked+=(_,_)=>Reset();play.Checked+=(_,_)=>timer.Start();play.Unchecked+=(_,_)=>timer.Stop();timer.Tick+=(_,_)=>Show(index+1);Closed+=(_,_)=>timer.Stop();Reset();
 }
 void Reset(){timer.Stop();play.IsChecked=false;var library=all.IsChecked==true?source:new Library{Profiles=[source.Profiles[source.Selected].Clone()],Selected=0};frames=QrTransfer.Frames(library);Show(0);}
 void Show(int next){index=(next%frames.Length+frames.Length)%frames.Length;using var generator=new QRCodeGenerator();using var data=generator.CreateQrCode(frames[index],QRCodeGenerator.ECCLevel.M);using var qr=new PngByteQRCode(data);using var stream=new MemoryStream(qr.GetGraphic(6));var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();image.Source=bitmap;counter.Text=I18n.T("Parte ")+(index+1)+" / "+frames.Length;play.IsEnabled=frames.Length>1;}
}
