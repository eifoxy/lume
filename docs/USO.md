# Curvas, perfis e envio

[← Início](../README.md)

## Entender o dia

Cada perfil tem **cinco canais e 48 pontos**, um a cada 30 minutos, de 00:00 a 23:30. Cada ponto guarda porcentagens inteiras de 0 a 100. A prévia interpola entre os pontos e fecha o ciclo interpolando de 23:30 para 00:00.

Os nomes e cores ajudam a identificar canais, mas não mudam a ordem física CH1–CH5. Renomeie conforme a ligação da sua luminária. Os exemplos de intensidade são ilustrativos, não recomendações de iluminação para espécies.

## Editar

### Windows

Selecione o perfil e **Editar curva** no canal desejado. Sem ativar **Desenhar curva**, um clique escolhe apenas o horário da prévia. Ative o desenho para mudar valores com o mouse. **Isolar canal selecionado** e **Ampliar curva** facilitam trabalhar com uma curva de cada vez.

Os controles editam o ponto de meia hora indicado. Na tabela de precisão, digite uma porcentagem e confirme com Enter. A prévia visual pode estar em um instante interpolado entre dois pontos: confira o ponto indicado antes de editar.

### Android

Na aba **Curvas**, toque no gráfico para escolher um ponto e use as setas para ir ao ponto anterior/seguinte. Selecione um canal para destacar sua curva. Ajuste pelos sliders ou números; a tabela completa oferece edição de cada célula.

### Criar um ciclo

Escolha a hora de acender, a de apagar, a duração das duas rampas e os picos dos cinco canais. O intervalo deve terminar depois do início no mesmo dia e comportar as duas rampas. O assistente substitui os 48 pontos do perfil atual. Use **Desfazer** para recuperar a edição anterior.

## Perfis e arquivos

Crie e duplique perfis para experimentar sem perder o anterior. O limite local é 100 perfis; o envio em conjunto aceita até 16 posições da controladora. Os arquivos JSON do Lume usam o mesmo modelo nos dois aplicativos.

| Formato | Como usar |
|---|---|
| `.json` do Lume | Backup/exportação de toda a biblioteca. Importação adiciona perfis. |
| `.lmf` do Pled V2.2 | Um perfil com 48 horários e 5 canais. Importação nos dois apps, exportação no Windows. |
| `.lcm` do Pled | Projeto com referências a arquivos LMF locais. Importação somente no Windows; os arquivos referenciados precisam existir. |

O salvamento automático é local. Um backup interno não sobrevive à desinstalação/limpeza de dados do Android. Exporte um arquivo JSON para guardar uma cópia fora do aplicativo. Exportar o JSON no Windows e importá-lo no Android permite levar a biblioteca para o telefone; o caminho inverso também funciona.

## Enviar perfis

Primeiro encontre e selecione a controladora. A revisão mostra o perfil local e o nome abreviado que o aparelho receberá. O protocolo usa nomes de até sete caracteres ASCII; abreviações repetidas recebem sufixos para evitar colisões. O nome no seu arquivo local é preservado.

### Um perfil

Escolha **Somente este perfil** e uma posição de 1 a 16. O app substitui essa posição, sem executar a limpeza total. Confira a posição para não sobrescrever outro programa por engano.

### Todos os perfis

Escolha a opção de substituir a memória inteira. Confira a lista e marque a confirmação: o aparelho recebe um comando de **apagar todos os perfis existentes**, seguido dos perfis locais, em ordem, a partir da posição 1.

“Apagar perfis” não remove os canais físicos da luminária. Os cinco canais continuam existindo; suas intensidades são definidas em cada perfil.

Todos os dados são validados e há um backup da biblioteca local antes do primeiro comando de gravação. **O Lume não lê a programação já existente no dispositivo**, portanto esse backup não recupera programas que só estavam na controladora.

Mantenha a conexão ativa e o app aberto. Cada comando precisa de confirmação. Se houver falha, o app para e informa quantos perfis foram concluídos; pode haver uma limpeza ou gravação parcial. Não existe rollback do dispositivo ou repetição automática de comandos. Reconecte e reenvie o conjunto revisado.

Um envio confirmado não faz leitura de retorno de toda a memória e não seleciona automaticamente o perfil ativo. Escolha o perfil no aparelho conforme seu modo de operação.

## Relógio, retomada e prévia

- **Sincronizar horário:** envia a data e a hora local do PC/telefone. Confira o relógio e fuso do sistema antes de usar.
- **Retomar programa:** sai do modo manual; não é um seletor de perfil.
- **Aplicar este instante aos LEDs:** disponível apenas no Windows por USB; envia as intensidades reais depois de confirmação.
- **Recife / prévia do dia:** não envia comandos. Corais e peixes são uma representação visual; o movimento não comprova o estado físico da luminária.

A ativação real, o relógio e a gravação ainda aguardam confirmação no hardware. Consulte a [matriz de validação](COMPATIBILIDADE.md).
