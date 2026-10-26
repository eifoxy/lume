# Compatibilidade e validação

**Português (Brasil)** · [English](en/COMPATIBILITY.md)

[← Início](../README.pt-BR.md)

## Plataformas

| Plataforma | Requisito | Situação |
|---|---|---|
| Windows completo | Windows 10/11 x64; runtime incluído | Compilado e testado localmente. |
| Windows leve | Windows com .NET Desktop Runtime 8 | Compilado e testado localmente. |
| Android | Android 8+ (mínimo API 26; alvo API 36) | APK compilado e assinatura verificada; usuário usa Samsung SM-S948B/API 37 e confirmou descoberta, com retorno positivo após 1.0.5. Busca automática 1.0.6 pendente no aparelho. |

## Controladoras e transportes

| Equipamento / protocolo | Windows | Android | Evidência e limite |
|---|---|---|---|
| TC420 clássica · HID `0888:4000` | USB implementado | Não suportado | Protocolo de referência e testes de transporte em memória; comandos físicos pendentes. |
| Variante Pled V2.2 · HID `0483:5750` | USB implementado | Não suportado | HID detectado no PC de desenvolvimento; protocolo analisado no executável fornecido; gravação física pendente. |
| Módulo Wi-Fi com protocolo Pled V2.2 desktop | Caminho experimental implementado | Caminho legado mantido | Simulador; compatibilidade física não confirmada. |
| Smart WiFi Timer com respostas `88 00` | Windows 0.5.0 sem esta adaptação | Suporte desde Android 1.0.3 | Descoberta confirmada pelo usuário; retorno positivo após correções de envio na 1.0.5. Sem leitura de memória para conferência. |
| TC420 sem hardware Wi-Fi | USB, se compatível | Não suportado | Software não adiciona rádio ao aparelho. |
| Outros módulos/firmwares | Sem garantia | Sem garantia | Sem identificação ou testes. Nome comercial sozinho não prova compatibilidade. |

## Funções de hardware

| Função | Implementação | Verificação física |
|---|---|---|
| Descoberta USB Windows | Enumeração HID | Controladora detectada. |
| Descoberta Wi-Fi | Broadcast UDP / consulta por IP | Android confirmado pelo usuário; Windows ainda sem confirmação. Busca automática 1.0.6 testada com eventos simulados, reteste físico pendente. |
| Configurar SSID e senha | Dois pacotes com confirmações distintas | Pendente. |
| Enviar um perfil | Cabeçalho + 48 pontos, com confirmação por comando | Pendente. |
| Apagar e enviar todos | Limpeza única + perfis sequenciais | Pendente. |
| Sincronizar relógio | Data/hora local do sistema | Pendente. |
| Retomar programa | Saída do modo manual | Pendente. |
| Aplicar intensidade instantânea | Somente USB Windows | Pendente. Não implementado por Wi-Fi por falta de confirmação confiável. |
| Ler a memória / backup do dispositivo | Não implementado | Não disponível. O backup é da biblioteca local. |

## Testes locais

- **Idiomas, Android 1.1.0 e Windows 0.6.0:** 496 mensagens em PT-BR, inglês e espanhol. Os testes de catálogo verificam cada entrada em cada idioma; os de interface conferem troca automática/manual, persistência, idiomas de sistema não suportados e preservação da biblioteca e dos bytes dos perfis. Android também foi revisado em navegador; Windows foi renderizado fora da tela nos três idiomas. As novas versões ainda precisam de reteste no telefone/controladora. Windows 0.6.0 herda os transportes da 0.5.0 e continua sem a adaptação Smart Timer do Android.
- **Windows:** 77 verificações funcionais de modelo, arquivos, pacotes, transferências, erros e UDP; 24 verificações de interface com renderização fora da tela.
- **Android 1.0.6:** 55 verificações Java gerais, 34 Smart Timer, 16 JavaScript do editor e 29 de busca automática. UDP em loopback cobre relógio, credenciais fictícias, perfil completo e perdas de confirmação no sexto ponto e no primeiro cabeçalho após limpeza. Eventos e temporizadores simulados cobrem abertura, retomada, mudança de IP, perda/retorno do aparelho, várias controladoras, escolha persistida e exclusão durante operações/diálogos.
- **APK:** compilação javac/D8/aapt2, alinhamento e assinaturas v2/v3 conferidos.
- **Interface Android:** revisão no navegador em largura de celular, incluindo persistência após recarregar, ciclo, curvas, recife e conexão.

