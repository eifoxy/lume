# ◒ Lume

**Um dia de luz, do amanhecer ao anoitecer.**

Editor de iluminação para controladoras de aquário compatíveis com TC420 e com o protocolo Wi-Fi do Pled V2.2. Aplicativos para **Windows** e **Android**, em português, com curvas de cinco canais, perfis e um recife animado que acompanha a programação.

[![Verificações](https://github.com/eifoxy/lume/actions/workflows/checks.yml/badge.svg)](https://github.com/eifoxy/lume/actions/workflows/checks.yml)
[![Licença MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-91e3c7)](LICENSE)

> **Prévia em desenvolvimento:** testes automatizados e simulação de comunicação passaram. Gravação, sincronização do relógio e configuração Wi-Fi ainda precisam ser confirmadas na controladora física. A descoberta foi confirmada no aparelho; o Android 1.0.5 aguarda reteste após timeout no envio e na abertura do perfil após limpeza. Confira a [matriz de compatibilidade](docs/COMPATIBILIDADE.md) antes de enviar uma programação.

## Baixar e instalar

| Aplicativo | Download | Como instalar |
|---|---|---|
| Windows x64 · 0.5.0 | [ZIP completo, com .NET incluído](https://github.com/eifoxy/lume/releases/download/preview-2026.10.18/Lume-Windows-0.5.0-win-x64.zip) | Extraia tudo e abra `Lume.exe`. |
| Windows · 0.5.0 | [ZIP leve, requer .NET Desktop Runtime 8](https://github.com/eifoxy/lume/releases/download/preview-2026.10.18/Lume-Windows-0.5.0-portable.zip) | Instale o runtime, extraia tudo e abra `Lume.exe`. |
| Android 8+ · 1.0.5 | [APK](https://github.com/eifoxy/lume/releases/download/android-v1.0.5/Lume-Android-1.0.5.apk) | Baixe no telefone e abra pelo gerenciador de arquivos. |

[Todas as versões e checksums](https://github.com/eifoxy/lume/releases) · [Tutorial Windows](docs/INSTALACAO-WINDOWS.md) · [Tutorial Android](docs/INSTALACAO-ANDROID.md) · [Configurar Wi-Fi](docs/WIFI.md)

## Recursos

- **Cinco canais, 48 horários por dia:** intensidades de 0 a 100% e interpolação entre pontos, inclusive na passagem da meia-noite.
- **Curva em destaque:** escolha canais, explore horários e edite com controles deslizantes ou valores numéricos. No Windows, também é possível desenhar a curva e ampliar/isolar o canal.
- **Ciclo suave:** gere amanhecer, pico e anoitecer escolhendo horários, rampas e intensidades máximas.
- **Perfis locais:** crie, renomeie, duplique, exclua, desfaça alterações e faça backup. Importe bibliotecas JSON e arquivos `.lmf` do Pled V2.2.
- **Envio individual ou em lote:** grave uma posição ou substitua toda a memória por até 16 perfis. A substituição exige confirmação e mostra os nomes curtos usados no aparelho.
- **Wi-Fi:** descoberta local, busca por IP, configuração inicial do roteador, envio de perfis, sincronização do relógio e retomada do programa.
- **Recife animado:** corais abrem com o aumento da luz simulada e recolhem quando ela diminui; três peixes-palhaço nadam pelo aquário.
- **Salvamento com recuperação:** biblioteca local, gravação atômica e cópia antes do envio. O backup local não lê a memória da controladora.
- **Sem conta e sem nuvem:** o editor funciona offline; a comunicação com o aparelho usa a rede local ou USB no Windows.

### Diferenças entre os aplicativos

| Função | Windows | Android |
|---|:---:|:---:|
| Curvas, tabela, ciclos, cinco canais e perfis | ✓ | ✓ |
| Recife animado com prévia do dia | ✓ | ✓ |
| Descoberta e configuração Wi-Fi | ✓ | ✓ |
| Envio de perfis e relógio por Wi-Fi | ✓ | ✓ |
| USB HID | ✓ | — |
| Ajuste instantâneo dos LEDs físicos | USB, experimental | — |
| Importação `.lmf` / JSON | ✓ | ✓ |
| Importação de projeto `.lcm` | ✓ | — |
| Exportação `.lmf` / JSON | ✓ | JSON |

## Uma prévia do recife

<img src="docs/images/recife-celular.png" alt="Prévia da interface Android em largura de celular, com corais abertos ao meio-dia" width="320" />

*Captura da interface Android no navegador em largura de celular; não é uma captura de execução num Android físico.*

![Recife vetorial no Windows](docs/images/recife-dia.png)

*O visual é ilustrativo: não mede PAR, espectro, potência ou resposta biológica dos animais. A simulação não altera a luz física.*

## Começar pelo Wi-Fi

1. Conecte o computador ou telefone à rede criada pela controladora. Aceite permanecer nessa rede mesmo sem internet.
2. No Lume, abra a conexão Wi-Fi, busque e selecione o aparelho.
3. Envie o nome e a senha da rede de casa, usando a segurança correspondente ao roteador.
4. Conecte o computador ou telefone à rede de casa, busque novamente e selecione a controladora.
5. Sincronize o relógio e envie seus perfis.

Isso exige **hardware Wi-Fi compatível**. Uma TC420 comum, sem módulo Wi-Fi, não pode se conectar sem fio pelo aplicativo. Android funciona exclusivamente por Wi-Fi. [Tutorial com detalhes e solução de problemas →](docs/WIFI.md)

## Documentação

- [Instalar e usar no Windows](docs/INSTALACAO-WINDOWS.md)
- [Instalar e usar no Android](docs/INSTALACAO-ANDROID.md)
- [Configurar o Wi-Fi e encontrar o aparelho](docs/WIFI.md)
- [Editar curvas, trabalhar com perfis e enviar horários](docs/USO.md)
- [Compatibilidade, limites e estado dos testes](docs/COMPATIBILIDADE.md)
- [Compilar e executar testes](docs/DESENVOLVIMENTO.md)
- [Arquitetura e protocolo](docs/PROTOCOLO.md)
- [Análise do APK Smart WiFi Timer que funcionava](docs/ANALISE-SMART-WIFI-TIMER.md)
- [Histórico de versões](CHANGELOG.md)
- [Contribuir e relatar problemas](CONTRIBUTING.md)

## Código

```text
windows/src/          Aplicativo WPF / C# / .NET 8
android/app/          Java nativo + interface WebView local / SVG
android/tests/        Testes de protocolo, UDP e editor
docs/                Tutoriais, arquitetura e imagens
scripts/             Compilação e verificação
.github/workflows/   Verificações automatizadas
```

O código e os desenhos do Lume usam a [licença MIT](LICENSE). O programa original Pled, bibliotecas proprietárias, chaves de assinatura e bibliotecas pessoais de perfis não são distribuídos neste repositório. A biblioteca Android inicial contém apenas um exemplo ilustrativo.

Projeto independente, sem afiliação com os fabricantes das controladoras. Referências de protocolo e documentação oficial estão em [REFERENCIAS.md](docs/REFERENCIAS.md).
