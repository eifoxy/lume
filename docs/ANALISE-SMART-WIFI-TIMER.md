# Análise do Smart WiFi Timer 3.0

**Português (Brasil)** · [English](en/SMART-WIFI-TIMER-ANALYSIS.md)

Análise estática realizada em 19/10/2026 a partir do APK que o usuário informou funcionar com sua controladora. O aplicativo foi decompilado com JADX; **não foi instalado nem executado** durante esta análise. O APK e seu código decompilado permanecem fora do repositório. A implementação do Lume usa os campos e a sequência do protocolo descritos abaixo.

## Material analisado

| Campo | Identificação |
|---|---|
| Arquivo fornecido | `Smart+WifiTimer_3.0_APKPure.apk` |
| Pacote | `com.leyview.timercontroller` |
| Nome no manifesto | `SmartTimeControl` |
| Versão | 3.0, versionCode 3 |
| SDK mínimo / alvo | 15 / 26 |
| SHA-256 | `c2462bcd50cce0abb7f76d29cc616642a91a881aa64a0026ffe6bf21228dce7a` |

As funções relevantes ficam no namespace `com.leyview.item1`: `operation.Communication`, `operation.MyProtocol`, `operation.Common` e `timercontroller.DeviceDetailActivity`. Seus nomes são referências de localização da análise, não código redistribuído.

## Causa confirmada da rejeição

Há **assimetria entre pedido e resposta**:

| Direção | Cabeçalho | Tamanho |
|---|---|---|
| App → controladora | `55 AA` | 64 bytes |
| Controladora → app | `88 00` | 64 bytes |

O receptor de confirmações do APK exige `88 00`. A mesma sequência apareceu no diagnóstico físico enviado pelo usuário: 64 bytes, comando de descoberta 23 e cabeçalho `88 00`. O Lume até a versão Android 1.0.2 exigia `55 AA` também na resposta, portanto descartava esses datagramas antes de ler sua identidade.

As alterações anteriores de porta e identificação não corrigiam essa condição. A hipótese de identidade não hexadecimal não foi confirmada no equipamento; o suporte a bytes opacos foi mantido para evitar conversões com perda, mas a evidência direta deste caso é o cabeçalho de resposta.

## Como a descoberta funciona

1. `Communication` abre um `DatagramSocket` na porta local UDP **5000** e mantém uma thread recebendo datagramas.
2. `MyProtocol.SearchDevice` constrói 64 bytes zerados e define os primeiros campos como `55 AA 17 00 00 03 DD EE FF`.
3. O byte **61** recebe a soma dos bytes **0–48**, módulo 256. No pedido de descoberta isso resulta em `E3`.
4. Envia o pedido para **255.255.255.255:5000** e repete a consulta após 300 ms. São consultas de descoberta, não envio de horários.
5. Durante a janela de busca, a resposta com comando **0x17** (23 decimal) é tratada como um aparelho.
6. `Common.GetMacFromSearch` extrai a identificação dos bytes **4–15**, doze bytes.
7. O endereço IP é obtido da origem do datagrama. A lista elimina repetições pela identificação.

O teste preliminar `Communication.CheckReceive` do APK é permissivo; sua forma decompilada usa uma condição alternativa sobre os dois primeiros bytes. O caminho `MyProtocol.ReceiveData` é mais claro e exige ambos os bytes `88 00`. O Lume não reproduz a condição permissiva: aceita explicitamente o cabeçalho confirmado e continua rejeitando datagramas de outra rede, ecos da consulta, identidades vazias e tamanhos incompatíveis.

O Android 1.0.3 mantém os destinos adicionais de diagnóstico da 1.0.1 (gateway e broadcast da sub-rede), além do broadcast limitado usado pelo APK. O recebimento continua vinculado à rede Wi-Fi do telefone, inclusive sem internet.

## Confirmações de comandos

O APK verifica, em `MyProtocol.ReceiveData`:

- Quadro de **64 bytes** com cabeçalho **88 00**.
- Código de resultado no byte **4**.
- Identificação do aparelho nos bytes **49–60**, igual à do destino.

A identidade muda de posição entre descoberta e confirmação. O Lume 1.0.3 passou a conferir essa identidade nas respostas Smart Timer; não basta receber qualquer quadro com o código esperado. A origem IP/porta também é filtrada pelo transporte do Lume.

O aplicativo antigo aguarda cerca de um segundo e pode repetir comandos até três vezes. O Lume mantém seu prazo de 2,5 segundos. Desde Android 1.0.4, só o mesmo ponto indexado pode ter até três tentativas totais em timeout sem resposta válida, com bytes idênticos. Cabeçalhos, limpeza, credenciais, relógio e encerramentos não são repetidos automaticamente.

## Formato de saída e checksum

Todos os comandos enviados têm 64 bytes. Índices nesta descrição começam em zero:

| Posição | Função |
|---|---|
| 0–1 | `55 AA` |
| 2 | Comando |
| 3 | Subcomando, quando usado |
| 4 | Reservado, normalmente zero |
| 5 | Tamanho do conteúdo, quando usado |
| 6 em diante | Dados do comando |
| 49–60 | Identidade do aparelho |
| 61 | Soma dos bytes 0–48, módulo 256 |
| 62–63 | Zero no APK analisado |

