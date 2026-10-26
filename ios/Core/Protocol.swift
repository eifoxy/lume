import Foundation

public struct LumeFailure: Error, LocalizedError {
 public let message: String
 public init(_ message: String) { self.message = message }
 public var errorDescription: String? { message }
}
public struct WiFiDevice: Codable, Equatable {
 public var id: String
 public var ip: String
 public var kind: String
 enum CodingKeys: String, CodingKey { case id, ip; case kind = "protocol" }
 public init(id: String, ip: String, kind: String) { self.id = id; self.ip = ip; self.kind = kind }
}
public struct LightingProfile: Codable {
 public var Name: String
 public var Channels: [String]
 public var Values: [[Int]]
 public func validate() throws {
  guard !Name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, Name.utf16.count <= 80, Channels.count == 5, Channels.allSatisfy({ !$0.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty && $0.utf16.count <= 40 }), Values.count == 48, Values.allSatisfy({ $0.count == 5 && $0.allSatisfy({ (0...100).contains($0) }) }) else { throw LumeFailure("Perfil inválido: use 48 horários com cinco valores de 0 a 100.") }
 }
}
public struct ProfileLibrary: Codable {
 public var Version: Int = 1
 public var Profiles: [LightingProfile]
 public var Selected: Int
 enum CodingKeys: String, CodingKey { case Version, Profiles, Selected }
 public init(Profiles: [LightingProfile], Selected: Int = 0) { self.Profiles = Profiles; self.Selected = Selected }
 public init(from decoder: Decoder) throws { let c = try decoder.container(keyedBy: CodingKeys.self); Version = try c.decodeIfPresent(Int.self, forKey: .Version) ?? 1; Profiles = try c.decode([LightingProfile].self, forKey: .Profiles); Selected = try c.decodeIfPresent(Int.self, forKey: .Selected) ?? 0 }
 public static func decode(_ data: Data) throws -> ProfileLibrary {
  guard data.count <= 2_000_000 else { throw LumeFailure("Arquivo muito grande.") }
  var value = try JSONDecoder().decode(ProfileLibrary.self, from: data)
  guard (1...100).contains(value.Profiles.count) else { throw LumeFailure("A biblioteca precisa conter de 1 a 100 perfis.") }
  try value.Profiles.forEach { try $0.validate() }; value.Version = 1; value.Selected = max(0, min(value.Profiles.count - 1, value.Selected)); return value
 }
 public func encoded() throws -> Data { try JSONEncoder().encode(self) }
 public static func importFile(_ data: Data) throws -> ProfileLibrary {
  if data.count != 960 { return try decode(data) }
  var rows = [[Int]](); let bytes = [UInt8](data)
  for row in 0..<48 { var values = [Int](); for channel in 0..<5 { let at = (row * 5 + channel) * 4; guard bytes[at + 1] == 0 && bytes[at + 2] == 0 && bytes[at + 3] == 0 else { throw LumeFailure("Arquivo LMF inválido.") }; let v = Int((bytes[at] << 4) | (bytes[at] >> 4)); guard v <= 100 else { throw LumeFailure("Intensidade inválida no LMF.") }; values.append(v) }; rows.append(values) }
  return ProfileLibrary(Profiles: [LightingProfile(Name: "Perfil importado", Channels: (1...5).map { "Canal \($0)" }, Values: rows)], Selected: 0)
 }
}
public enum ControllerProtocol {
 public static func identity(_ id: String) throws -> [UInt8] {
  let value: [UInt8]
  if id.hasPrefix("raw:"), id.count == 28 { let hex = Array(id.dropFirst(4)); var decoded = [UInt8](); for n in stride(from: 0, to: 24, by: 2) { guard let b = UInt8(String(hex[n...n+1]), radix: 16) else { throw LumeFailure("Identificação inválida da controladora.") }; decoded.append(b) }; value = decoded }
  else if id.utf8.count == 12 && id.utf8.allSatisfy({ (48...57).contains($0) || (65...70).contains($0) || (97...102).contains($0) }) { value = Array(id.utf8) }
  else { throw LumeFailure("Identificação inválida da controladora.") }
  guard !value.allSatisfy({ $0 == 0 }), !value.allSatisfy({ $0 == 255 }), !value.allSatisfy({ $0 == 48 }), !value.allSatisfy({ $0 == 70 || $0 == 102 }) else { throw LumeFailure("Identificação vazia da controladora.") }; return value
 }
 public static func frame(_ payload: [UInt8], _ id: String) throws -> [UInt8] {
  guard (1...47).contains(payload.count) else { throw LumeFailure("Comando inválido") }
  var b = [UInt8](repeating: 0, count: 64); b[0] = 0x55; b[1] = 0xaa; b.replaceSubrange(2..<2+payload.count, with: payload); b.replaceSubrange(49..<61, with: try identity(id)); b[61] = UInt8((255 + payload.reduce(0) { $0 + Int($1) }) & 255); return b
 }
 public static func discovery() -> [UInt8] { var b = [UInt8](repeating: 0, count: 64); b.replaceSubrange(0..<9, with: [0x55,0xaa,0x17,0,0,3,0xdd,0xee,0xff]); b[61] = 0xe3; return b }
 public static func smart(_ b: [UInt8]) -> Bool { b.count == 64 && b[0] == 0x88 && b[1] == 0 }
 public static func discovered(_ b: [UInt8]) -> (String, String)? {
  guard (16...64).contains(b.count), (smart(b) || (b[0] == 0x55 && b[1] == 0xaa)), b[2] == 0x17 else { return nil }
  let raw = Array(b[4..<16]); guard raw != Array(discovery()[4..<16]) else { return nil }
  let ascii = String(bytes: raw, encoding: .ascii) ?? ""; let opaque = "raw:" + raw.map { String(format: "%02X", $0) }.joined()
  guard (try? identity(opaque)) != nil else { return nil }; let id = (try? identity(ascii)) != nil ? ascii : opaque
  return (id, smart(b) ? "smart" : "pled")
 }
 public static func accepts(_ b: [UInt8], command: UInt8, status: UInt8, id: String, smartOnly: Bool = false) -> Bool {
  if smart(b) { return b[4] == status && Array(b[49..<61]) == (try? identity(id)) }
  return !smartOnly && (5...64).contains(b.count) && b[0] == 0x55 && b[1] == 0xaa && b[2] == command && b[4] == status
 }
 public static func aliases(_ names: [String]) -> [String] {
  var used = Set<String>(); return names.map { name in let cleaned = name.folding(options: .diacriticInsensitive, locale: Locale(identifier: "en_US_POSIX")).unicodeScalars.filter { $0.isASCII && CharacterSet.alphanumerics.contains($0) }.map(String.init).joined(); let base = String((cleaned.isEmpty ? "PERFIL" : cleaned).prefix(7)); var alias = base; var suffix = 2; while used.contains(alias.uppercased()) { let tail = String(suffix); alias = String(base.prefix(7-tail.count)) + tail; suffix += 1 }; used.insert(alias.uppercased()); return alias }
 }
 public static func header(slot: Int, alias: String, smart: Bool) throws -> [UInt8] {
  guard (0...15).contains(slot), (1...7).contains(alias.utf8.count), alias.utf8.allSatisfy({ (48...57).contains($0) || (65...90).contains($0) || (97...122).contains($0) }) else { throw LumeFailure("Perfil inválido") }
  if smart { return [1,1,0,UInt8(alias.utf8.count),UInt8(slot)] + alias.utf8 }
  return [1,1,0,10,UInt8(slot)] + Array(alias.utf8) + [UInt8](repeating: 32, count: 7-alias.utf8.count) + [0,48]
 }
 public static func point(_ row: Int, _ values: [Int]) throws -> [UInt8] { guard (0..<48).contains(row) else { throw LumeFailure("Horário inválido") }; return [1,2,0,8,UInt8(row),UInt8(row/2),UInt8(row%2*30)] + (try levels(values)) }
 public static func levels(_ values: [Int]) throws -> [UInt8] { guard values.count == 5, values.allSatisfy({ (0...100).contains($0) }) else { throw LumeFailure("Intensidades inválidas.") }; return values.map(UInt8.init) }
 public static func previewStart(_ alias: String) throws -> [UInt8] { _ = try header(slot: 0, alias: alias, smart: true); return [6,1,0,UInt8(alias.utf8.count)] + alias.utf8 }
 public static func preview(_ values: [Int]) throws -> [UInt8] { [6,2,0,5] + (try levels(values)) }
 public static func clock(_ now: Date = Date()) -> [UInt8] { var calendar = Calendar(identifier: .gregorian); calendar.timeZone = .current; let c = calendar.dateComponents([.year,.month,.day,.hour,.minute,.second], from: now); let y = max(0,min(65535,c.year ?? 2026)); return [2,0,0,7,UInt8(y>>8),UInt8(y&255),UInt8(c.month ?? 1),UInt8(c.day ?? 1),UInt8(c.hour ?? 0),UInt8(c.minute ?? 0),UInt8(c.second ?? 0)] }
 public static func provision(id: String, ssid: String, password: String, wpa2: Bool, aes: Bool) throws -> [[UInt8]] {
  guard (1...32).contains(ssid.utf8.count), !ssid.trimmingCharacters(in: .whitespaces).isEmpty, ssid.utf8.allSatisfy({ (32...126).contains($0) }) else { throw LumeFailure("Rede: use 1 a 32 caracteres sem acentos.") }
  guard (8...32).contains(password.utf8.count), password.utf8.allSatisfy({ (32...126).contains($0) }) else { throw LumeFailure("Senha: este aparelho aceita 8 a 32 caracteres sem acentos.") }
  return try [ssid,password].enumerated().map { index, text in var b = [UInt8](repeating: 0, count: 64); b[0] = 0x55; b[1] = 0xaa; b[2] = 0x18; b[5] = UInt8(text.utf8.count); b[6] = UInt8(index+1); b.replaceSubrange(7..<7+text.utf8.count, with: text.utf8); if index == 0 { b[39] = wpa2 ? 2 : 1; b[40] = aes ? 1 : 2 }; b.replaceSubrange(49..<61, with: try identity(id)); b[61] = UInt8(b[0..<49].reduce(0) { $0 + Int($1) } & 255); return b }
 }
}
