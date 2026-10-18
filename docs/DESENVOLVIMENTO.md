# Compilar e testar

[← Início](../README.md)

## Obter o código

```powershell
git clone https://github.com/eifoxy/lume.git
cd lume
```

## Windows

Use Windows, SDK .NET 8 e PowerShell 7. Não há dependências NuGet de terceiros. O feed oficial nuget.org é usado para obter os pacotes de runtime na publicação completa.

```powershell
dotnet build windows/src/Lume.csproj -c Release
dotnet run --project windows/src/Lume.csproj -c Release
```

Criar as distribuições:

```powershell
pwsh ./scripts/build-windows.ps1
```

O script produz `artifacts/windows-portable/`, `artifacts/windows-x64/` e os ZIPs correspondentes. O pacote x64 inclui o .NET 8.0.32 verificado para esta versão; atualize a versão do runtime no script ao manter o projeto.

Testes (sem hardware):

```powershell
pwsh ./scripts/test-windows.ps1 -AppDirectory ./artifacts/windows-x64
```

Os argumentos nativos também estão disponíveis: `Lume.exe --self-test` e `Lume.exe --ui-test`. O primeiro usa arquivos temporários e sockets em loopback; o segundo renderiza a UI fora da tela. Os relatórios ficam ao lado do executável de teste. Use a pasta de artefatos, sem sua biblioteca pessoal. `--wifi-scan` faz uma busca real de descoberta na LAN; não é parte da suíte automática e não envia programação.

## Android: compilação direta no Windows

Requisitos: PowerShell 7, JDK 17, Android SDK Platform 36, Android build-tools 36.0.0. O código não usa dependências Android de terceiros. Instale os componentes pelo Android Studio/SDK Manager, aceitando as licenças do SDK no seu ambiente.

```powershell
pwsh ./android/build.ps1 `
  -Jdk 'C:\caminho\jdk-17' `
  -BuildTools 'C:\caminho\Android\Sdk\build-tools\36.0.0' `
  -Platform 'C:\caminho\Android\Sdk\platforms\android-36' `
  -SigningKey 'C:\caminho-privado\lume-local.p12' `
  -BuildDirectory './artifacts/android-build'
```

O resultado é `android/Lume-Android.apk`. A compilação executa javac → JAR → D8 → aapt2 → zipalign → apksigner. Se a chave não existir, o script gera uma chave local com alias `lume-local` e senha de desenvolvimento `android`. **Essa convenção é para builds locais; proteja o arquivo e não o publique.** Atualizações para a mesma instalação precisam da mesma chave. O APK público é assinado com a chave local preservada pelo mantenedor; não está no repositório.

### Android Studio

Abra `android/` como projeto Gradle. Requisitos: AGP 8.10.1, Gradle 8.11.1 e JDK 17, com SDK 36. O projeto inclui os arquivos Gradle, mas não um Gradle Wrapper; use uma instalação de Gradle 8.11.1 no IDE ou gere o wrapper com `gradle wrapper --gradle-version 8.11.1`. Não acrescente senhas, `local.properties` ou a chave ao Git.

### Testes Android sem dispositivo

Com Java 17 e Node.js 24:

```powershell
pwsh ./scripts/test-android.ps1 -Jdk 'C:\caminho\jdk-17'
```

`ProtocolTest.java` usa `DatagramSocket` real no loopback com respostas simuladas. O teste do editor executa validação, interpolação, histórico e persistência em um contexto JavaScript de teste; não substitui testar a WebView Android.

## CI e validação física

O workflow `checks.yml` compila os dois aplicativos, roda testes e publica artefatos de CI. Os APKs de CI são assinados com uma chave temporária de desenvolvimento a cada execução e não devem substituir o APK de lançamento na mesma instalação. Nenhum workflow guarda credenciais de roteador, acessa uma controladora real ou publica releases automaticamente.

Para contribuir com validação física, registre modelo, firmware, versão do Lume, Android/Windows e qual função confirmou no dispositivo. Não inclua senha, chave de assinatura, SSID real, IP público ou arquivos pessoais no relato. Testar envio no hardware altera a programação; faça uma cópia dos programas atuais pelo procedimento conhecido do fabricante antes de experimentar.