Esses resultados não equivalem a instalação no Android, teste por um rádio Wi-Fi real, leitura de retorno da memória ou validação do firmware. Os testes de loopback usam dados fictícios e não modificam a controladora.

Após a publicação inicial, houve relato de teste Android em que o aparelho apitou durante a busca, mas não apareceu na lista. Isso não confirma descoberta ou gravação física. A versão 1.0.1 amplia os destinos de consulta, remove um filtro de porta incompatível com o receptor original e acrescenta diagnóstico; o resultado no telefone ainda precisa ser confirmado.

No reteste da 1.0.1, o diagnóstico confirmou recepção de datagramas de 64 bytes com comando decimal 23, mas rejeição pelo parser. Isso afasta ausência de recepção como explicação desse teste, sem identificar qual outro campo diferia. A 1.0.2 remove a exigência indevida de identidade ASCII hexadecimal e acrescenta motivos específicos de rejeição; ainda precisa de reteste físico. Nenhuma gravação ou configuração foi confirmada por esse diagnóstico.

O reteste da 1.0.2 mostrou precisamente `88 00` no cabeçalho. A análise do Smart WiFi Timer 3.0 confirmou esse cabeçalho de resposta e diferenças no envio de perfis. A 1.0.3 corrige esses pontos. A restrição de cabeçalho foi a causa confirmada do descarte; a hipótese de identificação binária não foi comprovada no equipamento. Descoberta, ingresso no roteador e gravação com a 1.0.3 ainda precisam do reteste físico. [Análise](ANALISE-SMART-WIFI-TIMER.md).

No reteste da 1.0.3, o usuário confirmou a descoberta e relatou falha ao gravar o sexto horário, após cinco pontos confirmados. Isso confirma comunicação parcial de envio, mas não um perfil completo, a finalização ou a leitura de retorno. A 1.0.4 adiciona recuperação de timeout apenas do mesmo ponto, ritmo mais conservador e diagnóstico da resposta; o resultado no aparelho ainda precisa de reteste.

O relato seguinte mostrou limpeza confirmada e silêncio ao iniciar o primeiro perfil. A 1.0.5 acrescenta espera após limpeza e até três tentativas do cabeçalho antes dos pontos, mantendo limpeza única. O teste UDP recupera essa sequência com perda da primeira resposta. O usuário depois relatou bom funcionamento da atualização. Esse retorno não comprova a causa do silêncio nem substitui leitura de retorno da memória, que não está implementada. A 1.0.6 mantém esse fluxo de envio e acrescenta descoberta automática; a execução da função nova no telefone ainda precisa de reteste.

## Limites

Cinco canais; 48 pontos com intervalo de 30 minutos; intensidades inteiras 0-100; até 100 perfis locais e 16 posições de envio. Nomes no aparelho têm até sete caracteres ASCII. Rede IPv4 local, UDP 5000, sem conexão remota pela internet. SSID de 1-32 e senha de 8-32 caracteres ASCII; WPA/WPA2, AES/TKIP. WPA3 não implementado.

Não há leitura completa de memória, rollback do aparelho, atualização de firmware, descoberta Bluetooth, comunicação USB no Android ou telemetria na nuvem. Uma falha depois de um comando de limpeza pode deixar a memória apagada ou parcial. O app para e orienta reconectar e reenviar, limitando a recuperação automática ao mesmo ponto Smart Timer indexado (até três tentativas totais em timeout sem resposta válida).

## Prévia Wi-Fi, Android 1.2.0

Implementada apenas para Smart Timer com comandos `06 01`/`06 02` encontrados no APK original. Intensidades enviadas sem ACK individual; retomada `03 08` exige ACK Smart `83` e identidade correta. Testes com fixtures, caixa de valor mais recente, cancelamento, ausência de atividade, falhas de retomada e UDP real em loopback passaram. Interface testada nos três idiomas com ponte nativa simulada. O efeito na luminária ainda depende de teste físico. O Windows não recebe esta função. [Uso e limitações](PREVIA-AO-VIVO.md).
