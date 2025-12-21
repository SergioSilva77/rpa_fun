# Lógica (losango de decisão)

## Item e forma

- Item do inventário: `RpaInventory.App/Inventory/Items/Logic/LogicDecisionItem.cs`
- Kind: `WorkspaceShapeKind.LogicDecision`

Ele cria um losango (diamond) que representa uma decisão com duas saídas:

- **TRUE** (marcador verde com ✓)
- **FALSE** (marcador vermelho com ✕)

## Como criar as saídas

1. Coloque um losango de decisão no workspace.
2. Segure `SHIFT` e arraste uma linha a partir dele.
3. A primeira saída marcada é **TRUE**, a segunda é **FALSE**.
4. A partir da terceira, aparece um menu no mouse para escolher:
   - `Condição Verdadeira`
   - `Condição Falsa`

Ao escolher uma condição, se já existir uma linha daquela condição, ela é removida e a nova vira a atual.

## Onde está implementado

Em `RpaInventory.App/MainWindow.xaml.cs`:

- `ConfigureDraftLogicBranch()` decide a marcação do draft (TRUE/FALSE/nenhuma)
- `FinalizeDraftLogicBranch(...)` aplica a marcação na linha criada
- `ShowDecisionBranchPicker(...)` abre o menu quando já existem TRUE e FALSE
- `AssignDecisionBranch(...)` substitui a saída antiga daquela condição

O marcador é desenhado no meio da linha via bindings:

- `RpaInventory.App/Workspace/ViewModels/LineViewModel.cs` (`BranchKind`, `MarkerX/Y`, `MarkerFill`, `MarkerText`)
- `RpaInventory.App/Workspace/ViewModels/DraftLineViewModel.cs`
- `RpaInventory.App/MainWindow.xaml` (template da linha + draft marker)

