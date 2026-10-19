# Contribuir

Correções, melhorias de acessibilidade, testes e validação de compatibilidade são bem-vindos. Veja [como compilar e testar](docs/DESENVOLVIMENTO.md) e abra uma issue para discutir mudanças no protocolo ou novos modelos antes de implementá-las.

Para relatar um problema, inclua versão do Lume, sistema, modelo/firmware, tipo de conexão, passos para reproduzir e resultado esperado/observado. Para confirmar hardware, diga qual comando funcionou e se a confirmação veio do app ou foi conferida no aparelho. Uma resposta de pacote e uma leitura de retorno completa são evidências distintas.

Não publique senhas, SSID real, endereços públicos, chaves de assinatura, tokens, bibliotecas pessoais ou capturas com dados privados. Substitua dados por exemplos antes de anexar arquivos. Não inclua binários proprietários do fabricante.

Execute os testes pertinentes. Preserve validação antes da primeira mutação, confirmação antes de limpar a memória, backup local e prazo de resposta. A exceção de recuperação Smart Timer é limitada a três tentativas totais do mesmo ponto indexado e bytes idênticos, somente em timeout sem resposta válida do aparelho. Não amplie essa política para cabeçalhos, limpeza, credenciais, relógio ou encerramentos. A simulação deve continuar sem efeitos no hardware. Mudanças de comportamento devem atualizar os tutoriais e a matriz de compatibilidade.
