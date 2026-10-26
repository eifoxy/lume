# Lume para iPhone e iPad

**Português (Brasil)** · [English](en/INSTALL-IOS.md) · [Español](es/INSTALACION-IOS.md)

[← Lume](../README.md)

## Situação desta versão

**0.1.1, prévia de desenvolvimento para iOS/iPadOS 15.4 ou posterior.** O projeto inclui o editor e a comunicação Wi-Fi. A compilação e os testes automatizados são executados num Mac do GitHub Actions. Ainda não houve teste num iPhone real com a controladora. Não há lançamento na App Store ou no TestFlight.

O arquivo `UNSIGNED.ipa` é um artefato de desenvolvimento **sem assinatura Apple**. Ele não instala ao abrir no iPhone. O ZIP Simulator é para o simulador do Xcode em um Mac, não para um telefone. [Compilações e artefatos](https://github.com/eifoxy/lume/actions/workflows/ios.yml).

### iPhone 7 e outros modelos antigos

A versão 0.1.1 reduz o mínimo para **iOS 15.4**. Isso inclui iPhone 7/7 Plus e modelos como 6s e SE de primeira geração, desde que estejam atualizados para uma versão compatível do iOS 15. iOS 15.0 a 15.3 e aparelhos limitados ao iOS 12 não são atendidos. O mínimo 15.4 mantém o suporte nativo aos diálogos do editor; a tradução foi adaptada para não depender de recursos de JavaScript do iOS 16.4. [Diálogos no Safari 15.4](https://developer.apple.com/documentation/safari-release-notes/safari-15_4-release-notes).

As funções Wi-Fi e os três idiomas permanecem implementados. A compilação verifica o mínimo 15.4, e testes comparam as traduções sem os recursos mais recentes. O simulador disponível no GitHub usa um sistema mais novo; a tela pequena é verificada separadamente. Isso **não equivale a um teste num iPhone 7 ou num runtime iOS 15**. Desempenho do recife e comunicação no aparelho antigo ainda precisam desse teste. A exigência de assinatura Apple continua a mesma.

Se você não tem Mac nem acesso a assinatura Apple, pode usar as versões Windows/Android enquanto o iOS permanece nesta etapa. Baixar o IPA sozinho não resolve essa limitação. Nenhuma compra, conta ou inscrição no programa Apple é feita automaticamente pelo projeto.

## O que foi portado

- Curvas de cinco canais, tabela de 48 horários, ciclos suaves e biblioteca de perfis.
- Recife animado, simulação do dia e prévia vinculada à luminária Smart Timer.
- Português, inglês e espanhol, automáticos pelo idioma do dispositivo ou escolhidos no topo.
- Importação JSON/LMF, exportação JSON pelo app Arquivos e salvamento local com cópias antes de alterações/envios.
- Descoberta automática ao abrir/retornar, seleção anterior pelo identificador, busca por IP e novas tentativas em primeiro plano.
- Configuração da rede doméstica, envio individual ou substituição de até 16 perfis, relógio e retomada do programa.

Estas são funções implementadas, não uma declaração de compatibilidade física já verificada no iOS. TC420 comum sem rádio Wi-Fi continua sem suporte sem fio. O protocolo Smart Timer foi portado da adaptação do Android; o caminho legado Pled foi mantido, com verificação física pendente em ambos no iOS. USB não está disponível.

## Instalar para desenvolvimento com um Mac

1. Obtenha o código do repositório e instale uma versão do Xcode que suporte o seu iPhone.
2. Abra `ios/Lume.xcodeproj`, selecione o esquema **Lume** e o seu dispositivo.
3. Em **Signing & Capabilities**, escolha sua equipe e um identificador de pacote único. O projeto não inclui certificados ou equipe de outra pessoa.
4. Entre com sua Apple Account no Xcode. Uma **Personal Team gratuita** permite testes pessoais, com limites e assinatura que expira após sete dias; será preciso recompilar/reinstalar. Não exige inscrição paga para esse teste pessoal. [Regras da Apple](https://developer.apple.com/help/account/basics/about-your-developer-account).
5. Ative o modo de desenvolvedor no dispositivo quando solicitado pelo Xcode e execute o app. Exporte a biblioteca antes de remover o aplicativo ou trocar o identificador.

Para distribuição pelo TestFlight/App Store, alguém responsável pelo projeto precisa de uma equipe inscrita no programa apropriado, assinatura e configuração no App Store Connect. Essa publicação não foi realizada. [Programas Apple](https://developer.apple.com/help/account/membership/programs-overview/).

## Primeiro uso por Wi-Fi

1. No iPhone, abra **Ajustes > Wi-Fi** e entre na rede da controladora. Permaneça conectado mesmo sem internet.
2. Abra o Lume e permita **Rede Local**. Se negou, habilite em Ajustes nas permissões do aplicativo. O botão do Lume explica a conexão e abre as permissões; o iOS não permite que o app simplesmente mostre o seletor Wi-Fi do Android.
3. Aguarde a busca ou use **Conexão > Buscar aparelhos**. Na rede `192.168.4.x`, tente `192.168.4.1` em **Buscar por IP conhecido** se necessário.
4. Selecione a controladora, informe o Wi-Fi de casa e envie as credenciais. O módulo antigo normalmente exige 2,4 GHz, WPA2/AES e nome/senha ASCII; a senha aceita pelo protocolo tem de 8 a 32 caracteres. A senha não é salva pelo Lume.
5. Entre no Wi-Fi de casa pelo iPhone, volte ao Lume, encontre a controladora e sincronize o horário. Edite a programação e envie um perfil ou confirme a substituição de todos.

### Como a descoberta difere do Android

O iOS exige autorização especial para broadcast/multicast. Esta versão não depende dela: envia consultas **unicast** diretamente a endereços da rede Wi-Fi, usando a interface IPv4 do Wi-Fi e UDP 5000, sem migrar para dados móveis. Tenta o IP salvo, o endereço habitual do ponto de acesso e até 254 endereços próximos, respeitando a sub-rede atual. A busca tem prazo de até 14 segundos. Em redes maiores que `/24`, aparelhos fora desse grupo podem exigir IP conhecido, obtido no roteador. Redes com isolamento entre clientes também impedem a comunicação.

Não foi comprovado que todos os firmwares respondam a unicast. Se o aparelho só responder a broadcast, pode ser necessária uma futura versão com a autorização Apple. Um IP digitado apenas define onde consultar; não fabrica um dispositivo nem confirma sua identidade. [Rede Local e UDP no iOS, Apple](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy).

## Prévia na luz real

No **Recife**, vincule a luminária e confirme. Mover o horário ou reproduzir o dia envia os cinco canais ao Smart Timer, no máximo cinco atualizações por segundo. Isso muda a luz real, inclusive em reprodução acelerada. Cada intensidade é enviada sem confirmação individual, e a cena não mede a luz física nem prevê efeitos biológicos.

Ao parar ou sair do primeiro plano, o app tenta retomar a programação e só limpa o aviso de recuperação depois da confirmação do mesmo aparelho. Existe um prazo curto para essa tentativa em segundo plano, não execução contínua. Se a rede cair ou o processo for encerrado, a luz pode permanecer no último estado manual. Reconecte o mesmo aparelho e use **Retomar programa**. Não há restauração garantida após encerramento forçado. A prévia não apaga nem grava os perfis.

## Dados e privacidade

O editor e os recursos visuais ficam no próprio app. Não há conta Lume, telemetria, anúncios nem servidor de controle. Os comandos vão apenas à rede local. Biblioteca, preferências e aviso de recuperação ficam no armazenamento do aplicativo, sujeito às políticas de backup do sistema. O diagnóstico contém IPs locais e contagens, sem SSID ou senha; só é copiado/compartilhado por ação do usuário. Exporte JSON antes de desinstalar, pois remover o app pode remover os perfis.

## Verificar e compilar

Num Mac, a partir da raiz do repositório:

```sh
node scripts/sync-ios-assets.cjs --check
node scripts/generate-ios-project.cjs --check
node ios/Tests/bridge-test.cjs
swift test --package-path ios
xcodebuild -project ios/Lume.xcodeproj -scheme Lume -configuration Release \
  -sdk iphonesimulator -destination 'generic/platform=iOS Simulator' \
  -derivedDataPath artifacts/ios-simulator CODE_SIGNING_ALLOWED=NO build
```

O fluxo `ios.yml` também compila para dispositivo, sem assinatura, e empacota o IPA e o aplicativo do simulador com SHA-256. Testes de pacotes, validação, transações e UDP local não substituem a aprovação da permissão Rede Local nem um teste com a controladora real.
