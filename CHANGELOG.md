# Histórico

## Android 1.0.2 · 2026-10-18

- Trata a identidade Wi-Fi como 12 bytes opacos, conforme a cópia feita pelo Pled original, sem exigir texto hexadecimal ASCII.
- Preserva esses mesmos bytes ao montar comandos e os dois pacotes de configuração; mantém compatibilidade com identidades ASCII já suportadas.
- Rejeita explicitamente o identificador da própria consulta de busca, inclusive ecos com padding diferente, e identidades vazias.
- Diagnóstico distingue cabeçalho, comando, tamanho, eco e identidade vazia; mostra somente os dois bytes de cabeçalho quando incompatíveis, sem despejar pacotes/credenciais.
- Responde ao diagnóstico de datagramas de descoberta recebidos e não reconhecidos. A captura anterior não incluía a identidade/cabeçalho completos; a causa exata e o resultado desta atualização ainda dependem do reteste no aparelho.
- 55 verificações Java e 16 do editor passaram localmente; APK assinado com a chave anterior para atualização sem desinstalar.

## Android 1.0.1 · 2026-10-18

- Busca também pelo gateway da interface Wi-Fi, além dos broadcasts da sub-rede e limitado; escuta por oito segundos.
- Descoberta aceita resposta válida de uma porta de origem diferente de 5000, como o receptor do Pled original; mantém validação de quadro, identidade e origem na mesma rede.
- Exclui redes VPN da seleção de interface Wi-Fi.
- Diagnóstico persistente da última busca: endereço local, destinos, contagens e motivos resumidos de rejeição, com botão para copiar. Não guarda senha, SSID ou conteúdo bruto dos pacotes e não envia diagnósticos automaticamente.
- Diferencia ausência de respostas de respostas não reconhecidas. A falha relatada no hardware ainda precisa ser retestada com este APK; não há confirmação de qual condição ocorreu no telefone.
- 44 verificações Java e 16 do editor passaram localmente; assinatura preservada para atualizar sobre a versão anterior.

## Prévia pública · 2026-10-18

Publicação conjunta dos aplicativos, tutoriais de instalação/uso/rede, código-fonte, licença MIT, testes e CI. Os downloads públicos começam com exemplos genéricos; bibliotecas pessoais e chaves de assinatura ficam fora do repositório.

### Windows 0.5.0

- Editor de cinco canais, curvas, tabela de precisão, rampas, perfis, desfazer e backups.
- USB HID com caminhos para TC420 clássica e variante Pled V2.2.
- Envio individual e substituição de até 16 perfis, com revisão e confirmação.
- Sincronização do relógio na barra principal.
- Recife vetorial animado com corais e peixes-palhaço.
- Descoberta IPv4/UDP, configuração do roteador e envio de perfis por Wi-Fi.
- Testes de validação, transferências simuladas, UDP e renderização da interface.

### Android 1.0.0

- Aplicativo para Android 8+ exclusivamente por Wi-Fi.
- Editor por toque, controles de intensidade, tabela e assistente de ciclo.
- Biblioteca com importação JSON/LMF e exportação JSON pelo seletor Android.
- Recife SVG animado e prévia interpolada de 24 horas.
- Busca, configuração do roteador, relógio, retomada e envio individual/em lote.
- Comunicação vinculada à rede Wi-Fi mesmo sem internet; tratamento de desconexão e interrupção, sem repetição automática de gravação.
- Testes Java/JavaScript e APK assinado para instalação direta.

### Validação pendente

Instalação/execução Android e funções de gravação/configuração/relógio nas controladoras físicas. A versão pública é marcada como prévia por esse motivo. [Matriz completa](docs/COMPATIBILIDADE.md).
