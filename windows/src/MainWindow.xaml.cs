using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Data;
using System.Globalization;
using Microsoft.Win32;
namespace Lume;
public partial class MainWindow : Window
{
 Library library=new(); Profile P=>library.Profiles[library.Selected];
 readonly Stack<Library> history=new(); readonly DispatcherTimer saveTimer=new(){Interval=TimeSpan.FromMilliseconds(650)},simTimer=new(){Interval=TimeSpan.FromMilliseconds(40)};
 readonly string[] colors=["#66B8FF","#E0E9DB","#B29AFF","#E8BF73","#74D7B7"];
 readonly TextBlock[] percents=new TextBlock[5];readonly Slider[] sliders=new Slider[5];
 readonly TextBlock[] ledLabels=new TextBlock[5];
 double? hoverSlot;double daySeconds=36; bool tableRebuilding; int lastTableSlot=-1;
 Device? device;WifiDevice? wifiDevice;bool refreshing,drawing,busy,dirty;int channel,lastSlot=-1; CancellationTokenSource? operation;
 public MainWindow()
 {
  InitializeComponent();
  Width=Math.Min(Width,SystemParameters.WorkArea.Width);Height=Math.Min(Height,SystemParameters.WorkArea.Height);
  try {if(File.Exists(Store.FilePath)) library=Store.Load(Store.FilePath);}catch(Exception ex){App.Log(ex);var backup=Store.FilePath+".bak";try {File.Copy(Store.FilePath,Store.FilePath+".corrompido-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"),false);}catch{}try {library=Store.Load(backup);Status.Text=I18n.T("Biblioteca recuperada do backup anterior.");}catch {Status.Text=I18n.T("N\u00E3o foi poss\u00EDvel ler a biblioteca. Importe um backup para recuperar seus perfis.");dirty=false;} }
  saveTimer.Tick+=(_,_)=>{saveTimer.Stop();Save();};simTimer.Tick+=(_,_)=>Preview.Value=(Preview.Value+48*simTimer.Interval.TotalSeconds/daySeconds)%48;
  Closing+=(_,e)=>{if(busy){MessageBox.Show(this,I18n.T("Aguarde a opera\u00E7\u00E3o USB terminar antes de fechar."),"Lume");e.Cancel=true;return;}Table.CommitEdit(DataGridEditingUnit.Cell,true);saveTimer.Stop();if(dirty&&!Save()){if(MessageBox.Show(this,I18n.T("N\u00E3o foi poss\u00EDvel salvar. Fechar mesmo assim?"),"Lume",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)e.Cancel=true;}};
  RefreshProfiles();Render();InitializeLanguage();
 }
 static Brush Brush(string hex)=>(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
 void Note(string text)=>Status.Text=text;
 void ShareQr(object sender,RoutedEventArgs e){if(busy){Note(I18n.T("Aguarde a operação atual."));return;}if(!Table.CommitEdit(DataGridEditingUnit.Cell,true)||!Table.CommitEdit(DataGridEditingUnit.Row,true))return;try{new QrShareWindow(library){Owner=this}.ShowDialog();}catch(Exception ex){Note(I18n.T("Falha ao gerar QR: ")+ex.Message);}}
 bool Save(){try {Store.Save(library,Store.FilePath);dirty=false;Note(I18n.T("Salvo neste computador \u00B7 ")+DateTime.Now.ToString("HH:mm:ss"));return true;}catch(Exception ex){App.Log(ex);Note(I18n.T("Falha ao salvar: ")+ex.Message);return false;}}
 void Changed(){dirty=true;saveTimer.Stop();saveTimer.Start();UpdatePreview();}
 void Remember(){history.Push(new Library{Selected=library.Selected,Profiles=library.Profiles.Select(p=>p.Clone()).ToList()});if(history.Count>40){var all=history.Take(40).Reverse().ToArray();history.Clear();foreach(var p in all)history.Push(p);}}
 void RefreshProfiles(){refreshing=true;Profiles.ItemsSource=null;Profiles.ItemsSource=library.Profiles.Select(x=>x.Name).ToList();Profiles.SelectedIndex=library.Selected;refreshing=false;}
 void SelectProfile(object sender,SelectionChangedEventArgs e){if(refreshing||Profiles.SelectedIndex<0)return;Table.CommitEdit();library.Selected=Profiles.SelectedIndex;history.Clear();Render();Changed();}
 void Render()
 {
  refreshing=true;ProfileTitle.Text=P.Name;ChannelCards.Children.Clear();Legend.Children.Clear();LedReadings.Children.Clear();
  for(int c=0;c<5;c++){
   int index=c;var stack=new StackPanel();var top=new DockPanel();var tag=new TextBlock{Text=$"CH{c+1}",FontSize=10,Foreground=Brush(colors[c])};top.Children.Add(tag);stack.Children.Add(top);
   var name=new TextBox{Text=P.Channels[c],FontSize=12,Margin=new Thickness(0,8,0,0),Padding=new Thickness(5,5,5,5),MaxLength=40,ToolTip=I18n.T("Nome do canal f\u00EDsico ")+(c+1)};
   name.LostKeyboardFocus+=(_,_)=>{if(name.Text.Trim().Length==0){name.Text=P.Channels[index];return;}if(name.Text.Trim()!=P.Channels[index]){Remember();P.Channels[index]=name.Text.Trim();Changed();}};stack.Children.Add(name);
   var percent=new TextBlock{FontSize=28,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,8,0,0)};percents[c]=percent;stack.Children.Add(percent);
   var slider=new Slider{Minimum=0,Maximum=100,TickFrequency=1,IsSnapToTickEnabled=true,ToolTip=I18n.T("Editar este canal no hor\u00E1rio da pr\u00E9via")};sliders[c]=slider;
   slider.PreviewMouseLeftButtonDown+=(_,_)=>{PauseSimulation();Remember();};slider.PreviewKeyDown+=(_,_)=>{PauseSimulation();Remember();};slider.ValueChanged+=(_,_)=>{if(refreshing)return;int slot=EditSlot;P.Values[slot][index]=(int)slider.Value;Preview.Value=slot;Changed();RefreshTable();};stack.Children.Add(slider);
   var select=new Button {Content=c==channel?I18n.T("\u25CF  Editando curva"):I18n.T("Editar curva"),Margin=new Thickness(0),Padding=new Thickness(4,7,4,7),FontSize=11,Background=Brush(c==channel?"#29483F":"#223035")};
   select.Click+=(_,_)=>{channel=index;Render();};stack.Children.Add(select);
   ChannelCards.Children.Add(new Border{Child=stack,Padding=new Thickness(13),CornerRadius=new CornerRadius(10),Background=Brush("#172429"),Margin=new Thickness(0,0,c==4?0:8,0),BorderThickness=new Thickness(1),BorderBrush=Brush(c==channel?colors[c]:"#2A383C")});
   var legend=new Button{Content=$"● CH{c+1}",Foreground=Brush(colors[c]),FontSize=12,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,0,8,0),Background=Brush(c==channel?"#2A4148":"#1B2C32"),ToolTip=P.Channels[c]};legend.Click+=(_,_)=>{channel=index;Render();};Legend.Children.Add(legend);
   var ledLabel=new TextBlock{TextAlignment=TextAlignment.Center,Foreground=Brush(colors[c]),FontSize=11,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=P.Channels[c]};ledLabels[c]=ledLabel;LedReadings.Children.Add(ledLabel);
   var col=Table.Columns[c+1];col.Header=new TextBlock{Text=$"CH{c+1} · {ShortName(P.Channels[c])}{I18n.T("\nIntensidade (%)")}",Foreground=Brush(colors[c]),FontSize=11,LineHeight=15,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=P.Channels[c]};
   ((DataGridTextColumn)col).Binding=new Binding($"C{c+1}");
   var style=new Style(typeof(DataGridCell),(Style)Table.FindResource(typeof(DataGridCell)));style.Setters.Add(new Setter(BackgroundProperty,new Binding($"C{c+1}"){Converter=new IntensityBrushConverter(),ConverterParameter=colors[c]}));col.CellStyle=style;
  }
  refreshing=false;RefreshTable();Draw();UpdatePreview();
 }
 static string ShortName(string name)=>name.Length>14?name[..13]+"…":name;
 int EditSlot=>Math.Clamp((int)Math.Round(Preview.Value),0,47);
 static string TimeAt(double slot){int minutes=Math.Clamp((int)(slot*30),0,1439);return $"{minutes/60:00}:{minutes%60:00}";}
 void RefreshTable(){tableRebuilding=true;Table.ItemsSource=Enumerable.Range(0,48).Select(i=>new ScheduleRow(i,P.Values[i])).ToList();tableRebuilding=false;}
 void UpdatePreview(){if(percents[0] is null)return;refreshing=true;PreviewTime.Text=TimeAt(Preview.Value);EditingTime.Text=I18n.T("Sliders editam o ponto de ")+TimeAt(EditSlot);double[] values=Enumerable.Range(0,5).Select(c=>P.At(Preview.Value,c)).ToArray();for(int c=0;c<5;c++){percents[c].Text=P.Values[EditSlot][c]+"%";sliders[c].Value=P.Values[EditSlot][c];ledLabels[c].Text=$"CH{c+1} · {Math.Round(values[c])}%";}Tank.Levels=values;refreshing=false;Draw();}
 void PreviewChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(IsLoaded)UpdatePreview();}
 void Simulate(object sender,RoutedEventArgs e){if(simTimer.IsEnabled){simTimer.Stop();SimButton.Content="▶";}else{simTimer.Start();SimButton.Content="Ⅱ";}}
 void PauseSimulation(){simTimer.Stop();SimButton.Content="▶";}
 void ReefMotionChanged(object sender,RoutedEventArgs e){if(Tank is not null)Tank.MotionEnabled=AnimateReef.IsChecked==true;}
 void JumpTime(object sender,RoutedEventArgs e){PauseSimulation();Preview.Value=double.Parse((string)((Button)sender).Tag,CultureInfo.InvariantCulture);}
 void SpeedChanged(object sender,SelectionChangedEventArgs e){daySeconds=SimSpeed?.SelectedIndex switch{0=>12,2=>72,_=>36};}
 void TableSelected(object sender,SelectedCellsChangedEventArgs e){if(tableRebuilding||Table.SelectedCells.Count==0||Table.SelectedCells[0].Item is not ScheduleRow r||r.Index==lastTableSlot)return;lastTableSlot=r.Index;PauseSimulation();Preview.Value=r.Index;}
 void ChartOptions(object sender,RoutedEventArgs e){if(Chart is null)return;Chart.Cursor=DrawMode.IsChecked==true?Cursors.Pen:Cursors.Cross;Draw();}
 void ExpandCurve(object sender,RoutedEventArgs e){bool expand=Chart.Height<400;Chart.Height=expand?480:300;ExpandChart.Content=expand?I18n.T("\u26F6  Compactar curva"):I18n.T("\u26F6  Ampliar curva");}
 void Draw()
 {
  if(Chart is null||Chart.ActualWidth<100||Legend is null)return;Chart.Children.Clear();double w=Chart.ActualWidth-70,h=Chart.ActualHeight-54;const double left=50,top=18;
  // Clock bands give spatial landmarks; they do not alter the schedule.
  foreach(int start in new[]{0,36}){var night=new Rectangle{Width=w/4,Height=h,Fill=Brush("#0F191E"),IsHitTestVisible=false};Canvas.SetLeft(night,left+w*start/48);Canvas.SetTop(night,top);Chart.Children.Add(night);}
  for(int v=0;v<=100;v+=25){double y=top+h*(1-v/100d);Chart.Children.Add(new Line{X1=left,X2=left+w,Y1=y,Y2=y,Stroke=Brush(v==0?"#52656A":"#31464C"),StrokeThickness=1,IsHitTestVisible=false});Label(v+"%",3,y-8,"#ACBFBE",11);}
  int tickStep=w>850?4:8;
  for(int t=0;t<=48;t+=tickStep){double x=left+w*t/48;Chart.Children.Add(new Line{X1=x,X2=x,Y1=top,Y2=top+h,Stroke=Brush("#26393F"),StrokeThickness=1,StrokeDashArray=new DoubleCollection{2,5},IsHitTestVisible=false});Label($"{t/2:00}:00",x-15,top+h+12,"#ACBFBE",11);}
  var area=new Polygon{Fill=new LinearGradientBrush(((SolidColorBrush)Brush(colors[channel])).Color,Colors.Transparent,90),Opacity=.13,IsHitTestVisible=false};area.Points.Add(new Point(left,top+h));for(int t=0;t<=48;t++)area.Points.Add(new Point(left+w*t/48,top+h*(1-P.Values[t%48][channel]/100d)));area.Points.Add(new Point(left+w,top+h));Chart.Children.Add(area);
  bool solo=SoloMode.IsChecked==true;
  foreach(int c in Enumerable.Range(0,5).OrderBy(c=>c==channel?1:0)){
   if(solo&&c!=channel)continue;
   var line=new Polyline{Stroke=Brush(colors[c]),StrokeThickness=c==channel?3.4:2.2,Opacity=c==channel?1:.8,StrokeLineJoin=PenLineJoin.Round,IsHitTestVisible=false};for(int t=0;t<=48;t++)line.Points.Add(new Point(left+w*t/48,top+h*(1-P.Values[t%48][c]/100d)));Chart.Children.Add(line);
   if(c==channel)for(int t=0;t<48;t++)Dot(left+w*t/48,top+h*(1-P.Values[t][c]/100d),2.4,colors[c],.8);
  }
  double slot=hoverSlot??Preview.Value,px=left+w*slot/48;Chart.Children.Add(new Line{X1=px,X2=px,Y1=top,Y2=top+h,Stroke=Brush("#D3EAE2"),StrokeThickness=1,StrokeDashArray=new DoubleCollection{4,4},IsHitTestVisible=false});
  for(int c=0;c<5;c++)if(!solo||c==channel)Dot(px,top+h*(1-P.At(slot,c)/100),c==channel?5:3.5,colors[c],1);
  ChartReading.Text=TimeAt(slot)+"   ·   "+string.Join("     ",Enumerable.Range(0,5).Select(c=>$"CH{c+1} {Math.Round(P.At(slot,c))}%"))+(DrawMode.IsChecked==true?I18n.T("   |   DESENHO: ")+P.Channels[channel]:"");
 }
 void Dot(double x,double y,double radius,string color,double opacity){var dot=new Ellipse{Width=radius*2,Height=radius*2,Fill=Brush(color),Stroke=Brush("#152125"),StrokeThickness=1,Opacity=opacity,IsHitTestVisible=false};Canvas.SetLeft(dot,x-radius);Canvas.SetTop(dot,y-radius);Chart.Children.Add(dot);}
 void Label(string text,double x,double y,string color,int size){var t=new TextBlock{Text=text,Foreground=Brush(color),FontSize=size,IsHitTestVisible=false};Canvas.SetLeft(t,x);Canvas.SetTop(t,y);Chart.Children.Add(t);}
 void ChartSized(object sender,SizeChangedEventArgs e)=>Draw();
 double SlotAt(Point p)=>Math.Clamp((p.X-50)/(Chart.ActualWidth-70)*48,0,47.999);
 void ChartDown(object sender,MouseButtonEventArgs e){PauseSimulation();hoverSlot=null;if(!CommitTable())return;if(DrawMode.IsChecked!=true){Preview.Value=SlotAt(e.GetPosition(Chart));Draw();return;}Remember();drawing=true;lastSlot=-1;Chart.CaptureMouse();Paint(e.GetPosition(Chart));}
 void ChartMove(object sender,MouseEventArgs e){if(drawing&&e.LeftButton==MouseButtonState.Pressed)Paint(e.GetPosition(Chart));else{hoverSlot=SlotAt(e.GetPosition(Chart));Draw();}}
 void ChartLeave(object sender,MouseEventArgs e){if(drawing)return;hoverSlot=null;Draw();}
 void ChartUp(object sender,MouseButtonEventArgs e){drawing=false;Chart.ReleaseMouseCapture();RefreshTable();}
 void ChartLost(object sender,MouseEventArgs e){drawing=false;RefreshTable();}
 void Paint(Point pt){double h=Chart.ActualHeight-54;if(Chart.ActualWidth<=70||h<=0)return;int t=Math.Clamp((int)Math.Round(SlotAt(pt)),0,47),v=Math.Clamp((int)Math.Round((1-(pt.Y-18)/h)*100),0,100);if(lastSlot>=0&&t!=lastSlot){int prev=P.Values[lastSlot][channel];for(int i=Math.Min(t,lastSlot);i<=Math.Max(t,lastSlot);i++)P.Values[i][channel]=(int)Math.Round(prev+(v-prev)*(i-lastSlot)/(double)(t-lastSlot));}P.Values[t][channel]=v;lastSlot=t;Changed();}
 void Undo(object sender,RoutedEventArgs e){if(busy)return;if(history.Count==0){Note(I18n.T("Nenhuma altera\u00E7\u00E3o para desfazer neste perfil."));return;}library=history.Pop();RefreshProfiles();Render();Changed();}
 void CellEdited(object sender,DataGridCellEditEndingEventArgs e)
 {
  if(e.EditAction!=DataGridEditAction.Commit||e.EditingElement is not TextBox text)return;int c=e.Column.DisplayIndex-1;
  if(!int.TryParse(text.Text,out int v)||v<0||v>100){e.Cancel=true;Note(I18n.T("Use um n\u00FAmero inteiro entre 0 e 100%."));return;}Remember();P.Values[((ScheduleRow)e.Row.Item).Index][c]=v;Changed();
 }
 string? Ask(string title,string label,string initial)
 {
  var d=Dialog(title);var stack=new StackPanel{Margin=new Thickness(24)};stack.Children.Add(new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)});var box=new TextBox{Text=initial,MaxLength=80};stack.Children.Add(box);var ok=new Button{Content=I18n.T("Confirmar"),IsDefault=true,Margin=new Thickness(0,18,0,0)};ok.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(box.Text))d.DialogResult=true;};stack.Children.Add(ok);d.Content=stack;d.Loaded+=(_,_)=>{box.Focus();box.SelectAll();};return d.ShowDialog()==true?box.Text.Trim():null;
 }
 Window Dialog(string title)=>new(){Title=title,Owner=this,Width=460,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,Background=Brush("#152125"),Foreground=Brush("#E4ECEB"),WindowStartupLocation=WindowStartupLocation.CenterOwner};
 void Add(Profile p){if(library.Profiles.Count>=100)throw new InvalidDataException(I18n.T("Limite de 100 perfis atingido."));library.Profiles.Add(p);library.Selected=library.Profiles.Count-1;history.Clear();RefreshProfiles();Render();Changed();}
 void NewProfile(object sender,RoutedEventArgs e){var name=Ask(I18n.T("Novo perfil"),I18n.T("Dê um nome ao perfil. Ele começa com todos os canais apagados."),I18n.T("Novo aqu\u00E1rio"));if(name!=null)Add(new Profile{Name=name});}
 void Rename(object sender,RoutedEventArgs e){var name=Ask(I18n.T("Renomear perfil"),I18n.T("Nome do perfil"),P.Name);if(name!=null){Remember();P.Name=name;RefreshProfiles();Render();Changed();}}
 void Duplicate(object sender,RoutedEventArgs e){var copy=P.Clone();copy.Name=(copy.Name.Length>70?copy.Name[..70]:copy.Name)+I18n.T(" \u00B7 c\u00F3pia");Add(copy);}
 void Import(object sender,RoutedEventArgs e)
 {
  var open=new OpenFileDialog{Title=I18n.T("Importar perfis"),Filter=I18n.T("Perfis e projetos|*.lmf;*.lcm;*.json|Perfil Pled|*.lmf|Projeto Pled|*.lcm|Backup Lume|*.json"),Multiselect=true};if(open.ShowDialog(this)!=true)return;
  try{
   var incoming=new List<Profile>();var missing=new List<string>();
   foreach(var path in open.FileNames){switch(System.IO.Path.GetExtension(path).ToLowerInvariant()){
    case ".lmf":incoming.Add(Legacy.Read(path));break;
    case ".lcm":foreach(var p in Legacy.ProjectPaths(path)){var resolved=File.Exists(p)?p:System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,System.IO.Path.GetFileName(p));if(File.Exists(resolved))incoming.Add(Legacy.Read(resolved));else missing.Add(System.IO.Path.GetFileName(p));}break;
    case ".json":incoming.AddRange(Store.Load(path).Profiles);break;
   }}
   if(incoming.Count+library.Profiles.Count>100)throw new InvalidDataException(I18n.T("A importa\u00E7\u00E3o excederia o limite de 100 perfis."));
   foreach(var p in incoming)p.Validate();foreach(var p in incoming)library.Profiles.Add(p);
   if(incoming.Count>0){library.Selected=library.Profiles.Count-incoming.Count;history.Clear();RefreshProfiles();Render();Changed();}
   if(missing.Count>0)MessageBox.Show(this,I18n.T("Alguns arquivos do projeto n\u00E3o foram encontrados:\n")+string.Join("\n",missing)+I18n.T("\n\nColoque os .lmf junto do .lcm e importe novamente."),I18n.T("Importa\u00E7\u00E3o parcial"));
   Note($"{incoming.Count}{I18n.T(" perfil(is) importado(s). ")}"+(missing.Count>0?$"{missing.Count}{I18n.T(" arquivo(s) ausente(s).")}":I18n.T("Arquivos originais preservados.")));
  }catch(Exception ex){App.Log(ex);MessageBox.Show(this,ex.Message,I18n.T("N\u00E3o foi poss\u00EDvel importar"));}
 }
 void Export(object sender,RoutedEventArgs e){var save=new SaveFileDialog{Filter=I18n.T("Perfil Pled V2.2|*.lmf"),FileName=SafeName(P.Name)+".lmf"};if(save.ShowDialog(this)==true)try {File.WriteAllBytes(save.FileName,Legacy.Encode(P));Note(I18n.T("Perfil exportado para o Pled."));}catch(Exception ex){MessageBox.Show(this,ex.Message,I18n.T("Falha ao exportar"));}}
 bool CommitTable(){if(!Table.CommitEdit(DataGridEditingUnit.Cell,true)||!Table.CommitEdit(DataGridEditingUnit.Row,true)){Note(I18n.T("Corrija o valor da c\u00E9lula antes de continuar (0 a 100%)."));return false;}return true;}
 void Backup(object sender,RoutedEventArgs e){if(!CommitTable())return;var save=new SaveFileDialog{Filter="Backup Lume|*.json",FileName="Lume-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+".json"};if(save.ShowDialog(this)==true)try{Store.Save(library,save.FileName);Note(I18n.T("Backup de todos os perfis salvo."));}catch(Exception ex){MessageBox.Show(this,ex.Message,I18n.T("Falha no backup"));}}
 static string SafeName(string n)=>string.Concat(n.Select(c=>System.IO.Path.GetInvalidFileNameChars().Contains(c)?'_':c));
 void ReorderChannels(object sender,RoutedEventArgs e)
 {
  if(busy||!CommitTable())return;
  var d=Dialog(I18n.T("Reorganizar canais"));d.Width=510;d.MaxHeight=SystemParameters.WorkArea.Height;
  var s=new StackPanel{Margin=new Thickness(24)};
  void Text(string value){s.Children.Add(new TextBlock{Text=I18n.T(value),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,8)});}
  Text("Troque as curvas entre canais sem perder nenhuma programação. Os nomes acompanham os 48 horários.");
  ComboBox Choice(string label,int selected){Text(label);var c=new ComboBox{ItemsSource=P.Channels.Select((n,i)=>$"CH{i+1} · {n}").ToArray(),SelectedIndex=selected};s.Children.Add(c);return c;}
  var from=Choice("Canal de origem",channel);var to=Choice("Canal de destino",(channel+1)%5);
  Text("O que deseja fazer?");var mode=new ComboBox{ItemsSource=new[]{I18n.T("Trocar canais (preserva as duas curvas)"),I18n.T("Copiar canal (substitui o destino)")},SelectedIndex=0};s.Children.Add(mode);
  var all=new CheckBox{Content=I18n.T("Aplicar a todos os perfis da biblioteca"),Margin=new Thickness(0,16,0,8)};s.Children.Add(all);
  var review=new TextBlock{Margin=new Thickness(0,8,0,8)};s.Children.Add(review);
  var ack=new CheckBox{Content=new TextBlock{Text=I18n.T("Confirmo substituir o nome e a curva do canal de destino."),TextWrapping=TextWrapping.Wrap},Visibility=Visibility.Collapsed};s.Children.Add(ack);
  Text("A alteração é local e pode ser desfeita. Envie a programação para atualizar a controladora.");
  var ok=new Button{Content=I18n.T("Confirmar"),Margin=new Thickness(0,12,0,0)};s.Children.Add(ok);
  void Review(){bool copy=mode.SelectedIndex==1;ack.Visibility=copy?Visibility.Visible:Visibility.Collapsed;review.Text=$"CH{from.SelectedIndex+1} {(copy?"→":"↔")} CH{to.SelectedIndex+1} · {(all.IsChecked==true?library.Profiles.Count:1)} {I18n.T("perfil(is)")}";ok.IsEnabled=from.SelectedIndex!=to.SelectedIndex&&(!copy||ack.IsChecked==true);}
  void SelectionChanged(){ack.IsChecked=false;Review();}
  from.SelectionChanged+=(_,_)=>SelectionChanged();to.SelectionChanged+=(_,_)=>SelectionChanged();mode.SelectionChanged+=(_,_)=>SelectionChanged();all.Checked+=(_,_)=>SelectionChanged();all.Unchecked+=(_,_)=>SelectionChanged();ack.Checked+=(_,_)=>Review();ack.Unchecked+=(_,_)=>Review();
  ok.Click+=(_,_)=>{if(busy)return;library.Validate();Remember();foreach(var p in all.IsChecked==true?library.Profiles:new List<Profile>{P})p.TransferChannel(from.SelectedIndex,to.SelectedIndex,mode.SelectedIndex==1);channel=to.SelectedIndex;Render();Changed();d.DialogResult=true;};
  Review();d.Content=new ScrollViewer{Content=s,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};d.ShowDialog();
 }
 void Wizard(object sender,RoutedEventArgs e)
 {
  var d=Dialog(I18n.T("Criar ciclo suave"));d.Width=500;var s=new StackPanel{Margin=new Thickness(24)};
  s.Children.Add(new TextBlock{Text=I18n.T("Um dia de luz, sem planilhas."),FontSize=20,FontWeight=FontWeights.SemiBold});s.Children.Add(new TextBlock{Text=I18n.T("Escolha os hor\u00E1rios e a intensidade m\u00E1xima de cada canal.\nA rampa \u00E9 calculada em passos de 30 minutos."),Foreground=Brush("#A4B7B0"),Margin=new Thickness(0,10,0,18)});
  ComboBox Choice(string label,int value){s.Children.Add(new TextBlock{Text=label,Margin=new Thickness(0,8,0,5)});var c=new ComboBox{ItemsSource=Enumerable.Range(0,48).Select(t=>$"{t/2:00}:{t%2*30:00}").ToList(),SelectedIndex=value};s.Children.Add(c);return c;}
  var start=Choice(I18n.T("In\u00EDcio da luz"),16);var end=Choice(I18n.T("Fim da luz"),40);s.Children.Add(new TextBlock{Text=I18n.T("Dura\u00E7\u00E3o de cada rampa"),Margin=new Thickness(0,12,0,5)});var ramp=new ComboBox{ItemsSource=new[]{I18n.T("30 minutos"),I18n.T("1 hora"),I18n.T("1 hora e 30 min"),I18n.T("2 horas"),I18n.T("2 horas e 30 min"),I18n.T("3 horas")},SelectedIndex=3};s.Children.Add(ramp);
  var peaks=new TextBox[5];for(int c=0;c<5;c++){var row=new DockPanel{Margin=new Thickness(0,9,0,0)};var label=new TextBlock{Text=$"CH{c+1} · {P.Channels[c]}",VerticalAlignment=VerticalAlignment.Center,Width=270};row.Children.Add(label);peaks[c]=new TextBox{Text=(c<3?65:0).ToString(),Width=70,MaxLength=3};row.Children.Add(peaks[c]);s.Children.Add(row);}
  var error=new TextBlock{Foreground=Brush("#EFBD90"),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};s.Children.Add(error);var ok=new Button{Content=I18n.T("Aplicar ciclo a este perfil"),Background=Brush("#77DFBF"),Foreground=Brush("#102820"),Margin=new Thickness(0,16,0,0)};
  ok.Click+=(_,_)=>{try{var v=peaks.Select(x=>int.TryParse(x.Text,out int n)&&n>=0&&n<=100?n:throw new InvalidDataException(I18n.T("As intensidades precisam estar entre 0 e 100%."))).ToArray();var p=Profile.Day(P.Name,start.SelectedIndex,end.SelectedIndex,ramp.SelectedIndex+1,v);Remember();P.Values=p.Values;Render();Changed();d.DialogResult=true;}catch(Exception ex){error.Text=ex.Message;}};s.Children.Add(ok);d.Content=s;d.ShowDialog();
 }
 async void Detect(object sender,RoutedEventArgs e)
 {
  if(busy)return;busy=true;wifiDevice=null;device=null;EnableUsb();DetectButton.IsEnabled=false;Note(I18n.T("Buscando controladoras USB\u2026"));
  try {var found=await Task.Run(Usb.Find);device=found.Count==1?found[0]:null;if(device is not null)wifiDevice=null;Connection.Text=found.Count switch{0=>I18n.T("Nenhuma TC420 encontrada \u00B7 edi\u00E7\u00E3o offline dispon\u00EDvel"),1=>device!.Label+I18n.T(" encontrada \u00B7 USB \u00B7 integra\u00E7\u00E3o em valida\u00E7\u00E3o"),_=>I18n.T("Mais de uma TC420 encontrada \u00B7 conecte apenas uma")};Note(found.Count==1?I18n.T("Controladora detectada. Nenhum comando enviado."):I18n.T("Conecte a TC420 por USB e clique em Buscar USB."));}catch(Exception ex){device=null;App.Log(ex);Note(I18n.T("Falha na busca USB: ")+ex.Message);}finally{busy=false;DetectButton.IsEnabled=true;EnableUsb();}
 }
 void OpenWifi(object sender,RoutedEventArgs e)
 {
  if(busy)return;var dialog=Dialog(I18n.T("Conectar por Wi-Fi"));dialog.Width=650;dialog.MaxHeight=Math.Max(500,SystemParameters.WorkArea.Height-50);
  using var chooser=new WifiChooser();dialog.Content=chooser;
  chooser.Connected+=(_,_)=>dialog.DialogResult=true;
  dialog.Closing+=(_,a)=>{if(chooser.IsConfiguring){a.Cancel=true;MessageBox.Show(dialog,I18n.T("Aguarde a confirma\u00E7\u00E3o da configura\u00E7\u00E3o de rede antes de fechar."),"Lume");}};
  if(dialog.ShowDialog()==true&&chooser.SelectedDevice is WifiDevice selected){wifiDevice=selected;device=null;Connection.Text=selected.Label+I18n.T(" \u00B7 Wi-Fi \u00B7 integra\u00E7\u00E3o em valida\u00E7\u00E3o");Note(I18n.T("Controladora selecionada pela rede. Nenhuma programa\u00E7\u00E3o foi alterada."));EnableUsb();}
 }
 void EnableUsb(){bool enabled=(device!=null||wifiDevice!=null)&&!busy;UploadButton.IsEnabled=SyncButton.IsEnabled=ResumeButton.IsEnabled=enabled;ManualButton.IsEnabled=enabled&&wifiDevice is null;ManualButton.ToolTip=wifiDevice is null?null:I18n.T("Ajuste instant\u00E2neo dispon\u00EDvel via USB nesta vers\u00E3o.");WifiButton.IsEnabled=!busy;LanguageMenu.IsEnabled=!busy;}
 async Task UseUsb(Func<IControllerSession,CancellationToken,Task> action,string success,string? failureHint=null)
 {
  if(busy||(device is null&&wifiDevice is null))return;busy=true;EnableUsb();DetectButton.IsEnabled=false;operation=new();Progress.Visibility=Visibility.Visible;Progress.Value=0;Note(I18n.T("Comunicando com a TC420\u2026"));
  try {var selected=device;var wireless=wifiDevice;await Task.Run(async()=>{using IControllerSession session=wireless is null?new Usb(selected!):new WifiSession(wireless);await action(session,operation.Token);});Note(success);}
  catch(Exception ex){App.Log(ex);device=null;wifiDevice=null;Connection.Text=I18n.T("Comunica\u00E7\u00E3o interrompida \u00B7 busque a controladora novamente");MessageBox.Show(this,ex.Message+"\n\n"+(failureHint??I18n.T("Uma grava\u00E7\u00E3o interrompida pode deixar o perfil incompleto. Reenvie o perfil antes de ativ\u00E1-lo.")),I18n.T("Falha na comunica\u00E7\u00E3o"),MessageBoxButton.OK,MessageBoxImage.Warning);Note(I18n.T("Opera\u00E7\u00E3o interrompida. O projeto local foi preservado."));}
  finally{busy=false;operation.Dispose();operation=null;Progress.Visibility=Visibility.Collapsed;DetectButton.IsEnabled=true;EnableUsb();}
 }
 async void Upload(object sender,RoutedEventArgs e)
 {
  if(busy||(device is null&&wifiDevice is null)||!CommitTable()||!Save())return;
  var backupLibrary=new Library{Profiles=library.Profiles.Select(p=>p.Clone()).ToList(),Selected=library.Selected};
  var d=Dialog(I18n.T("Enviar programa\u00E7\u00E3o"));d.Width=580;d.MaxHeight=Math.Max(500,SystemParameters.WorkArea.Height-50);
  var chooser=new UploadChooser(backupLibrary.Profiles,backupLibrary.Selected,wifiDevice is null?device!.Label+" · USB":wifiDevice.Label+" · Wi-Fi");
  chooser.Confirmed+=(_,_)=>d.DialogResult=true;chooser.Cancelled+=(_,_)=>d.DialogResult=false;d.Content=chooser;
  if(d.ShowDialog()!=true||chooser.Plan is not UploadPlan plan)return;
  string backupPath=System.IO.Path.Combine(Store.Root,"backups","antes-do-envio-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N")[..6]+".json");
  try{Store.Save(backupLibrary,backupPath);}catch(Exception ex){App.Log(ex);MessageBox.Show(this,I18n.T("O backup local n\u00E3o p\u00F4de ser criado. Nenhum comando foi enviado \u00E0 controladora.\n\n")+ex.Message,I18n.T("Envio cancelado"));return;}
  PauseSimulation();
  var progress=new Progress<TransferProgress>(p=>{Progress.Value=p.Percent;Note(p.Message);});
  string success=plan.ClearExisting?$"{I18n.T("Mem\u00F3ria substitu\u00EDda \u00B7 ")}{plan.Items.Count}{I18n.T(" perfis recebidos nos n\u00FAmeros 1 a ")}{plan.Items.Count}{I18n.T(". Selecione o desejado na controladora.")}":$"{I18n.T("Perfil recebido no n\u00FAmero ")}{plan.Items[0].Slot+1}{I18n.T(". Selecione-o na controladora para usar.")}";
  await UseUsb((u,t)=>u.Upload(plan,progress,t),success,I18n.T("Backup da biblioteca local: ")+backupPath+I18n.T("\nEste backup n\u00E3o cont\u00E9m perfis que existiam apenas na mem\u00F3ria da controladora."));
 }
 async void Manual(object sender,RoutedEventArgs e){if(!CommitTable())return;var p=P.Clone();var values=Enumerable.Range(0,5).Select(c=>(byte)Math.Round(p.At(Preview.Value,c))).ToArray();if(MessageBox.Show(this,I18n.T("Aplicar agora ")+string.Join(" / ",values.Select(v=>v+"%"))+I18n.T(" nos canais CH1 a CH5?\n\nIsso altera a luz do aqu\u00E1rio. A pr\u00E9via deixa de ser apenas local. A integra\u00E7\u00E3o USB ainda est\u00E1 em valida\u00E7\u00E3o."),I18n.T("Aplicar aos LEDs"),MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;await UseUsb((u,t)=>u.Manual(p,values,t),I18n.T("Intensidades enviadas. Use Retomar programa para sair do ajuste manual."));}
 async void Resume(object sender,RoutedEventArgs e)=>await UseUsb((u,t)=>u.Resume(t),I18n.T("Comando para retomar o programa confirmado pela controladora."));
 async void Sync(object sender,RoutedEventArgs e){var clock=DateTime.Now;await UseUsb((u,t)=>u.Sync(clock,t),I18n.T("Hor\u00E1rio sincronizado com este PC \u00B7 ")+clock.ToString("dd/MM/yyyy HH:mm:ss"),I18n.T("N\u00E3o foi poss\u00EDvel confirmar o ajuste do rel\u00F3gio. Verifique a conex\u00E3o e busque a controladora novamente."));}
}
public class ScheduleRow(int index,int[] values)
{
 public int Index=>index;public string Time=>$"{index/2:00}:{index%2*30:00}";
 public int C1{get;set;}=values[0];public int C2{get;set;}=values[1];public int C3{get;set;}=values[2];public int C4{get;set;}=values[3];public int C5{get;set;}=values[4];
}
public sealed class IntensityBrushConverter : IValueConverter
{
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture){int v=value is int number?Math.Clamp(number,0,100):0;var c=(Color)ColorConverter.ConvertFromString((string)parameter);return new SolidColorBrush(Color.FromArgb((byte)(12+v*.42),c.R,c.G,c.B));}
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
