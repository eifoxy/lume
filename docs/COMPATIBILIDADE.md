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
| Módulo Wi-Fi com protocolo Pled V2.2 desktop | Caminho experimental implementado | Caminho legado mantido | Simulador; compatibilidade física não confirmada. |
| Smart WiFi Timer com respostas `88 00` | Windows 0.5.0 sem esta adaptação | Suporte no Android 1.0.3 | Cabeçalho observado fisicamente e sequência analisada no APK funcional; reteste Lume 1.0.3 no aparelho pendente. |
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
- **Android 1.0.3:** 55 verificações Java de protocolo/descoberta, 22 de Smart Timer (incluindo integração UDP de relógio, SSID/senha fictícios, 48 pontos e ambos os encerramentos) e 16 JavaScript do editor. Na 1.0.2 eram 55 verificações Java, na 1.0.1 eram 44 e na 1.0.0 eram 32.
- **APK:** compilação javac/D8/aapt2, alinhamento e assinaturas v2/v3 conferidos.
- **Interface Android:** revisão no navegador em largura de celular, incluindo persistência após recarregar, ciclo, curvas, recife e conexão.

Esses resultados não equivalem a instalação no Android, teste por um rádio Wi-Fi real, leitura de retorno da memória ou validação do firmware. Os testes de loopback usam dados fictícios e não modificam a controladora.

Após a publicação inicial, houve relato de teste Android em que o aparelho apitou durante a busca, mas não apareceu na lista. Isso não confirma descoberta ou gravação física. A versão 1.0.1 amplia os destinos de consulta, remove um filtro de porta incompatível com o receptor original e acrescenta diagnóstico; o resultado no telefone ainda precisa ser confirmado.

No reteste da 1.0.1, o diagnóstico confirmou recepção de datagramas de 64 bytes com comando decimal 23, mas rejeição pelo parser. Isso afasta ausência de recepção como explicação desse teste, sem identificar qual outro campo diferia. A 1.0.2 remove a exigência indevida de identidade ASCII hexadecimal e acrescenta motivos específicos de rejeição; ainda precisa de reteste físico. Nenhuma gravação ou configuração foi confirmada por esse diagnóstico.

O reteste da 1.0.2 mostrou precisamente `88 00` no cabeçalho. A análise do Smart WiFi Timer 3.0 confirmou esse cabeçalho de resposta e diferenças no envio de perfis. A 1.0.3 corrige esses pontos. A restrição de cabeçalho foi a causa confirmada do descarte; a hipótese de identificação binária não foi comprovada no equipamento. Descoberta, ingresso no roteador e gravação com a 1.0.3 ainda precisam do reteste físico. [Análise](ANALISE-SMART-WIFI-TIMER.md).

## Limites

Cinco canais; 48 pontos com intervalo de 30 minutos; intensidades inteiras 0–100; até 100 perfis locais e 16 posições de envio. Nomes no aparelho têm até sete caracteres ASCII. Rede IPv4 local, UDP 5000, sem conexão remota pela internet. SSID de 1–32 e senha de 8–32 caracteres ASCII; WPA/WPA2, AES/TKIP. WPA3 não implementado.

Não há leitura completa de memória, rollback do aparelho, atualização de firmware, descoberta Bluetooth, comunicação USB no Android ou telemetria na nuvem. Uma falha depois de um comando de limpeza pode deixar a memória apagada ou parcial. O app para e orienta reconectar e reenviar, sem repetir automaticamente a gravação.
