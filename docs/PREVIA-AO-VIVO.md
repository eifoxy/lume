# Prévia na luminária pelo Wi-Fi

[Início](../README.md) · **Português (Brasil)** · [English](en/LIVE-PREVIEW.md) · [Español](es/VISTA-EN-VIVO.md)

Disponível no **Android 1.2.0** para controladoras identificadas como **Smart Timer**, com respostas `88 00`. Usa os comandos de prévia encontrados no APK original fornecido pelo usuário. A implementação foi testada localmente com simuladores; o resultado na luminária ainda precisa de reteste.

## Como usar

1. Conecte o celular ao mesmo Wi-Fi da controladora e aguarde a descoberta.
2. Abra o perfil desejado. Se quiser salvá-lo no aparelho, envie a programação normalmente. Após o envio confirmado, o app abre o Recife.
3. No **Recife**, toque em **Vincular luminária** e confirme que deseja alterar a luz real. A prévia não é ativada sozinha após o envio.
4. Mova o controle de horário ou toque em **Reproduzir dia**. A luminária recebe as cinco intensidades interpoladas do mesmo perfil mostrado no Recife. Pausar o dia mantém o instante atual; a prévia ao vivo continua ativa.
5. Para voltar ao programa salvo, toque em **Encerrar prévia e retomar programa**. Aguarde a confirmação de retomada. Confira no aparelho qual perfil ficou ativo.

Também é possível editar os pontos do perfil atual enquanto testa a luz. As alterações locais continuam sendo salvas no celular; a prévia não regrava a programação da controladora. Trocar de perfil, enviar programação, configurar o Wi-Fi, sincronizar o relógio e procurar outros aparelhos ficam bloqueados durante a sessão. Encerre a prévia antes dessas operações.

## O que significa estar vinculado

O aplicativo envia valores, sem ler a intensidade física de volta. Os comandos de prévia do APK original não aguardam confirmação individual. **Último envio** mostra o horário e os valores cujo datagrama foi enviado pelo celular; não é uma medição nem prova de recepção. UDP pode perder pacotes, e pode haver atraso entre a tela e a luminária.

Para não acumular comandos, a sessão mantém apenas os valores mais recentes e envia no máximo cinco quadros de intensidade por segundo. Valores repetidos não são reenviados continuamente. A interface envia um sinal local de atividade a cada 200 ms; se ele parar por mais de três segundos, a sessão tenta encerrar e retomar o programa.

## Ao sair do app ou perder conexão

Ao sair do primeiro plano, a sessão deixa de enviar intensidades e tenta retomar o programa com `03 08`, exigindo uma resposta Smart `83` com origem e identificação corretas. A tentativa é limitada pelo prazo de resposta de 2,5 segundos. Não há serviço de prévia em segundo plano nem reinício automático da sessão.

Se a conexão cair, o processo for encerrado pelo sistema ou a confirmação não chegar, **a luminária pode permanecer em modo manual no último valor recebido**. O aparelho não oferece um watchdog confirmado que permita prometer retorno automático sem comunicação.

Antes do primeiro comando, o app salva a identificação da sessão para recuperação. Se a retomada ficar pendente, o aviso aparece na próxima abertura. Reconecte a **mesma controladora**, aguarde a descoberta e use **Retomar programação no aparelho**. Novas prévias, gravações, configuração de rede e ajuste do relógio aguardam essa recuperação. A informação não guarda senha nem biblioteca de perfis.

O diagnóstico da última comunicação registra destino local, contagem de quadros enviados e se a retomada foi confirmada, sem expor nome do perfil, SSID, senha ou pacotes brutos.

## Protocolo e limites

- Início: `06 01`, tamanho do nome no byte 5 e nome a partir do byte 6. Dois envios idênticos, separados por 300 ms, como no APK original.
- Intensidades: `06 02`, byte 5 igual a `05`, cinco valores de 0 a 100 nos bytes 6 a 10.
- Quadros de 64 bytes, pedido `55 AA`, identidade em 49 a 60 e checksum em 61.
- Encerramento: `03 08`, confirmação Smart `88 00` com status `83` e identidade do destino.
- Não envia comandos de apagar, gravar perfis, mudar o Wi-Fi ou ajustar o relógio durante a prévia.

Essa função não adiciona prévia Wi-Fi ao Windows ou ao caminho legado Pled. O Windows mantém o ajuste instantâneo por USB. [Análise do APK](ANALISE-SMART-WIFI-TIMER.md) · [Compatibilidade](COMPATIBILIDADE.md).
