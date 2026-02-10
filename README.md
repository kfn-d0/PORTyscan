# 🔍 PORTy Scan

Scanner de portas para Windows com interface gráfica moderna, desenvolvido em **C#** com **.NET 9** e **WPF** (Windows Presentation Foundation).

## ✨ Funcionalidades

- **Scan TCP e UDP** com detecção de status (Open, Closed, Filtered, Open|Filtered)
- **Múltiplos alvos** — IP único, CIDR (`192.168.1.0/24`), range (`192.168.1.1-254`) ou lista separada por vírgula
- **Presets de portas** — Top 20, Web, Database, Top 100 ou portas customizadas
- **Resolução DNS reversa** — Exibe o hostname dos IPs com portas abertas (com cache)
- **Identificação de serviços** — 47 serviços mapeados (FTP, SSH, HTTP, RDP, bancos de dados, etc.)
- **Scan paralelo** — De 10 a 500 threads simultâneas
- **Timeout configurável** — De 100ms a 5000ms
- **Progresso em tempo real** — Barra de progresso, contadores e timer
- **Cancelamento** — Interrompe o scan a qualquer momento, mantendo resultados parciais
- **Exportação CSV** — Tabela com Host, Hostname, Port, Protocol, Status, Service, Timestamp
- **Exportação HTML** — Relatório estilizado com tema dark, cards de estatísticas e tabela


## 📖 Como Usar

1. **Target** — Insira o IP, hostname ou range. Exemplos:
   - `192.168.1.1`
   - `192.168.1.0/24`
   - `192.168.1.1-254`
   - `google.com`
   - `192.168.1.1, 10.0.0.1`

2. **Protocol** — Selecione TCP (recomendado), UDP ou ambos

3. **Port Preset** — Escolha um preset ou selecione "Custom" para portas específicas
   - Formato custom: `80,443,8080-8090`

4. **Threads/Timeout** — Ajuste o paralelismo e timeout com os sliders

5. **Start Scan** — Inicia o scan com progresso em tempo real

6. **Export** — Exporte os resultados como CSV ou HTML (salvo no Desktop)


### Pré-requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

- ## ⚠️ Aviso Legal

Esta ferramenta é destinada **exclusivamente para uso em redes e sistemas que você tem autorização para testar**. 
O uso não autorizado de scanner de portas pode violar leis e regulamentos. O autor não se responsabiliza pelo uso indevido desta ferramenta.
