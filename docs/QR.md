# Perfis do PC no celular por QR code

**Português (Brasil)** · [English](en/QR.md) · [Español](es/QR.md)

Requer **Windows 0.7.0** e **Android 1.3.0** ou **iOS 0.2.0**. A transferência é offline: não precisa de conta, internet ou da mesma rede Wi-Fi. O QR contém os nomes, os cinco canais e os 48 horários dos perfis da biblioteca do PC. Não contém senha do roteador, endereço da controladora ou comandos para a luminária.

1. No Lume do PC, selecione um perfil e clique em **Enviar ao celular por QR**. Corrija células inválidas antes de abrir a janela.
2. O perfil selecionado é a opção inicial. Marque **Todos os perfis** se quiser transferir a biblioteca inteira.
3. No celular, abra **Perfis > Ler QR do PC** e autorize a câmera quando solicitado. Aponte para o código exibido na tela do computador.
4. Se houver várias partes, use **Próximo** no PC ou marque **Alternar QR codes automaticamente**. A sequência repete a cada dois segundos por código. O celular mostra quantas partes já recebeu; partes repetidas ou fora de ordem são aceitas.
5. Quando todas as partes forem recebidas e verificadas, a câmera fecha e o celular pede confirmação para adicionar os perfis. **Cancelar preserva a biblioteca atual.** Confirmar adiciona os novos perfis sem substituir os existentes, respeitando o limite de 100 perfis no celular.

Se a câmera não conseguir ler, aproxime o telefone, evite reflexos e aumente a janela do PC. Para transferências longas, prefira o modo automático e mantenha o telefone apontado até o contador completar. A leitura encerra depois de dez minutos; nesse caso, comece uma nova leitura. Um envio não pode ser misturado com outro: cancele antes de trocar entre perfil atual e biblioteca inteira.

As imagens da câmera são usadas apenas para reconhecer o QR, sem salvar fotos ou enviar imagens a um servidor. Sair do aplicativo encerra a câmera. Se a permissão foi negada, habilite a câmera nas configurações do aplicativo ou continue usando a importação JSON/LMF.

**Importar não envia nada à luminária.** Depois, confira os perfis no celular e envie a programação pela função de gravação quando desejar. O recurso transfere a biblioteca local do PC; não lê a memória da controladora. Exportar um backup JSON continua disponível e é útil antes de desinstalar.

No iOS, o mínimo permanece **15.4** e a instalação ainda depende de assinatura Apple. [Guia iOS](INSTALACAO-IOS.md). A leitura por câmera nos telefones reais ainda precisa de validação. Os testes automatizados verificam o QR gerado no Windows com o decodificador Android, a reconstrução dos dados, a confirmação/cancelamento e os três idiomas; isso não substitui teste físico de foco e iluminação da câmera.
