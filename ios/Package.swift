// swift-tools-version: 5.9
import PackageDescription
let package = Package(name: "LumeCore", platforms: [.macOS(.v13), .iOS(.v17)], products: [.library(name: "LumeCore", targets: ["LumeCore"])], targets: [.target(name: "LumeCore", path: "Core"), .testTarget(name: "LumeCoreTests", dependencies: ["LumeCore"], path: "Tests", exclude: ["bridge-test.cjs"])])
