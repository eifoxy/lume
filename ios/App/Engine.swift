import Foundation
import Network

final class ControllerEngine {
 let store: LibraryStore
 var emit: (String, Any) -> Void = { _,_ in }
 var finishedPreview: () -> Void = {}
 private let worker = DispatchQueue(label: "Lume.network", qos: .userInitiated)
 private let monitor = NWPathMonitor(requiredInterfaceType: .wifi)
 private let lock = NSLock()
 private var wifiNames = Set<String>()
 private var busy = false, cancelled = false, foreground = true, abort = false
 private var liveToken = "", stopLive = false
 private var sample = (minute: 0, values: [0,0,0,0,0], updated: 0.0)
 private(set) var diagnostic = "Lume iOS 0.1.0. Faça uma busca para verificar a comunicação Wi-Fi."
 init(store: LibraryStore) { self.store = store; monitor.pathUpdateHandler = { [weak self] path in guard let self = self else { return }; self.lock.lock(); self.wifiNames = Set(path.availableInterfaces.filter { $0.type == .wifi }.map { $0.name }); self.lock.unlock() }; monitor.start(queue: DispatchQueue(label: "Lume.path")) }
 deinit { monitor.cancel() }
 func setForeground(_ value: Bool) { lock.lock(); foreground = value; if !value { if liveToken.isEmpty { cancelled = true } else { stopLive = true } }; lock.unlock(); emit("foreground", value) }
 func expireBackgroundTask() { lock.lock(); abort = true; stopLive = true; lock.unlock() }
 func cancel() { lock.lock(); if liveToken.isEmpty { cancelled = true } else { stopLive = true }; lock.unlock() }
 func isLive() -> Bool { lock.lock(); defer { lock.unlock() }; return !liveToken.isEmpty }
 private func check(resuming: Bool = false) throws { lock.lock(); defer { lock.unlock() }; if abort || (!resuming && (cancelled || !foreground)) { throw LumeFailure("Operação interrompida. Se estava gravando, a memória pode estar incompleta.") } }
 private func route() throws -> IPv4Route { lock.lock(); let names = wifiNames; lock.unlock(); guard let route = IPv4Route.interfaces(named: names).first else { throw LumeFailure("Conecte o celular ao Wi-Fi do aparelho ou da sua casa. Se a rede estiver sem internet, escolha permanecer conectado.") }; return route }
 private func guardRoute(_ route: IPv4Route, resuming: Bool = false) throws { try check(resuming: resuming); guard try self.route().address == route.address else { throw LumeFailure("O Wi-Fi mudou ou desconectou durante a operação.") } }
 private func target(_ device: WiFiDevice, route: IPv4Route) throws -> UInt32 { _ = try ControllerProtocol.identity(device.id); guard let ip = IPv4Route.parse(device.ip), route.peer(ip) else { throw LumeFailure("Selecione um dispositivo da mesma rede Wi-Fi.") }; return ip }
 private func acquire() -> Bool { lock.lock(); defer { lock.unlock() }; guard !busy, foreground else { return false }; busy = true; cancelled = false; abort = false; return true }
 private func release() { lock.lock(); busy = false; lock.unlock() }
 private func job(quiet: Bool = false, _ body: @escaping () throws -> Void) { guard acquire() else { emit(quiet ? "searching" : "error", quiet ? false : "Aguarde a operação atual."); return }; emit(quiet ? "searching" : "busy", true); worker.async { defer { self.release(); self.emit(quiet ? "searching" : "busy",false) }; do { try body() } catch { if !quiet { self.emit("error",error.localizedDescription) } } } }
 private func report(_ text: String) { diagnostic = text; emit("diagnostic",text) }
 private func progress(_ text: String, _ percent: Int) { emit("progress",["message":text,"percent":percent]) }
 private func send(_ p: [UInt8], _ status: [UInt8], socket: UDPSocket, route: IPv4Route, device: WiFiDevice, resuming: Bool = false) throws { try Exchange.run(socket: socket, target: target(device,route:route), payload: p, id: device.id, accepted: status, smartOnly: resuming, guardCheck: { try self.guardRoute(route,resuming:resuming) }) }
 func discover(_ direct: String, automatic: Bool) {
  job(quiet: automatic) {
   var found = [WiFiDevice](); var sent = 0, received = 0, rejected = 0; var note = "Lume iOS 0.1.0 · descoberta unicast\n"
   defer { self.emit(automatic ? "autoDevices" : "devices", found.map { ["id":$0.id,"ip":$0.ip,"protocol":$0.kind] }); if !automatic { self.report(note+"\nConsultas enviadas: \(sent)\nDatagramas recebidos: \(received)\nNão reconhecidos: \(rejected)\nControladoras: \(found.count)\nSomente descoberta; nenhuma configuração ou programação enviada.") } }
   do {
    let route = try self.route(), socket = try UDPSocket(address: route.address, interface: route.interface); defer { socket.close() }
    let cached = UserDefaults.standard.string(forKey:"lastDeviceIP") ?? "", preferred = UserDefaults.standard.string(forKey:"device") ?? ""; let targets = try route.targets(direct:direct,cached:cached)
    note += "Wi-Fi IPv4: \(IPv4Route.text(route.address))/\(route.prefix)\nConsultas diretas limitadas: \(targets.count) · UDP 5000\n"; let start = ProcessInfo.processInfo.systemUptime; var next = start, index = 0
    while ProcessInfo.processInfo.systemUptime-start < (direct.isEmpty ? 14 : 4) {
     try self.guardRoute(route); let now = ProcessInfo.processInfo.systemUptime
     if index < targets.count && now >= next { try socket.send(ControllerProtocol.discovery(),to:targets[index]); sent += 1; index += 1; next = now+0.04 }
     if let response = try socket.receive() { received += 1; if route.peer(response.ip), let (id,kind) = ControllerProtocol.discovered(response.bytes) { if let old = found.firstIndex(where: { $0.id.caseInsensitiveCompare(id) == .orderedSame }) { found[old] = WiFiDevice(id:id,ip:IPv4Route.text(response.ip),kind:kind) } else { found.append(WiFiDevice(id:id,ip:IPv4Route.text(response.ip),kind:kind)) }; if automatic && id.caseInsensitiveCompare(preferred) == .orderedSame { break } } else { rejected += 1 } }
     if index == targets.count && now > next+0.8 { break }; if !automatic { self.progress("Buscando no Wi-Fi · \(found.count) encontrada(s)",min(99,Int((now-start)*100/14))) }
    }
    if !automatic { self.emit(found.isEmpty ? "error" : "success",found.isEmpty ? "Nenhuma resposta recebida. Permita Rede Local nos Ajustes do Lume; tente o IP conhecido ou mantenha o Wi-Fi conectado." : "Busca concluída.") }
   } catch { note += "Falha: \(error.localizedDescription)"; throw error }
  }
 }
 func configure(_ device: WiFiDevice, ssid: String, password: String, wpa2: Bool, aes: Bool) {
  job { guard self.store.recovery().isEmpty else { throw LumeFailure("Retome o programa antes de iniciar outra prévia.") }; var frames = try ControllerProtocol.provision(id:device.id,ssid:ssid,password:password,wpa2:wpa2,aes:aes); defer { for i in frames.indices { frames[i] = Array(repeating:0,count:64) } }; let route = try self.route(), socket = try UDPSocket(address:route.address,interface:route.interface); defer { socket.close() }; self.progress("Configurando o roteador…",20)
   try Exchange.run(socket:socket,target:self.target(device,route:route),payload:Array(frames[0][2..<49]),id:device.id,accepted:device.kind == "smart" ? [0x70] : [0x70,0x74],guardCheck:{ try self.guardRoute(route) })
   self.progress("Enviando credenciais…",60); do { try Exchange.run(socket:socket,target:self.target(device,route:route),payload:Array(frames[1][2..<49]),id:device.id,accepted:[0x72],guardCheck:{ try self.guardRoute(route) }) } catch { throw LumeFailure("Credenciais enviadas, mas sem confirmação final. O aparelho pode já ter trocado de rede. Conecte o celular ao Wi-Fi de casa e faça uma nova busca antes de tentar configurar novamente.") }
   self.emit("configured","Configuração confirmada. Agora conecte o celular à rede de casa e busque a controladora novamente.")
  }
 }
 func command(_ device: WiFiDevice, _ name: String) {
  job { let pending = self.store.recovery(); if !pending.isEmpty { let original = try JSONDecoder().decode(WiFiDevice.self,from:Data(pending.utf8)); guard name == "resume", original.id.caseInsensitiveCompare(device.id) == .orderedSame else { throw LumeFailure("Retome o programa antes de iniciar outra prévia.") } }; let route = try self.route(), socket = try UDPSocket(address:route.address,interface:route.interface); defer { socket.close() }
   if name == "clock" { try self.send(ControllerProtocol.clock(),[0x85],socket:socket,route:route,device:device); self.emit("success","Relógio sincronizado com a data e a hora local do celular.") }
   else if name == "resume" { try self.send([3,8],[0x83],socket:socket,route:route,device:device); try self.store.clear(device.id); self.emit("previewRecovered",true); self.emit("success","Retomada do programa confirmada.") }
   else { throw LumeFailure("Comando desconhecido.") }
  }
 }
 func upload(_ device: WiFiDevice, text: String, replace: Bool, selected: Int, slot: Int) {
  job {
   guard self.store.recovery().isEmpty else { throw LumeFailure("Retome o programa antes de iniciar outra prévia.") }; let library = try ProfileLibrary.decode(Data(text.utf8)); guard replace ? library.Profiles.count <= 16 : ((0..<library.Profiles.count).contains(selected) && (0...15).contains(slot)) else { throw LumeFailure("Perfil ou posição inválida.") }; let profiles = replace ? library.Profiles : [library.Profiles[selected]], aliases = ControllerProtocol.aliases(profiles.map { $0.Name }); try self.store.backup(library.encoded())
   var complete = 0, erased = false, phase = "iniciar o envio"; let smart = device.kind == "smart"
   do { let route = try self.route(), socket = try UDPSocket(address:route.address,interface:route.interface); defer { socket.close() }
    func transmit(_ p: [UInt8], _ ack: UInt8, retry: Bool = false) throws { for attempt in 0..<(retry && smart ? 3 : 1) { do { try self.send(p,[ack],socket:socket,route:route,device:device); return } catch let e as AckTimeout { if !retry || !smart || e.validReply || attempt == 2 { throw e }; try self.guardRoute(route); Thread.sleep(forTimeInterval:0.15) } } }
    if replace { phase = "limpar perfis antigos"; try transmit([5,0xfe],0x82); erased = true; if smart { for _ in 0..<14 { try self.guardRoute(route); Thread.sleep(forTimeInterval:0.15) } } }
    for i in profiles.indices { phase = "iniciar perfil \(i+1)"; try transmit(ControllerProtocol.header(slot:replace ? i : slot,alias:aliases[i],smart:smart),0x80,retry:true)
     for row in 0..<48 { phase = "gravar perfil \(i+1), horário \(row+1)"; try transmit(ControllerProtocol.point(row,profiles[i].Values[row]),0x81,retry:true); self.progress("Perfil \(i+1) · horário \(row+1)/48",(i*48+row+1)*95/(profiles.count*48)); if smart { try self.guardRoute(route); Thread.sleep(forTimeInterval:0.15) } }
     if smart { phase = "encerrar perfil \(i+1)"; try transmit([3,8],0x83) }; complete += 1
    }
    if smart { phase = "finalizar o conjunto"; try transmit([4,10],0x91) }; self.progress("Finalizando a transferência…",100); self.emit("success","\(complete) perfil(is) confirmado(s). Confira no aparelho o perfil ativo."); self.emit("uploadComplete",true)
   } catch { let warning = erased ? "A memória pode ter sido apagada ou estar incompleta." : "A gravação/finalização pode estar incompleta."; let message = "Falha ao \(phase). \(complete)/\(profiles.count) perfis tiveram os horários recebidos. \(warning) Reconecte e reenvie. \(error.localizedDescription)"; self.report("Lume iOS 0.1.0 · envio de perfis\n"+message); throw LumeFailure(message) }
  }
 }
 func updatePreview(token: String, minute: Int, values: [Int]) throws { guard (0...1439).contains(minute) else { throw LumeFailure("Horário inválido") }; _ = try ControllerProtocol.levels(values); lock.lock(); defer { lock.unlock() }; guard liveToken == token, !stopLive else { throw LumeFailure("Prévia não está ativa.") }; sample = (minute,values,ProcessInfo.processInfo.systemUptime) }
 func stopPreview(_ token: String) { lock.lock(); if liveToken == token { stopLive = true }; lock.unlock() }
 private func liveState(_ state: String, token: String, message: String, pending: Bool) { emit("previewState",["state":state,"token":token,"message":message,"needsRestore":pending]) }
 func startPreview(_ device: WiFiDevice, alias: String, minute: Int, values: [Int]) throws -> String {
  guard device.kind == "smart" else { throw LumeFailure("A prévia Wi-Fi está disponível apenas para Smart Timer.") }; _ = try ControllerProtocol.previewStart(alias); _ = try ControllerProtocol.levels(values); guard (0...1439).contains(minute) else { throw LumeFailure("Horário inválido") }; guard store.recovery().isEmpty else { throw LumeFailure("Retome o programa antes de iniciar outra prévia.") }; guard acquire() else { throw LumeFailure("Aguarde a operação atual.") }
  let token = UUID().uuidString; lock.lock(); liveToken = token; stopLive = false; sample = (minute,values,ProcessInfo.processInfo.systemUptime); lock.unlock(); liveState("starting",token:token,message:"Preparando prévia na luminária…",pending:false)
  worker.async {
   var attempted = false, restored = false, frames = 0; var failure = ""
   do { let route = try self.route(), socket = try UDPSocket(address:route.address,interface:route.interface); defer { socket.close() }; let ip = try self.target(device,route:route)
    do {
     try self.guardRoute(route); self.lock.lock(); let stopped = self.stopLive, age = ProcessInfo.processInfo.systemUptime-self.sample.updated; self.lock.unlock(); if !stopped { guard age <= 3 else { throw LumeFailure("A prévia parou de receber atualizações do celular.") }; try self.store.arm(device); attempted = true; let start = try ControllerProtocol.frame(ControllerProtocol.previewStart(alias),device.id); try socket.send(start,to:ip); Thread.sleep(forTimeInterval:0.3); try self.guardRoute(route); self.lock.lock(); let stop = self.stopLive; self.lock.unlock(); if !stop { try socket.send(start,to:ip); self.liveState("running",token:token,message:"Prévia ao vivo · enviando pelo Wi-Fi",pending:false) }
      var previous = [Int](); while true { self.lock.lock(); let stop = self.stopLive, current = self.sample; self.lock.unlock(); if stop { break }; try self.guardRoute(route); guard ProcessInfo.processInfo.systemUptime-current.updated <= 3 else { throw LumeFailure("A prévia parou de receber atualizações do celular.") }; if previous != current.values { try socket.send(ControllerProtocol.frame(ControllerProtocol.preview(current.values),device.id),to:ip); previous = current.values; frames += 1; self.emit("previewFrame",["token":token,"minute":current.minute,"levels":current.values]) }; Thread.sleep(forTimeInterval:0.2) }
     }
    } catch { failure = error.localizedDescription }
    if attempted { self.liveState("stopping",token:token,message:"Retomando programação no aparelho…",pending:true); do { try self.send([3,8],[0x83],socket:socket,route:route,device:device,resuming:true); restored = true; try self.store.clear(device.id) } catch { failure = error.localizedDescription } }
   } catch { failure = error.localizedDescription }
   let pending = !self.store.recovery().isEmpty; self.lock.lock(); self.liveToken = ""; self.busy = false; self.lock.unlock(); let text = pending ? "Não foi possível confirmar a retomada. A luminária pode continuar em modo manual. Reconecte e use Retomar programa." : restored ? "Prévia encerrada. Retomada do programa confirmada." : "Prévia não iniciada."
   self.report("Lume iOS 0.1.0 · prévia ao vivo\nDestino: \(device.ip)\nQuadros de intensidade enviados: \(frames)\nRetomada confirmada: \(restored)\n\(failure)"); self.liveState(pending ? "failed" : "idle",token:token,message:text+(failure.isEmpty ? "" : " "+failure),pending:pending); self.finishedPreview()
  }; return "OK:"+token
 }
}
