# Compatibilidade e validação

[← Início](../README.md)

## Plataformas

| Plataforma | Requisito | Situação |
|---|---|---|
| Windows completo | Windows 10/11 x64; runtime incluído | Compilado e testado localmente. |
| Windows leve | Windows com .NET Desktop Runtime 8 | Compilado e testado localmente. |
| Android | Android 8+ (mínimo API 26; alvo API 36) | APK compilado e assinatura verificada; execução em telefone/emulador pendente. |

## Controladoras e transportes

| Equipamento / protocolo | Windows | Android | Evidência e limite |
|---|---|---|---|
| TC420 clássica · HID `0888:4000` | USB implementado | Não suportado | Protocolo de referência e testes de transporte em memória; comandos físicos pendentes. |
| Variante Pled V2.2 · HID `0483:5750` | USB implementado | Não suportado | HID detectado no PC de desenvolvimento; protocolo analisado no executável fornecido; gravação física pendente. |
| Módulo Wi-Fi com protocolo Pled V2.2, como variantes TC421 | Descoberta, configuração e comandos implementados | Mesmos recursos por Wi-Fi | Pacotes e confirmações verificados em simulador UDP local; validação no módulo real pendente. |
| TC420 sem hardware Wi-Fi | USB, se compatível | Não suportado | Software não adiciona rádio ao aparelho. |
| Outros módulos/firmwares | Sem garantia | Sem garantia | Sem identificação ou testes. Nome comercial sozinho não prova compatibilidade. |

## Funções de hardware

| Função | Implementação | Verificação física |
|---|---|---|
| Descoberta USB Windows | Enumeração HID | Controladora detectada. |
| Descoberta Wi-Fi | Broadcast UDP / consulta por IP | Busca no PC retornou zero; rede do aparelho ainda não configurada. |
| Configurar SSID e senha | Dois pacotes com confirmações distintas | Pendente. |
| Enviar um perfil | Cabeçalho + 48 pontos, com confirmação por comando | Pendente. |
| Apagar e enviar todos | Limpeza única + perfis sequenciais | Pendente. |
| Sincronizar relógio | Data/hora local do sistema | Pendente. |
| Retomar programa | Saída do modo manual | Pendente. |
| Aplicar intensidade instantânea | Somente USB Windows | Pendente. Não implementado por Wi-Fi por falta de confirmação confiável. |
| Ler a memória / backup do dispositivo | Não implementado | Não disponível. O backup é da biblioteca local. |

## Testes locais

- **Windows:** 77 verificações funcionais de modelo, arquivos, pacotes, transferências, erros e UDP; 24 verificações de interface com renderização fora da tela.
- **Android:** 32 verificações Java de pacotes, checksums, nomes, validação, subnet, confirmações e timeout, incluindo UDP real em loopback com um aparelho simulado; 16 verificações JavaScript do editor.
- **APK:** compilação javac/D8/aapt2, alinhamento e assinaturas v2/v3 conferidos.
- **Interface Android:** revisão no navegador em largura de celular, incluindo persistência após recarregar, ciclo, curvas, recife e conexão.

Esses resultados não equivalem a instalação no Android, teste por um rádio Wi-Fi real, leitura de retorno da memória ou validação do firmware. Os testes de loopback usam dados fictícios e não modificam a controladora.

## Limites

Cinco canais; 48 pontos com intervalo de 30 minutos; intensidades inteiras 0–100; até 100 perfis locais e 16 posições de envio. Nomes no aparelho têm até sete caracteres ASCII. Rede IPv4 local, UDP 5000, sem conexão remota pela internet. SSID de 1–32 e senha de 8–32 caracteres ASCII; WPA/WPA2, AES/TKIP. WPA3 não implementado.

Não há leitura completa de memória, rollback do aparelho, atualização de firmware, descoberta Bluetooth, comunicação USB no Android ou telemetria na nuvem. Uma falha depois de um comando de limpeza pode deixar a memória apagada ou parcial. O app para e orienta reconectar e reenviar, sem repetir automaticamente a gravação.
