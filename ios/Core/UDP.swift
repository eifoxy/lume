import Foundation
import Darwin

public struct IPv4Route {
 public var address: UInt32
 public var prefix: Int
 public var interface: UInt32
 public init(address: UInt32, prefix: Int, interface: UInt32 = 0) { self.address = address; self.prefix = prefix; self.interface = interface }
 public static func parse(_ text: String) -> UInt32? { let fields = text.split(separator: ".", omittingEmptySubsequences: false); guard fields.count == 4 else { return nil }; var value: UInt32 = 0; for f in fields { guard !f.isEmpty, f.utf8.allSatisfy({ (48...57).contains($0) }), let b = UInt8(f) else { return nil }; value = (value << 8) | UInt32(b) }; return value }
 public static func text(_ value: UInt32) -> String { [24,16,8,0].map { String((value >> $0) & 255) }.joined(separator: ".") }
 public var mask: UInt32 { prefix == 0 ? 0 : UInt32.max << (32-prefix) }
 public func peer(_ ip: UInt32) -> Bool { let first = ip >> 24; guard (1...32).contains(prefix), ip != address, first > 0, first != 127, first < 224, ip & mask == address & mask else { return false }; if prefix <= 30 && (ip == address & mask || ip == (address & mask) | ~mask) { return false }; return true }
 public func targets(direct: String = "", cached: String = "") throws -> [UInt32] {
  guard (1...32).contains(prefix) else { throw LumeFailure("Rede IPv4 inválida.") }
  if !direct.isEmpty { guard let ip = Self.parse(direct), peer(ip) else { throw LumeFailure("O IP precisa ser de outro aparelho na mesma rede Wi-Fi.") }; return [ip] }
  var result = [UInt32](); func add(_ value: UInt32?) { if let value = value, peer(value), !result.contains(value) { result.append(value) } }
  add(Self.parse(cached)); add(Self.parse("192.168.4.1")); add((address & mask) | 1); let block = address & 0xffffff00; add(block | 1)
  // Bounded unicast-only discovery. No broadcast entitlement or broad subnet sweep.
  for host in UInt32(1)...254 { add(block | host) }; return result
 }
 public static func interfaces(named names: Set<String>) -> [IPv4Route] {
  var first: UnsafeMutablePointer<ifaddrs>?; guard getifaddrs(&first) == 0 else { return [] }; defer { freeifaddrs(first) }; var found = [IPv4Route](); var cursor = first
  while let p = cursor { defer { cursor = p.pointee.ifa_next }; let a = p.pointee; let name = String(cString: a.ifa_name); guard names.contains(name), a.ifa_flags & UInt32(IFF_UP) != 0, let raw = a.ifa_addr, raw.pointee.sa_family == sa_family_t(AF_INET), let maskRaw = a.ifa_netmask else { continue }; let ip = UnsafeRawPointer(raw).assumingMemoryBound(to: sockaddr_in.self).pointee.sin_addr.s_addr.bigEndian; let mask = UnsafeRawPointer(maskRaw).assumingMemoryBound(to: sockaddr_in.self).pointee.sin_addr.s_addr.bigEndian; let prefix = mask.nonzeroBitCount; guard prefix > 0, mask == UInt32.max << (32-prefix) else { continue }; found.append(IPv4Route(address: ip, prefix: prefix, interface: if_nametoindex(a.ifa_name))) }; return found
 }
}
public struct ReceivedDatagram { public let bytes: [UInt8]; public let ip: UInt32; public let port: UInt16 }
public final class UDPSocket {
 private var fd: Int32
 public private(set) var localPort: UInt16 = 0
 public init(address: UInt32, port: UInt16 = 5000, interface: UInt32 = 0) throws {
  fd = Darwin.socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP); guard fd >= 0 else { throw LumeFailure("Não foi possível abrir a comunicação UDP local.") }
  var a = sockaddr_in(); a.sin_len = UInt8(MemoryLayout<sockaddr_in>.size); a.sin_family = sa_family_t(AF_INET); a.sin_port = port.bigEndian; a.sin_addr.s_addr = address.bigEndian
  if interface != 0 { var index = interface; if setsockopt(fd, IPPROTO_IP, IP_BOUND_IF, &index, socklen_t(MemoryLayout.size(ofValue: index))) != 0 { Darwin.close(fd); fd = -1; throw LumeFailure("Não foi possível vincular a comunicação ao Wi-Fi.") } }
  let bound = withUnsafePointer(to: &a) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { Darwin.bind(fd, $0, socklen_t(MemoryLayout<sockaddr_in>.size)) } }
  guard bound == 0 else { Darwin.close(fd); fd = -1; throw LumeFailure("A porta UDP 5000 está ocupada. Feche o Pled ou outra janela de busca e tente novamente.") }
  var length = socklen_t(MemoryLayout<sockaddr_in>.size); _ = withUnsafeMutablePointer(to: &a) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { getsockname(fd,$0,&length) } }; localPort = a.sin_port.bigEndian
  var timeout = timeval(tv_sec: 0, tv_usec: 20_000); _ = setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &timeout, socklen_t(MemoryLayout<timeval>.size)); timeout.tv_usec = 500_000; _ = setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, &timeout, socklen_t(MemoryLayout<timeval>.size))
 }
 deinit { close() }
 public func close() { if fd >= 0 { Darwin.close(fd); fd = -1 } }
 public func send(_ bytes: [UInt8], to ip: UInt32, port: UInt16 = 5000) throws {
  var a = sockaddr_in(); a.sin_len = UInt8(MemoryLayout<sockaddr_in>.size); a.sin_family = sa_family_t(AF_INET); a.sin_port = port.bigEndian; a.sin_addr.s_addr = ip.bigEndian
  let sent = withUnsafePointer(to: &a) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { Darwin.sendto(fd, bytes, bytes.count, 0, $0, socklen_t(MemoryLayout<sockaddr_in>.size)) } }
  guard sent == bytes.count else { throw LumeFailure("Falha na comunicação UDP. Permita Rede Local nos Ajustes do Lume e mantenha o Wi-Fi conectado.") }
 }
 public func receive() throws -> ReceivedDatagram? {
  var bytes = [UInt8](repeating: 0, count: 65); var a = sockaddr_in(); var length = socklen_t(MemoryLayout<sockaddr_in>.size)
  let count = withUnsafeMutablePointer(to: &a) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { recvfrom(fd, &bytes, 65, 0, $0, &length) } }
  if count < 0 { if errno == EAGAIN || errno == EWOULDBLOCK || errno == EINTR { return nil }; throw LumeFailure("Falha na comunicação UDP local.") }
  return ReceivedDatagram(bytes: Array(bytes.prefix(count)), ip: a.sin_addr.s_addr.bigEndian, port: a.sin_port.bigEndian)
 }
}
public struct AckTimeout: Error, LocalizedError {
 public let validReply: Bool; public let count: Int
 public var errorDescription: String? { "Sem confirmação após o prazo de espera. Datagramas recebidos: \(count)." }
}
public enum Exchange {
 public static func run(socket: UDPSocket, target: UInt32, port: UInt16 = 5000, payload: [UInt8], id: String, accepted: [UInt8], smartOnly: Bool = false, timeout: TimeInterval = 2.5, guardCheck: () throws -> Void = {}) throws {
  try guardCheck(); for _ in 0..<32 { try guardCheck(); if try socket.receive() == nil { break } }
  try socket.send(ControllerProtocol.frame(payload,id), to: target, port: port); let deadline = ProcessInfo.processInfo.systemUptime + timeout; var received = 0; var valid = false
  while ProcessInfo.processInfo.systemUptime < deadline { try guardCheck(); guard let response = try socket.receive() else { continue }; received += 1; guard response.ip == target, response.port == port else { continue }; if accepted.contains(where: { ControllerProtocol.accepts(response.bytes, command: payload[0], status: $0, id: id, smartOnly: smartOnly) }) { return }; if ControllerProtocol.smart(response.bytes), Array(response.bytes[49..<61]) == (try? ControllerProtocol.identity(id)) { valid = true } }
  throw AckTimeout(validReply: valid, count: received)
 }
}
