# Instalar no Android

[← Início](../README.md) · [Configuração Wi-Fi](WIFI.md) · [Uso e perfis](USO.md)

## Requisitos

- Android **8.0 ou mais recente** (API 26).
- Wi-Fi no telefone e uma controladora com módulo compatível com o protocolo do Pled V2.2.
- Android System WebView atualizado para renderizar a interface local.

Não é necessário cabo, PC conectado ao telefone, conta ou serviço na nuvem. USB não é implementado no Android. A TC420 comum sem módulo Wi-Fi não pode se conectar por este app.

## Instalar o APK

1. No telefone, abra a [versão Android publicada](https://github.com/eifoxy/lume/releases/tag/android-v1.0.2).
2. Baixe **`Lume-Android-1.0.2.apk`** na área **Assets**. Se já usa uma versão anterior, instale por cima, sem desinstalar, para manter os perfis.
3. Abra o download pelo gerenciador de arquivos ou pela notificação de conclusão.
4. Se o Android pedir, permita que **esse navegador ou gerenciador de arquivos** instale aplicativos. O nome da tela varia entre fabricantes.
5. Toque em **Instalar** e abra **Lume**.

O APK é distribuído diretamente pelo GitHub e não está na Play Store. É assinado com a chave local do projeto. Não é necessário ativar depuração USB.

## Primeira conexão

Abra **Conexão** no menu inferior e siga as quatro etapas: entrar no Wi-Fi do aparelho, buscar a controladora, configurar o roteador e retornar à rede de casa. O botão **Abrir redes Wi-Fi** abre o painel do Android; você escolhe a rede no sistema.

O aplicativo não precisa que a rede do aparelho tenha internet. Se o Android avisar “sem internet”, escolha permanecer conectado. A comunicação UDP é vinculada à interface Wi-Fi, mesmo se a internet móvel estiver ativa.

[Tutorial completo de configuração e descoberta →](WIFI.md)

## Editar e enviar

1. Em **Curvas**, selecione um perfil e um canal.
2. Toque na curva para escolher um horário. Use as setas para avançar em passos de 30 minutos.
3. Edite as intensidades pelos controles ou pela tabela. **Criar ciclo** preenche o dia com rampas suaves.
4. Em **Recife**, deslize o relógio ou reproduza um dia. A prévia não altera os LEDs físicos.
5. Com o aparelho encontrado e selecionado, toque **Enviar**. Revise perfil, posição e nome curto antes de confirmar.
6. Em **Conexão**, use **Sincronizar horário** para enviar a hora local do telefone.

Para enviar todos os perfis, escolha a substituição da memória e marque a confirmação de apagar os perfis antigos. Mantenha o app aberto e o Wi-Fi conectado durante o processo. [Detalhes do envio →](USO.md#enviar-perfis)

## Importar, exportar e atualizar

Em **Perfis → Importar**, escolha uma biblioteca JSON do Lume ou um `.lmf` do Pled V2.2. A importação adiciona os perfis aos que já existem. Não há importação de projetos `.lcm` no Android.

Use **Exportar biblioteca** e escolha a pasta pelo seletor do Android. O app só acessa os arquivos selecionados; não pede acesso geral ao armazenamento.

As alterações e backups internos ficam nos dados do app. **Desinstalar apaga esses dados**. Exporte antes de desinstalar, limpar dados ou trocar de telefone. Atualizações assinadas com a mesma chave preservam a instalação. APKs de desenvolvimento/CI podem usar outra chave e não substituir a versão de lançamento diretamente.

## Permissões

São declaradas apenas: `INTERNET`, `ACCESS_NETWORK_STATE`, `ACCESS_WIFI_STATE` e `CHANGE_WIFI_MULTICAST_STATE`. Não há permissões de USB, Bluetooth, câmera, contatos ou localização. A senha do roteador não é salva na biblioteca ou no backup.

O alvo atual é SDK 36. Ao migrar para alvo SDK 37, o app precisará declarar e solicitar a permissão de rede local do Android 17; veja a [documentação oficial](https://developer.android.com/privacy-and-security/local-network-permission).

## Estado da versão

Compilação, assinatura e testes locais passaram. **A instalação e a execução em telefone, a descoberta e a gravação na controladora física ainda não foram validadas.** Isso está separado dos resultados do simulador na [matriz de compatibilidade](COMPATIBILIDADE.md).
