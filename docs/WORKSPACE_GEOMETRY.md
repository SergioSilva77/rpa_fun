# Geometria e Snap

## Candidatos de snap

O snap é calculado em `RpaInventory.App/MainWindow.xaml.cs` (`FindBestSnapCandidate`).

Ele considera, nessa ordem:

1. **Pontos existentes** (endpoints de linhas já existentes)
2. **Projeção em qualquer parte de uma linha** (ponto mais próximo no segmento)
3. **Borda de formas/imagens** (ponto mais próximo na borda)

O preview é exibido por `Workspace.SnapPreview` (um círculo verde).

## Distância em pixels vs world

O limiar (`SnapThresholdPixels`) é em **pixels**, mas o workspace está em world.  
Por isso, o limiar é convertido para world usando o `scale` atual do `WorldTransform`.

## Projeção em um segmento (linhas)

Para um ponto `P` e um segmento `AB`, a projeção encontra um `t` (0..1):

- `t = clamp( dot(P-A, B-A) / |B-A|², 0, 1 )`
- `Proj = A + t*(B-A)`

Implementação: `RpaInventory.App/Workspace/Geometry/WorkspaceGeometry.cs` (`ProjectPointOntoSegment`).

O snap em linhas cria um `PointOnLineWorkspacePoint(Line, T)`, que mantém o ponto “colado” naquele `T` da linha.

## Ponto mais próximo na borda (formas)

Formas são tratadas como `IWorkspaceSurface`:

- Retângulos: ponto mais próximo **na borda** (se o cursor estiver dentro, escolhe a borda mais próxima)
- Losango (diamond / decisão): projeta o ponto nas 4 arestas do losango e pega a menor distância
- START: tratado como círculo (projeção radial no perímetro)

Ao snapar em uma forma, cria um `PointOnShapeWorkspacePoint(surface, localX, localY)` (local = relativo ao topo-esquerdo da forma).

## Tipos de ponto (por que isso importa)

- `FreeWorkspacePoint`
  - guarda `X/Y` próprios
  - mover = atualizar coordenadas
- `PointOnLineWorkspacePoint`
  - guarda `ParentLine` + `T`
  - `X/Y` sempre recalculado pela linha
- `PointOnShapeWorkspacePoint`
  - guarda `Shape` + `LocalX/LocalY`
  - `X/Y` recalculado pela forma

Isso permite conexões “hiper conectadas” e movimento em cascata (efeito galhos).

