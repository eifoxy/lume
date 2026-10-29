import UIKit
import WebKit
import UniformTypeIdentifiers
import AVFoundation

@main final class AppDelegate: UIResponder, UIApplicationDelegate {
 var window: UIWindow?
 var controller: LumeViewController?
 private var backgroundTask: UIBackgroundTaskIdentifier = .invalid
 func application(_ application: UIApplication, didFinishLaunchingWithOptions options: [UIApplication.LaunchOptionsKey:Any]? = nil) -> Bool { let vc = LumeViewController(); controller = vc; let window = UIWindow(frame:UIScreen.main.bounds); window.rootViewController = vc; window.makeKeyAndVisible(); self.window = window; vc.previewFinished = { [weak self] in self?.endBackgroundTask() }; return true }
 func applicationWillResignActive(_ application: UIApplication) { controller?.cancelQr(); if controller?.engine?.isLive() == true { backgroundTask = application.beginBackgroundTask(withName:"LumePreviewResume") { [weak self] in self?.controller?.engine?.expireBackgroundTask(); self?.endBackgroundTask() } }; controller?.engine?.setForeground(false) }
 func applicationDidBecomeActive(_ application: UIApplication) { controller?.engine?.setForeground(true) }
 private func endBackgroundTask() { DispatchQueue.main.async { if self.backgroundTask != .invalid { UIApplication.shared.endBackgroundTask(self.backgroundTask); self.backgroundTask = .invalid } } }
}

