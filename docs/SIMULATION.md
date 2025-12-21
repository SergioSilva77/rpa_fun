# Simulação (START → bolinhas)

## Como usar

1. Arraste o item `START` (seção **Formas**) para a área de trabalho.
2. Conecte uma ou mais linhas no `START` (snap das pontas das linhas no círculo).
3. Clique no botão `START` (topo da janela).

## O que acontece

O motor está em `RpaInventory.App/Workspace/Simulation/LineFlowSimulator.cs`.

- Ele constrói um **grafo** a partir das linhas e conexões do workspace.
- Cria uma “bolinha” para cada aresta que sai do `START`.
- Cada bolinha percorre uma aresta em velocidade constante.
- Em bifurcação (mais de uma saída), a bolinha **se divide** (spawn de novas bolas).
- Quando chega em um ponto sem saída, aquela bolinha termina.
- Se existir loop (ciclo), a bolinha continua em loop na mesma velocidade.

## Nós e arestas

### Nós (NodeKey)

- `FreePointNodeKey`: ponto livre compartilhado
- `LineAnchorNodeKey`: conexão em um `T` específico de uma linha
- `ShapeAnchorNodeKey`: conexão em um ponto da forma

### Arestas (Edge)

Cada linha vira uma sequência de segmentos entre âncoras ordenadas por `T`.

- O grafo é **bidirecional** (A→B e B→A).
- Para evitar “voltar imediatamente”, a transição remove a aresta que voltaria para o nó anterior.

## “Teleport” em formas

`GetOutgoingEdges` trata `ShapeAnchorNodeKey` como um **hub**:

- Se a bolinha chega em uma forma, ela pode sair por qualquer outra linha conectada naquela mesma forma.

