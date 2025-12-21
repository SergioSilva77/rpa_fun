# Itens do inventário: como cadastrar

## Como o catálogo descobre itens

O catálogo padrão (`RpaInventory.App/Inventory/Catalog/ReflectionInventoryCatalog.cs`) varre o assembly e instancia automaticamente qualquer classe que:

- implementa `IInventoryItem`
- não é abstrata/interface
- possui construtor público sem parâmetros

Os itens são agrupados por `SectionId` e ordenados por:

1. `SlotIndex` (se informado)
2. `DisplayName`

## Criando um item simples (ação)

Implemente `IInventoryItem`:

- `Id`: identificador único
- `DisplayName` / `Description`
- `SectionId`: seção onde aparece
- `Execute(IExecutionContext)`: ação do item

O `IExecutionContext` expõe `ShowInfo(...)`/`ShowError(...)` e pode ser expandido depois para executar RPA real.

## Criando um item “arrastável” para o workspace

Implemente também `IWorkspacePlaceableInventoryItem` e crie o objeto no workspace:

- `WorkspaceViewModel.Shapes.Add(new WorkspaceShapeViewModel(...))`
- `WorkspaceViewModel.Lines.Add(new LineViewModel(...))`
- `WorkspaceViewModel.Images.Add(new WorkspaceImageViewModel(...))`

Veja exemplos em:

- `RpaInventory.App/Inventory/Items/Shapes/ShapeSquareItem.cs`
- `RpaInventory.App/Inventory/Items/Shapes/ShapeLineItem.cs`
- `RpaInventory.App/Inventory/Items/Logic/LogicDecisionItem.cs`

## Criando/ajustando seções

As seções visíveis ficam no catálogo:

- Topo: `CreateTopSections()`
- Inferior: `CreateBottomSections()`

O ID vem de `RpaInventory.App/Inventory/Sections/InventorySectionId.cs`.

