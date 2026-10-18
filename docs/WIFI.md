# Configurar o Wi-Fi

[← Início](../README.md) · [Compatibilidade](COMPATIBILIDADE.md)

O fluxo é o mesmo nos dois aplicativos. Android usa somente Wi-Fi; Windows também oferece USB. Esta função depende de um módulo que fale o protocolo de rede do Pled V2.2. O app não adiciona Wi-Fi a uma TC420 sem esse hardware.

## 1. Entrar na rede criada pelo aparelho

Ligue a controladora e consulte o modo de acesso inicial indicado pelo fabricante. No sistema operacional, escolha a rede Wi-Fi criada pelo aparelho; em alguns módulos o nome começa com `KITELIGHT-WIFI`. O nome e a eventual senha de acesso dependem do equipamento: o Lume não presume uma senha universal.

No Android, **Conexão → Abrir redes Wi-Fi** abre o painel do sistema. No Windows, escolha a rede pelo menu de redes do próprio Windows. Se aparecer um aviso de rede sem internet, permaneça conectado. Ainda não é a etapa de conectar ao roteador de casa.

## 2. Buscar e selecionar

- Windows: **Conectar por Wi-Fi → Buscar na rede**.
- Android: **Conexão → Buscar aparelhos**.

A busca dura aproximadamente cinco segundos no Windows e oito segundos no Android 1.0.1. Ela envia consultas UDP na rede local e mostra identificação e endereço IP dos aparelhos que responderem. No Android, consulta também o gateway da interface Wi-Fi e usa dois destinos de broadcast, sem varrer endereços/portas. Não grava horários nem troca a configuração da luz.

Selecione a controladora correta. No Windows, finalize com **Usar esta controladora** quando quiser começar a operá-la; para a configuração inicial, use os campos do roteador na própria janela.

Se você já conhece o IP do aparelho, há uma busca direta pelo endereço. O IP precisa estar na mesma rede IPv4 do computador/telefone. Não é uma conexão pela internet.

## 3. Configurar a rede de casa

Ainda conectado à rede do aparelho, preencha:

| Campo | Limite implementado no protocolo |
|---|---|
| Nome da rede (SSID) | De 1 a 32 caracteres ASCII, sem acentos. Diferencie maiúsculas e minúsculas. |
| Senha | De 8 a 32 caracteres ASCII, sem acentos. Não há corte silencioso de senhas longas. |
| Segurança | WPA2-PSK ou WPA-PSK. |
| Criptografia | AES ou TKIP. |

WPA2-PSK/AES é a escolha inicial da interface. Selecione valores compatíveis com o roteador e o módulo. WPA3 e redes empresariais/portais de login não são suportados por esta implementação. Use a faixa suportada pelo aparelho, frequentemente 2,4 GHz.

Revise o destino e confirme **Enviar rede à controladora** no Windows ou **Configurar rede de casa** no Android. A senha não aparece na revisão, não é salva e não entra no backup. Os comandos seguem o protocolo legado local do dispositivo; não há túnel TLS oferecido por esse módulo.

O envio tem duas etapas: rede e senha, com respostas independentes. **Se a confirmação final não chegar, o aparelho pode já ter saído da rede inicial.** Antes de tentar de novo, faça a etapa seguinte e busque no roteador. O app não repete automaticamente credenciais.

## 4. Voltar à rede de casa

1. Conecte o computador ou telefone ao roteador informado.
2. Aguarde a controladora ingressar nessa rede.
3. Faça uma nova busca no Lume e selecione novamente o aparelho. O endereço IP pode mudar.
4. Sincronize o relógio, revise seu perfil e envie a programação.

No Android, a seleção do dispositivo é descartada ao retornar de outra tela/app para evitar reutilizar um destino antigo após trocar de rede. Faça uma nova busca. O app não sincroniza o relógio ou grava perfis sozinho.

## Se não encontrar o aparelho

| Sintoma | O que conferir |
|---|---|
| A rede inicial do aparelho não aparece | Confirme que há módulo Wi-Fi e como ativar o ponto de acesso no modelo. Não faça reset de fábrica sem conhecer o procedimento e ter cópia da programação. |
| Busca sem resultados na configuração inicial | Telefone/PC deve estar na rede do aparelho, mesmo sem internet. Confira a permanência nessa rede. |
| Busca sem resultados em casa | Ambos precisam estar na mesma rede local; rede de convidados, isolamento entre clientes e VLANs podem bloquear UDP. |
| IP conhecido, descoberta por broadcast bloqueada | Experimente a busca por IP. Não é garantia: o firmware e a rede ainda precisam aceitar a comunicação. |
| Porta UDP 5000 ocupada | Feche o Pled, outra instância do Lume e outros controladores que usem essa porta. |
| O Windows bloqueia a resposta | Confira as permissões de rede local do aplicativo no firewall. O Lume não desativa nem altera o firewall automaticamente. |
| Configuração enviada, sem confirmação final | Tente voltar ao roteador e buscar antes de reenviar. |
| Recebeu um erro durante gravação | A memória pode estar incompleta; reconecte e reenvie a biblioteca revisada. Não há repetição automática nem rollback físico. |

Não é necessário expor a porta 5000 na internet ou criar encaminhamento de portas no roteador. O uso previsto é restrito à rede local.

## O que foi realmente testado

### Android: o aparelho apita, mas a lista fica vazia

O apito pode indicar recebimento de uma consulta; sozinho não comprova que a resposta chegou ao telefone. Atualize para [Android 1.0.1](https://github.com/eifoxy/lume/releases/tag/android-v1.0.1), instalando o APK sobre a versão anterior, sem desinstalar.

Permaneça na rede do aparelho mesmo com o aviso de ausência de internet. Para isolar roteamento do telefone, experimente temporariamente sem VPN e dados móveis e repita a busca. Isso é um teste de diagnóstico, não uma confirmação da causa.

Se não aparecer, abra **Diagnóstico da última busca → Copiar diagnóstico**. “Datagramas recebidos: 0” distingue ausência de recepção de respostas que chegaram mas foram rejeitadas por formato ou origem. O relatório mostra IPs locais, gateway, destinos, modelo/versão do Android e contagens; não inclui SSID, senha ou bytes brutos e não é transmitido automaticamente. Revise-o antes de compartilhar.

O Android 1.0.1 removeu a exigência de porta de origem 5000 somente na descoberta, alinhando-a ao receptor original. Os comandos de configuração e gravação continuam com suas validações anteriores.

### Evidência disponível

Os pacotes foram identificados no executável Pled V2.2 fornecido e testados com respostas simuladas, inclusive comunicação UDP real em loopback. A busca feita na rede do PC original encontrou zero aparelhos porque o Wi-Fi da controladora ainda não estava configurado. A descoberta na rede do aparelho, ingresso no roteador e gravação física continuam pendentes de validação. [Estado detalhado →](COMPATIBILIDADE.md)
