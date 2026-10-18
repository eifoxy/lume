using System.IO;
using System.Windows;
namespace Lume;
public partial class App : Application
{
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  DispatcherUnhandledException += (_, a) => { Log(a.Exception); MessageBox.Show("Não foi possível concluir a operação. Seu último salvamento está preservado.\n\n" + a.Exception.Message, "Lume", MessageBoxButton.OK, MessageBoxImage.Warning); a.Handled = true; };
  if(e.Args.Contains("--self-test")) { try { SelfTest.Run(); Shutdown(0); } catch(Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"test-failure.txt"),ex.ToString()); Shutdown(1); } return; }
  if(e.Args.Contains("--ui-test")) {try {var window=new MainWindow();window.CheckUi();Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-test-failure.txt"),ex.ToString());Shutdown(1);}return;}
  if(e.Args.Contains("--wifi-scan")) {try {var found=Task.Run(async()=>await WifiDiscovery.Search(LocalRoute.List(),null,new Progress<DiscoveryUpdate>(_=>{}),CancellationToken.None)).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"wifi-scan-results.txt"),$"{found.Count} controladora(s) encontrada(s).\n"+string.Join("\n",found.Select(d=>d.Label))+"\nSomente consulta de descoberta; nenhuma programação ou rede alterada.");Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"wifi-scan-results.txt"),ex.Message);Shutdown(1);}return;}
  new MainWindow().Show();
 }
 public static void Log(Exception ex) { try { Directory.CreateDirectory(Store.Root); File.AppendAllText(Path.Combine(Store.Root,"diagnostico.log"),DateTime.Now.ToString("O")+" "+ex+Environment.NewLine); } catch { } }
}
