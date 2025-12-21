# RpaInventory (WPF .NET 8)

Inventário estilo Minecraft (criativo) para montar fluxos de automação/RPA na **área de trabalho** usando **formas** e **linhas conectáveis** (snap + preview), com **mochila (hotbar)** fixa e um **simulador** que percorre as linhas a partir de um item `START`.

## Como rodar

- Build: `dotnet build -c Debug RpaInventory.sln`
- Run: `dotnet run -c Debug --project RpaInventory.App`

## Atalhos / controles

- `E`: abre/fecha o inventário
- `CTRL + Scroll`: zoom (no ponto do cursor)
- `SHIFT + Arrastar (botão esquerdo)`: cria uma nova linha (P1 fixo, P2 segue o cursor)
- `ALT + Arrastar bolinha vermelha (P1/P2)`: descola o ponto e move
- `Arrastar com o mouse (área vazia)`: retângulo de seleção (seleciona formas/linhas dentro)
- `Delete`: remove **apenas** os itens selecionados
- Inventário aberto: tecla `1..7` coloca o item (mouse em cima do item do inventário) na mochila
- Botão `START` (topo): inicia a simulação das “bolinhas” percorrendo as linhas a partir do item `START` na área de trabalho

## Documentação

- `docs/ARCHITECTURE.md`
- `docs/WORKSPACE_INTERACTIONS.md`
- `docs/WORKSPACE_GEOMETRY.md`
- `docs/SIMULATION.md`
- `docs/INVENTORY_ITEMS.md`
- `docs/LOGIC_DECISION.md`