O cálculo anterior do Lume (`0xFF + soma(payload)`) é equivalente para seus quadros comuns, pois `0x55 + 0xAA = 0xFF` e o restante da área é zerado. A identidade não entra no checksum.

## Conectar ao roteador

`MyProtocol.SetWifi` usa comando **0x18** e dois pacotes para uma rede protegida:

| Campo | Rede / SSID | Senha |
|---|---|---|
| Byte 5 | Tamanho do SSID | Tamanho da senha |
| Byte 6 | `01` | `02` |
| Bytes 7 em diante | SSID | Senha |
| Byte 39 | `01` WPA ou `02` WPA2 | Zero |
| Byte 40 | `01` AES ou `02` TKIP | Zero |
| ACK esperado, byte 4 | `70` | `72` |

A tela do APK limita SSID/senha a 32 bytes; ela pode truncar texto. O Lume conserva seus limites explícitos e recusa conteúdo incompatível, sem truncar. A interface de rede aberta do APK usa uma terceira variante: byte 6 `03`, SSID e ACK `74`. **Rede aberta não foi adicionada à interface do Lume nesta correção.**

O código antigo salva informações de Wi-Fi em seu banco local. O Lume não copia esse comportamento: não persiste a senha e limpa os buffers dos pacotes após a tentativa. Se a confirmação final não chegar após a troca de rede, orienta procurar a controladora no roteador antes de repetir a configuração.

## Relógio e retomada

- Relógio: comando **02**, tamanho **07**, sete bytes desde a posição 6: ano alto, ano baixo, mês, dia, hora, minuto, segundo. ACK **85**.
- Encerrar processamento de um perfil / sair de prévia: **03 08**, ACK **83**.
- Encerrar processamento do conjunto: **04 0A**, ACK **91**.
- Limpar perfis: **05 FE**, ACK **82**.

Os campos de relógio já eram compatíveis no Lume; a confirmação `88 00` estava sendo recusada. Os nomes internos de função do APK não comprovam todos os efeitos do firmware: a interpretação física de ativação/retomada continua dependendo do teste no aparelho.

## Enviar horários

O APK não usa o cabeçalho fixo do Pled desktop que estava sendo usado pelo Lume. Seu `StartDownload` envia:

- Byte 2 `01`, byte 3 `01`.
- Byte 5: **quantidade real de bytes do nome**.
- Byte 6: posição do perfil, base zero.
- Byte 7 em diante: nome, sem acrescentar um contador `48` no final.
- ACK **80**.

`DownloadProcess` envia cada um dos 48 pontos:

- Byte 2 `01`, byte 3 `02`, byte 5 `08`.
- Byte 6: índice do ponto, 0–47.
- Bytes 7–8: hora e minuto (00 ou 30).
- Bytes 9–13: cinco intensidades.
- ACK **81**.

Depois de cada ponto, a tela de transferência aguarda **50 ms**. Depois dos 48 pontos de cada perfil, chama **03 08 / ACK 83**. Ao terminar o conjunto, chama **04 0A / ACK 91**.

O Android 1.0.3 do Lume detecta o formato Smart Timer pela resposta de descoberta `88 00` e usa essa sequência, incluindo os dois encerramentos e o intervalo entre pontos. Mantém nomes de sete caracteres, um subconjunto do limite de 12 bytes usado na tela do aplicativo antigo, por compatibilidade com a revisão de nomes do Lume. Para a substituição total explicitamente confirmada, faz uma limpeza única antes do conjunto. Para envio individual, não limpa a memória inteira.

## O que foi alterado e como foi verificado

- Parser de descoberta: resposta `88 00`, 64 bytes, identificação em 4–15.
- Parser de ACK Smart: cabeçalho `88 00`, 64 bytes, código em 4, identificação em 49–60.
- Seleção automática do formato de envio Smart Timer, mantendo o fluxo anterior separado.
- Cabeçalho de perfil com tamanho real do nome, pausa de 50 ms por ponto e encerramentos por perfil/conjunto.
- Validação e cópia do plano inteiro antes do primeiro comando, backup local, interrupção na primeira falha e nenhuma repetição automática de mutações.

Testes locais: **55 verificações de protocolo/descoberta, 22 de Smart Timer e 16 do editor**. O teste de integração usa sockets UDP reais em loopback, com simulador que recebe relógio, SSID, senha fictícia, cabeçalho e 48 pontos, responde `88 00` com identidade e exige os dois encerramentos. Há casos negativos para identidade errada, código/tamanho/cabeçalho inválido, cancelamento, falha no meio da transferência e ausência de confirmação final.

Esses testes não foram executados na controladora física. A evidência física disponível confirma recepção de `88 00` na descoberta e o usuário informou que o APK original funcionava; configuração, relógio e gravação com Lume 1.0.3 ainda precisam de confirmação. O Windows 0.5.0 não recebeu esta adaptação do formato Smart Timer.

## Atualização 1.0.4
A descoberta foi confirmada fisicamente, mas um envio parou no sexto ponto por timeout. A versão 1.0.4 usa 150 ms entre pontos e recuperação limitada do mesmo ponto, com diagnóstico persistente. O teste de loopback que perde o ACK do sexto ponto recupera e encerra o perfil. Essa simulação não confirma a causa ou a recuperação no aparelho real.
