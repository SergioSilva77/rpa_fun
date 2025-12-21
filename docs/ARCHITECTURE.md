# Arquitetura

## Visão geral

O app é um WPF (.NET 8) com duas áreas principais:

- **Inventário**: seções + slots (7 colunas × 30 linhas) e **mochila** fixa (7 slots).
- **Área de trabalho (Workspace)**: canvas com **zoom/pan**, **formas**, **linhas conectáveis**, **seleção** e **simulação**.

## Pastas principais

- `RpaInventory.App/Inventory`
  - `Sections/`: IDs e metadados das seções
  - `Catalog/`: catálogo por reflexão (descobre itens automaticamente)
  - `Items/`: cada item é uma classe (SOLID: ação dentro do item)
  - `ViewModels/`: `MainViewModel`, slots, comandos
- `RpaInventory.App/Workspace`
  - `ViewModels/`: formas, linhas, pontos, draft line, preview, bolas
  - `Geometry/`: matemática para projeção/snap
  - `Simulation/`: motor do `START` (bolinhas)
- `RpaInventory.App/MainWindow.xaml(.cs)`
  - composição WPF + handlers de interação (drag, seleção, snap, zoom, delete…)

## ViewModels centrais

- `RpaInventory.App/Inventory/ViewModels/MainViewModel.cs`
  - Estado do inventário (aberto/fechado, seção selecionada)
  - `Slots` do inventário e `BackpackSlots` (mochila/hotbar)
- `RpaInventory.App/Workspace/ViewModels/WorkspaceViewModel.cs`
  - Coleções: `Shapes`, `Images`, `Lines`, `Balls`
  - `DraftLine` (linha temporária do SHIFT)
  - `SnapPreview` (preview de conexão)

## Interação (code-behind)

A interação está em `RpaInventory.App/MainWindow.xaml.cs`:

- Criação de linha com `SHIFT` (draft line → linha real ao soltar)
- Arraste de endpoints (bolinhas vermelhas) + snap
- Seleção por retângulo + movimento proporcional (efeito “galhos”)
- Zoom no cursor (`CTRL + Scroll`) e barras de rolagem do workspace
- `Delete` remove selecionados

## SOLID na prática

- **Itens do inventário**: cada item implementa `IInventoryItem` e (se aplicável) `IWorkspacePlaceableInventoryItem`.
  - `Execute(...)` representa a ação do item (RPA, mensagem, etc.).
  - `PlaceOnWorkspace(...)` cria o nó/forma/linha na área de trabalho.
- **Motor do workspace**: pontos e superfícies são abstrações (`IMovableWorkspacePoint`, `IWorkspaceSurface`), permitindo conectar linhas em:
  - ponto livre
  - ponto de outra linha (qualquer T do segmento)
  - ponto na borda de uma forma

