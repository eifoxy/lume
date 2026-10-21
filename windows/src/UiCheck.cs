using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace Lume;
public partial class MainWindow
{
 public void CheckUi()
 {
  int checks=0;void Assert(bool b,string text){checks++;if(!b)throw new Exception(text);}
  library=new Library {Profiles=[Profile.Day("Ciclo do aquário",16,40,4,[85,65,45,20,0])]};RefreshProfiles();Render();
  var root=(Grid)Content;root.Background=Background;
  void Layout(int width=1460,int height=1120){root.Measure(new Size(width,height));root.Arrange(new Rect(0,0,width,height));root.UpdateLayout();Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);root.UpdateLayout();}
  void Snapshot(string name){root.UpdateLayout();Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);root.UpdateLayout();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(stream);}
  byte[] ReefFrame(string name,double time){Tank.SeekForPreview(time);root.UpdateLayout();Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);var image=new RenderTargetBitmap((int)Tank.ActualWidth,(int)Tank.ActualHeight,96,96,PixelFormats.Pbgra32);var visual=new DrawingVisual();using(var draw=visual.RenderOpen()){draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(21,33,37)),null,new Rect(0,0,Tank.ActualWidth,Tank.ActualHeight));draw.DrawRectangle(new VisualBrush(Tank){Stretch=Stretch.Fill},null,new Rect(0,0,Tank.ActualWidth,Tank.ActualHeight));}image.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var memory=new MemoryStream();encoder.Save(memory);var bytes=memory.ToArray();File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory,name),bytes);return bytes;}
  Layout();Preview.Value=18.5;UpdatePreview();Assert(PreviewTime.Text=="09:15","Clock must interpolate to minutes");Assert(Math.Abs(Tank.Levels[0]-53)<.01,"Simulation must interpolate schedule values");
  Assert(DrawMode.IsChecked!=true,"Exploration must be default, never drawing");
  var before=Legacy.Encode(P);Preview.Value=10;UpdatePreview();Assert(Legacy.Encode(P).SequenceEqual(before),"Preview must not edit schedule");
  Assert(Tank.Levels.All(v=>v==0),"Night simulation must have zero brightness");
  Assert(Tank.CoralOpening==0,"Corals must close when all channels are off");ReefFrame("recife-noite.png",3);
  Preview.Value=24;UpdatePreview();Assert(Tank.Levels.SequenceEqual(new double[]{85,65,45,20,0}),"Noon simulation must reflect all channels");
  Assert(Tank.CoralOpening>.98,"Corals must open at high light");var fishFrame=ReefFrame("recife-dia.png",3);var otherFrame=ReefFrame("recife-nado.png",7);Assert(!fishFrame.SequenceEqual(otherFrame),"Fish and water animation must change the rendered scene");Assert(Legacy.Encode(P).SequenceEqual(before),"Reef animation must never edit lighting values");
  Tank.Levels=new double[]{40,20,10,0,0};Assert(Tank.CoralOpening>0&&Tank.CoralOpening<.5,"Corals must have an intermediate opening at medium light");ReefFrame("recife-meia-luz.png",3);Tank.MotionEnabled=false;Assert(!Tank.MotionEnabled,"Reef animation can be paused independently");Tank.MotionEnabled=true;UpdatePreview();
  var svg=ReefSvg.Export();var doc=System.Xml.Linq.XDocument.Parse(svg);Assert(doc.Root?.Name.LocalName=="svg"&&doc.Descendants().Any(e=>e.Name.LocalName=="animateTransform"),"Exported SVG must contain vector animation");File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"recife-animado.svg"),svg);
  SoloMode.IsChecked=true;Draw();Assert(Chart.Children.OfType<System.Windows.Shapes.Polyline>().Count()==1,"Solo mode must show one curve");SoloMode.IsChecked=false;
  ExpandCurve(this,new RoutedEventArgs());Assert(Chart.Height==480,"Expanded curve height");ExpandCurve(this,new RoutedEventArgs());
  Snapshot("previa-curvas.png");Precision.IsExpanded=true;Layout();Workspace.ScrollToBottom();Layout();
  Assert(Table.Items.Count==48,"Table must retain 48 schedule rows");Assert(Table.ColumnHeaderHeight>=42&&Table.RowHeight>=36,"Readable table dimensions");Assert(Table.Columns[1].CellStyle!=null,"Intensity backgrounds must be styled");Table.ScrollIntoView(Table.Items[28]);Layout();Snapshot("previa-tabela.png");
  // Verify the compact viewport remains within its layout constraints.
  Workspace.ScrollToTop();Precision.IsExpanded=false;Layout(1024,768);Assert(Chart.ActualWidth>500,"Curve must remain readable on a compact screen");Snapshot("previa-compacta.png");
  var localContent=Content;var demoNames=new[]{"Diario","LEDAZUL","LedBranco","LedVioleta","Total"};
  var chooser=new UploadChooser(demoNames.Select(n=>Profile.Day(n)).ToList(),0,"Controladora Pled V2.2");
  Assert(chooser.Plan?.ClearExisting==false&&chooser.CanConfirm,"Single profile must remain the default");
  chooser.SelectAllForTest(false);Assert(!chooser.CanConfirm,"Bulk erase needs explicit acknowledgment in the dialog");
  chooser.SelectAllForTest(true);Assert(chooser.CanConfirm&&chooser.Plan?.Items.Count==5&&chooser.Plan.ClearExisting,"All-profile selection must create a full replacement plan");
  root=new Grid{Background=Background};root.Children.Add(chooser);Content=root;Layout(580,800);Snapshot("previa-envio.png");Content=localContent;
  var wifiDemo=new WifiDevice("A1B2C3D4E5F6",System.Net.IPAddress.Parse("192.168.1.42"),"Wi-Fi");using var wifiChooser=new WifiChooser(true);wifiChooser.DemoDevice(wifiDemo);
  root=new Grid{Background=Background};root.Children.Add(wifiChooser);Content=root;Layout(650,940);Snapshot("previa-wifi.png");Content=localContent;
  wifiDevice=wifiDemo;device=null;EnableUsb();Assert(SyncButton.IsEnabled&&UploadButton.IsEnabled&&!ManualButton.IsEnabled,"Wi-Fi selection routes supported actions and disables unsupported live control");wifiDevice=null;EnableUsb();Assert(!UploadButton.IsEnabled&&!SyncButton.IsEnabled,"Disconnected actions remain disabled");
  var encodedBeforeLanguages=Legacy.Encode(P);var profileNameBeforeLanguages=P.Name;
  root=(Grid)localContent;
  foreach(var locale in new[]{"en","es","pt-BR"}){I18n.Current.Set(locale,false);InitializeLanguage();Render();Layout();Assert(UploadButton.Content.ToString()!.Contains(locale=="en"?"Upload to TC420":locale=="es"?"Enviar a la TC420":"Enviar à TC420"),"Upload button language");Assert(Legacy.Encode(P).SequenceEqual(encodedBeforeLanguages)&&P.Name==profileNameBeforeLanguages,"Language switch must preserve user profiles");Snapshot("windows-"+locale+".png");}
  I18n.Current.Set("pt-BR",false);InitializeLanguage();
  saveTimer.Stop();simTimer.Stop();dirty=false;
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-test-results.txt"),$"{checks} UI checks passed. {DateTime.Now:O}\nOffscreen WPF rendering. No USB commands sent.\n");
 }
}
