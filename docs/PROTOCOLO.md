# Arquitetura e protocolo

[← Início](../README.md) · [Estado da validação](COMPATIBILIDADE.md)

## Smart WiFi Timer: correção confirmada pela análise do APK

Desde Android 1.0.3, o Lume reconhece respostas `88 00` de 64 bytes e usa o formato Smart Timer automaticamente. Pedidos continuam `55 AA`; descoberta fornece identidade em 4–15, confirmações em 49–60. Cabeçalho de perfil usa o tamanho real do nome; a transferência encerra cada perfil com `03 08` e o conjunto com `04 0A`. Veja a [análise completa do APK que funcionava](ANALISE-SMART-WIFI-TIMER.md), incluindo offsets, checksums, SSID/senha, ACKs e sequência de envio.

O cabeçalho `88 00` foi observado no diagnóstico físico. Até a 1.0.2 o Lume exigia incorretamente `55 AA` também na resposta. O formato desktop abaixo continua separado como caminho legado; o Windows 0.5.0 ainda não recebeu a adaptação Smart Timer.

## Implementação

Windows usa C#/WPF/.NET 8, HID nativo para USB e sockets para IPv4/UDP. `Transfer.cs` prepara uma cópia validada da biblioteca e separa a execução do transporte. `Wifi.cs` faz descoberta, credenciais e sessões. `LedSimulation.cs` desenha o recife com vetores WPF.

Android usa uma Activity Java, persistência com `AtomicFile`, seletor de documentos do sistema e uma WebView com HTML/CSS/JS/SVG embarcados. A ponte JavaScript só é exposta à interface local; navegação e carregamento externo são bloqueados. `Protocol.java` codifica os quadros; `PacketIO.java` troca os datagramas. Os sockets são vinculados explicitamente à rede Wi-Fi com `Network.bindSocket`, inclusive numa rede sem internet, e a descoberta mantém `MulticastLock` durante a busca. Operações rodam fora da thread de interface.

Os dois aplicativos salvam JSON com `Version`, `Profiles` e `Selected`. Cada perfil contém `Name`, cinco `Channels` e 48 linhas `Values` com cinco inteiros. O salvamento é atômico e os arquivos inválidos são preservados. A edição e a simulação não enviam comandos físicos.

## Caminho legado de Wi-Fi baseado no Pled V2.2 desktop

Esta descrição deriva da análise do executável fornecido e dos testes simulados, não de uma especificação oficial do fabricante. **Não comprova compatibilidade física.**

- IPv4/UDP, porta local e remota **5000**.
- Um socket permanece aberto entre envio e recepção. O Android 1.0.4 limita a recuperação a três tentativas totais do mesmo ponto Smart Timer indexado em timeout sem resposta válida; outros comandos não são repetidos automaticamente.
- Quadro de 64 bytes: `55 AA`, payload a partir de 2, identidade ASCII de 12 caracteres nos bytes 49–60, checksum no byte 61.
- Comandos comuns: checksum = `0xFF + soma(payload)`, módulo 256, sem incluir a identidade.
- Descoberta: prefixo `55 AA 17 00 00 03 DD EE FF`, byte 61 `E3`. A resposta fornece identidade nos bytes 4–15 e o IP vem do datagrama.
- Resposta de comando: cabeçalho `55 AA`, comando ecoado no byte 2 e estado de confirmação no byte 4. A implementação filtra origem, porta, tamanho e confirmação. Prazo de comando: 2,5 s.
- No Android 1.0.1, a descoberta aceita qualquer porta de origem para uma resposta válida da mesma sub-rede, conforme o receptor do Pled original. `Discovery.java` monta os destinos (gateway, broadcast da sub-rede e limitado) e rejeita eco próprio, endereço de rede/broadcast e origens externas. Os filtros das respostas a comandos de gravação não foram alterados.
- No Android 1.0.2, o campo de identificação é preservado como 12 bytes opacos, inclusive valores binários e texto não hexadecimal. A representação interna `raw:` seguida de 24 dígitos hexadecimais serializa esses bytes no JSON; ela é decodificada para os 12 bytes originais nos quadros de saída, sem enviar o texto dessa representação. IDs ASCII hexadecimais antigos continuam compatíveis. O Windows 0.5.0 ainda pressupõe ASCII hexadecimal para esse campo.

| Operação | Payload / comando | Confirmação |
|---|---|---|
| Cabeçalho de perfil | `01 01 00 0A`, posição base zero, nome ASCII de 7 bytes preenchido com espaços, `00 30` | `80` |
| Ponto | `01 02 00 08`, índice 0–47, hora, minuto, cinco intensidades | `81` |
| Limpar perfis | `05 FE` | `82` |
| Retomar | `03 08` | `83` |
| Relógio | `02 00 00 07`, ano alto/baixo, mês, dia, hora, minuto, segundo | `85` |

Rede e senha usam quadros especiais `18`, índice 1/2, texto a partir de 7, autenticação em 39 e criptografia em 40 para a etapa SSID. Seu checksum soma os bytes 0–48, módulo 256. SSID confirma com `70` ou `74`; senha com `72`. Os buffers dessas mensagens são apagados após o uso. A senha não é gravada em arquivo.

Uma substituição total valida todos os perfis, cria backup local, limpa uma vez e envia cabeçalhos/pontos sequencialmente. Se houver erro, para na primeira falha e relata quantos perfis foram concluídos. Não há leitura de retorno, restauração automática ou ativação de perfil após a gravação.

## USB

Há dois protocolos separados, selecionados pela identificação HID: Pled V2.2 `0483:5750` e TC420 clássica `0888:4000`. A TC420 clássica usa framing e comandos distintos; veja `Usb.cs`, `Transfer.cs` e a [referência](REFERENCIAS.md). Android não contém transporte USB.

## Formatos legados

LMF suportado: 960 bytes = 48 × 5 × quatro bytes little-endian. O byte baixo guarda a intensidade com os dois nibbles trocados; os três bytes altos devem ser zero. LCM contém caminhos separados por NUL com bytes também transformados por troca de nibbles. A importação desse projeto só existe no Windows porque depende dos arquivos locais referenciados.
