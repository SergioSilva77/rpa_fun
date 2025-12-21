# Interações do Workspace

## Coordenadas: viewport × world

Os elementos são desenhados em **coordenadas de mundo (world)**, dentro de `WorldCanvas` com `MatrixTransform` (`WorldTransform`) aplicado.  
Eventos do mouse chegam em **coordenadas de viewport** (pixels da tela), e são convertidos via `ViewportToWorld(...)`.

## Criar linha (SHIFT)

- Handler: `RpaInventory.App/MainWindow.xaml.cs`
  - `StartDraftLine(...)` → ativa `Workspace.DraftLine` (linha tracejada)
  - `UpdateDraftLine(...)` → P2 segue o cursor + calcula snap candidate
  - `FinalizeDraftLine(...)` → cria `LineViewModel` real

Detalhes importantes:

- O P1 do draft já “nasce” com snap se estiver perto de algo (linha/forma/ponto).
- O P2 mostra preview (círculo verde) quando há um candidato válido.
- Ao soltar o mouse (ou soltar `SHIFT`), a linha é criada com:
  - `FreeWorkspacePoint` (livre) ou
  - `PointOnLineWorkspacePoint` (ancorado em outra linha) ou
  - `PointOnShapeWorkspacePoint` (ancorado na borda de uma forma)

## Mover endpoints / descolar (ALT)

- As pontas são as bolinhas vermelhas (`P1` e `P2`).
- Arrastar a bolinha move o endpoint:
  - se for um ponto livre → ele move
  - se estiver ancorado (forma/linha) → segura `ALT` para descolar e então mover

## Seleção por retângulo

- Arrastar em área vazia cria o retângulo azul.
- Ao soltar, seleciona:
  - formas (interseção com `Bounds`)
  - imagens (interseção com `Bounds`)
  - linhas (interseção com o retângulo “bounding box” da linha)

## Mover seleção (efeito “galhos”)

Mover qualquer item selecionado chama `MoveSelectedBy(delta)`.

O movimento é **propagado** para manter conexões:

- Se um endpoint é `FreeWorkspacePoint` → move o ponto.
- Se é `PointOnShapeWorkspacePoint` → move a forma inteira (a linha “segue”).
- Se é `PointOnLineWorkspacePoint` → move a linha “pai” e isso pode puxar outras coisas conectadas.

Para evitar loops, a rotina usa `HashSet` para marcar o que já foi movido (linhas, superfícies, pontos livres).

## Deletar selecionados

- Tecla: `Delete`
- Remove apenas o que está selecionado.
- Ao remover:
  - linhas: qualquer endpoint de outra linha que estava ancorado nelas vira ponto livre
  - formas/imagens: endpoints ancorados nelas viram pontos livres (preservando compartilhamento quando possível)

## Inventário / mochila (hotbar)

- Inventário abre/fecha com `E` e fica centralizado.
- A mochila (7 slots) fica visível mesmo com inventário fechado.
- Você pode:
  - arrastar itens do inventário para a mochila
  - arrastar itens da mochila para a área de trabalho
  - com inventário aberto: tecla `1..7` coloca o item (mouse sobre o item do inventário) no slot correspondente da mochila