final class LumeViewController: UIViewController, WKUIDelegate, WKNavigationDelegate, UIDocumentPickerDelegate {
 private var web: WKWebView!
 private var store: LibraryStore?
 private(set) var engine: ControllerEngine?
 var previewFinished: () -> Void = {}
 private var exportedURL: URL?
 private var scanner: QrScanner?
 func cancelQr() { if let camera = scanner { scanner = nil; camera.finish(notify:true) } }
 private func scanQr(_ labels: [String]) {
  guard scanner == nil, presentedViewController == nil, engine?.isWorking() == false else { emit("qrCancelled",true); return }
  func open(_ granted: Bool) { DispatchQueue.main.async { guard granted, UIApplication.shared.applicationState == .active else { self.emit("qrCancelled",true); if !granted { self.emit("error","Permita a câmera para ler o QR do PC.") }; return }; let camera = QrScanner(); camera.labels = labels; camera.modalPresentationStyle = .fullScreen; camera.onRead = { [weak self] text in self?.emit("qr",text) }; camera.onError = { [weak self] text in self?.emit("error",text) }; camera.onCancel = { [weak self] in self?.scanner = nil; self?.emit("qrCancelled",true) }; self.scanner = camera; self.present(camera,animated:true) } }
  if ProcessInfo.processInfo.arguments.contains("--smoke-test") { emit("qrCancelled",true); return }
  if AVCaptureDevice.authorizationStatus(for:.video) == .notDetermined { AVCaptureDevice.requestAccess(for:.video,completionHandler:open) } else { open(AVCaptureDevice.authorizationStatus(for:.video) == .authorized) }
 }
 private var webRoot: URL!
 override var preferredStatusBarStyle: UIStatusBarStyle { .lightContent }
 override func viewDidLoad() {
  super.viewDidLoad(); view.backgroundColor = UIColor(red:0.063,green:0.11,blue:0.137,alpha:1)
  webRoot = Bundle.main.bundleURL.appendingPathComponent("Web",isDirectory:true)
  let config = WKWebViewConfiguration(); config.preferences.javaScriptCanOpenWindowsAutomatically = false; web = WKWebView(frame:.zero,configuration:config); web.isOpaque = false; web.backgroundColor = view.backgroundColor; web.scrollView.backgroundColor = view.backgroundColor; web.uiDelegate = self; web.navigationDelegate = self; web.translatesAutoresizingMaskIntoConstraints = false; view.addSubview(web)
  NSLayoutConstraint.activate([web.topAnchor.constraint(equalTo:view.safeAreaLayoutGuide.topAnchor),web.bottomAnchor.constraint(equalTo:view.safeAreaLayoutGuide.bottomAnchor),web.leadingAnchor.constraint(equalTo:view.safeAreaLayoutGuide.leadingAnchor),web.trailingAnchor.constraint(equalTo:view.safeAreaLayoutGuide.trailingAnchor)])
  do { let store = try LibraryStore(); self.store = store; let engine = ControllerEngine(store:store); self.engine = engine; engine.emit = { [weak self] type,data in self?.emit(type,data) }; engine.finishedPreview = { [weak self] in DispatchQueue.main.async { self?.previewFinished() } } } catch { showMessage(error.localizedDescription) }
  web.loadFileURL(webRoot.appendingPathComponent("index.html"),allowingReadAccessTo:webRoot)
 }
 private func emit(_ type: String, _ data: Any) { guard let encoded = try? JSONSerialization.data(withJSONObject:[type,data]), let string = String(data:encoded,encoding:.utf8) else { return }; DispatchQueue.main.async { self.web?.evaluateJavaScript("window.nativeEvent&&window.nativeEvent(...\(string))",completionHandler:nil) } }
 func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
  guard ProcessInfo.processInfo.arguments.contains("--smoke-test") else { return }
  let script = """
  (()=>{const before=JSON.stringify(library);const checks=[];
   for(const [code,title]of [['en','A day of light.'],['es','Un día de luz.'],['pt-BR','Um dia de luz.']]){LumeI18n.set(code);if(document.querySelector('#curves h1').textContent!==title)throw Error('Language '+code);checks.push(code);}
   if(before!==JSON.stringify(library)||library.Profiles[0].Values.length!==48)throw Error('Library changed');
   if(!document.querySelector('#aquarium').children.length)throw Error('Reef empty');
   if(!document.querySelector('#chart').children.length)throw Error('Curve empty');
   if(Lume.previewRecovery()!=='')throw Error('Unexpected recovery marker');
   if(!document.body.textContent.includes('iOS 0.2.0'))throw Error('Wrong platform assets');
   return {ok:true,languages:checks,profiles:library.Profiles.length,rows:48,bridge:typeof Lume.upload,network:'disabled for smoke test'};
  })()
  """
  webView.evaluateJavaScript(script) { value,error in
   var result: [String:Any] = error.map { ["ok":false,"error":$0.localizedDescription] } ?? (value as? [String:Any] ?? ["ok":false,"error":"No UI result"])
   result["runtime_ios"] = UIDevice.current.systemVersion
   result["minimum_ios"] = Bundle.main.object(forInfoDictionaryKey:"MinimumOSVersion") ?? "unknown"
   if let data = try? JSONSerialization.data(withJSONObject:result,options:.prettyPrinted), let documents = FileManager.default.urls(for:.documentDirectory,in:.userDomainMask).first { try? data.write(to:documents.appendingPathComponent("ios-smoke.json"),options:.atomic) }
  }
 }
 func webView(_ webView: WKWebView, decidePolicyFor action: WKNavigationAction, decisionHandler: @escaping (WKNavigationActionPolicy)->Void) { guard let url = action.request.url, url.isFileURL, url.standardizedFileURL.path.hasPrefix(webRoot.standardizedFileURL.path+"/") else { decisionHandler(.cancel); return }; decisionHandler(.allow) }
 func webView(_ webView: WKWebView, runJavaScriptTextInputPanelWithPrompt prompt: String, defaultText: String?, initiatedByFrame frame: WKFrameInfo, completionHandler: @escaping (String?)->Void) {
  guard prompt == "__LUME_NATIVE__", frame.isMainFrame, let url = frame.request.url, url.isFileURL, url.standardizedFileURL.path == webRoot.appendingPathComponent("index.html").standardizedFileURL.path, let text = defaultText, text.utf8.count <= 2_100_000 else { completionHandler(nil); return }
  do { guard let request = try JSONSerialization.jsonObject(with:Data(text.utf8)) as? [String:Any], let method = request["method"] as? String, let args = request["args"] as? [Any] else { throw LumeFailure("Comando inválido") }; let value = try call(method,args); let data = try JSONSerialization.data(withJSONObject:["value":value]); completionHandler(String(data:data,encoding:.utf8)) }
  catch { let data = try? JSONSerialization.data(withJSONObject:["error":error.localizedDescription]); completionHandler(data.flatMap { String(data:$0,encoding:.utf8) }) }
 }
 private func call(_ method: String, _ args: [Any]) throws -> Any {
  func text(_ index: Int) throws -> String { guard args.indices.contains(index),let value = args[index] as? String else { throw LumeFailure("Comando inválido") }; return value }
  func number(_ index: Int) throws -> Int { guard args.indices.contains(index),let value = args[index] as? NSNumber,value.doubleValue.isFinite,value.doubleValue.rounded() == value.doubleValue else { throw LumeFailure("Comando inválido") }; return value.intValue }
  func bool(_ index: Int) throws -> Bool { guard args.indices.contains(index),let value = args[index] as? Bool else { throw LumeFailure("Comando inválido") }; return value }
  func device() throws -> WiFiDevice { try JSONDecoder().decode(WiFiDevice.self,from:Data(text(0).utf8)) }
  guard let store = store,let engine = engine else { throw LumeFailure("Não foi possível criar o backup local.") }
  switch method {
  case "scanQr": scanQr(try [text(0),text(1),text(2)]); return NSNull()
  case "qrProgress": scanner?.update(try text(0)); return NSNull()
  case "stopQr": if let camera = scanner { scanner = nil; camera.finish(notify:false) }; return NSNull()
  case "loadLibrary": do { return try store.load() } catch { let value = ["error":"Biblioteca não carregada. O arquivo original foi preservado: "+error.localizedDescription]; return String(decoding:try JSONSerialization.data(withJSONObject:value),as:UTF8.self) }
  case "saveLibrary": do { try store.save(text(0)); return "" } catch { return error.localizedDescription }
  case "languagePreference": return UserDefaults.standard.string(forKey:"language") ?? "auto"
  case "deviceLanguage": return Locale.preferredLanguages.first ?? "en"
  case "saveLanguage": let value = try text(0); guard ["auto","pt-BR","en","es"].contains(value) else { throw LumeFailure("Invalid language") }; UserDefaults.standard.set(value,forKey:"language"); return ""
  case "preferredDevice": return UserDefaults.standard.string(forKey:"device") ?? ""
  case "rememberDevice": let id = try text(0); _ = try ControllerProtocol.identity(id); UserDefaults.standard.set(id,forKey:"device"); return NSNull()
  case "discover": engine.discover(try text(0),automatic:false); return NSNull()
  case "discoverAutomatic": engine.discover("",automatic:true); return NSNull()
  case "wifi": showWiFiHelp(); return NSNull()
  case "cancel": engine.cancel(); return NSNull()
  case "configure": engine.configure(try device(),ssid:try text(1),password:try text(2),wpa2:try bool(3),aes:try bool(4)); return NSNull()
  case "command": engine.command(try device(),try text(1)); return NSNull()
  case "upload": engine.upload(try device(),text:try text(1),replace:try bool(2),selected:try number(3),slot:try number(4)); return NSNull()
  case "previewRecovery": return store.recovery()
  case "startPreview": do { return try engine.startPreview(device(),alias:text(1),minute:number(2),values:(3...7).map { try number($0) }) } catch { return error.localizedDescription }
  case "previewLevels": do { try engine.updatePreview(token:text(0),minute:number(1),values:(2...6).map { try number($0) }); return "" } catch { engine.cancel(); return error.localizedDescription }
  case "stopPreview": engine.stopPreview(try text(0)); return NSNull()
  case "copyDiagnostic": UIPasteboard.general.string = String(try text(0).prefix(40_000)); emit("success","Diagnóstico copiado. Ele não contém senha nem nome da rede."); return NSNull()
  case "exportLibrary": let data = try ProfileLibrary.decode(Data(text(0).utf8)).encoded(); let file = FileManager.default.temporaryDirectory.appendingPathComponent("Lume-\(UUID().uuidString).json"); try data.write(to:file,options:.atomic); exportedURL = file; let picker = UIDocumentPickerViewController(forExporting:[file],asCopy:true); picker.delegate = self; present(picker,animated:true); return NSNull()
  case "importLibrary": let picker = UIDocumentPickerViewController(forOpeningContentTypes:[.json,.data],asCopy:true); picker.delegate = self; picker.allowsMultipleSelection = false; present(picker,animated:true); return NSNull()
  default: throw LumeFailure("Comando desconhecido.")
  }
 }
 private func showMessage(_ text: String) { let alert = UIAlertController(title:"Lume",message:text,preferredStyle:.alert); alert.addAction(UIAlertAction(title:"OK",style:.default)); present(alert,animated:true) }
 private func showWiFiHelp() { let code = UserDefaults.standard.string(forKey:"language") ?? "auto"; let language = (code == "auto" ? Locale.preferredLanguages.first ?? "en" : code).lowercased(); let message = language.hasPrefix("pt") ? "Abra Ajustes > Wi-Fi e conecte-se à rede do aparelho ou da sua casa. Mesmo sem internet, permaneça conectado. Depois volte ao Lume. Em Ajustes > Lume, permita Rede Local." : language.hasPrefix("es") ? "Abre Ajustes > Wi-Fi y conecta con la red del aparato o de casa, incluso sin internet. Vuelve a Lume. En Ajustes > Lume, permite Red local." : "Open Settings > Wi-Fi and join the device or home network, even without internet. Return to Lume. In Settings > Lume, allow Local Network."; let alert = UIAlertController(title:"Wi-Fi",message:message,preferredStyle:.alert); alert.addAction(UIAlertAction(title:"OK",style:.cancel)); alert.addAction(UIAlertAction(title:language.hasPrefix("pt") ? "Permissões do Lume" : language.hasPrefix("es") ? "Permisos de Lume" : "Lume permissions",style:.default) { _ in if let url = URL(string:UIApplication.openSettingsURLString) { UIApplication.shared.open(url) } }); present(alert,animated:true) }
 func documentPicker(_ controller: UIDocumentPickerViewController, didPickDocumentsAt urls: [URL]) { if let exported = exportedURL { try? FileManager.default.removeItem(at:exported); exportedURL = nil; emit("success","Biblioteca exportada."); return }; guard let url = urls.first else { return }; let granted = url.startAccessingSecurityScopedResource(); defer { if granted { url.stopAccessingSecurityScopedResource() } }; do { let size = try url.resourceValues(forKeys:[.fileSizeKey]).fileSize ?? 0; guard size <= 2_000_000 else { throw LumeFailure("Arquivo muito grande.") }; let library = try ProfileLibrary.importFile(Data(contentsOf:url)); let object = try JSONSerialization.jsonObject(with:library.encoded()); emit("import",object) } catch { emit("error",error.localizedDescription) } }
 func documentPickerWasCancelled(_ controller: UIDocumentPickerViewController) { if let url = exportedURL { try? FileManager.default.removeItem(at:url); exportedURL = nil } }
}
