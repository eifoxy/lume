import Foundation

public enum ProfileTransfer {
 // The transport is injected so complete transactions, dropped acknowledgments,
 // cancellation and finalization can be exercised without touching a controller.
 public static func run(profiles: [LightingProfile], replace: Bool, slot: Int, smart: Bool,
   exchange: ([UInt8], UInt8) throws -> Void, check: () throws -> Void,
   pause: () throws -> Void, progress: (String, Int) -> Void) throws {
  guard (1...16).contains(profiles.count), (0...15).contains(slot), replace || profiles.count == 1 else { throw LumeFailure("Perfil ou posição inválida.") }
  try profiles.forEach { try $0.validate() }
  let aliases = ControllerProtocol.aliases(profiles.map { $0.Name })
  var completed = 0, eraseAttempted = false, phase = "iniciar o envio"
  func transmit(_ p: [UInt8], _ status: UInt8, retry: Bool = false) throws {
   for attempt in 0..<(retry && smart ? 3 : 1) {
    try check()
    do { try exchange(p,status); return }
    catch let error as AckTimeout { if !retry || !smart || error.validReply || attempt == 2 { throw error }; try check(); try pause() }
   }
  }
  do {
   if replace { phase = "limpar perfis antigos"; try check(); eraseAttempted = true; try transmit([5,0xfe],0x82); if smart { for _ in 0..<14 { try check(); try pause() } } }
   for i in profiles.indices {
    phase = "iniciar perfil \(i+1)"; try transmit(ControllerProtocol.header(slot:replace ? i : slot,alias:aliases[i],smart:smart),0x80,retry:true)
    for row in 0..<48 { phase = "gravar perfil \(i+1), horário \(row+1)"; try transmit(ControllerProtocol.point(row,profiles[i].Values[row]),0x81,retry:true); progress("Perfil \(i+1) · horário \(row+1)/48",(i*48+row+1)*95/(profiles.count*48)); if smart { try check(); try pause() } }
    if smart { phase = "encerrar perfil \(i+1)"; try transmit([3,8],0x83) }; completed += 1
   }
   if smart { phase = "finalizar o conjunto"; try transmit([4,10],0x91) }; progress("Finalizando a transferência…",100)
  } catch {
   let warning = eraseAttempted ? "A memória pode ter sido apagada ou estar incompleta." : "A gravação/finalização pode estar incompleta."
   throw LumeFailure("Falha ao \(phase). \(completed)/\(profiles.count) perfis tiveram os horários recebidos. \(warning) Reconecte e reenvie. \(error.localizedDescription)")
  }
 }
}
