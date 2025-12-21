import fitz
import os
import sys

# =========================
# CONFIG
# =========================

caminho_pasta_alvo = st['folder_envelope'][0]
id1 = '1'

COMPRIMIR_AO_MAXIMO = False

# Carta entra no merged? (você disse que pode unificar, mas precisa manter o arquivo carta separado).
# Se quiser que a carta entre no merged, coloque True.
INCLUIR_CARTA_NO_MERGED = False

# Evita "prints parciais" (end=" ") que podem atrapalhar integração.
# Se quiser silêncio total, coloque LOG = False.
LOG = True
LOG_EM_STDERR = True


def log(msg: str) -> None:
    if not LOG:
        return
    print(msg, file=(sys.stderr if LOG_EM_STDERR else sys.stdout), flush=True)


def main() -> None:
    if not os.path.isdir(caminho_pasta_alvo):
        raise RuntimeError(f"O caminho informado não existe ou não é uma pasta: {caminho_pasta_alvo}")

    nome_pasta = os.path.basename(os.path.normpath(caminho_pasta_alvo))
    log(f"Processando pasta: {nome_pasta}")

    arquivos = os.listdir(caminho_pasta_alvo)

    # 1) Varredura única
    merged_encontrado = None
    cartas = []
    candidatos = []

    for f in arquivos:
        path = os.path.join(caminho_pasta_alvo, f)
        if not os.path.isfile(path):
            continue

        up = f.upper()

        if not up.endswith(".PDF"):
            continue

        # Se já existe merged, é skip (e não entra como candidato)
        if up.startswith("MERGED"):
            merged_encontrado = f
            continue

        # Carta é separada e nunca entra como candidato (para manter fora do merged)
        if "CARTA" in up:
            cartas.append(f)
            continue

        candidatos.append(f)

    # 2) Regras de negócio
    if merged_encontrado:
        log(f"-> SKIP: Já existe merged na pasta: {merged_encontrado}")
        return  # termina normalmente (sem sys.exit)

    if not candidatos:
        log("-> SKIP: Sem PDFs elegíveis para unir.")
        return

    # Se você NÃO inclui carta no merged, precisa de 2+ candidatos para valer a pena
    if len(candidatos) < 2 and not (INCLUIR_CARTA_NO_MERGED and cartas):
        log("-> SKIP: Apenas 1 PDF elegível (não há o que unir).")
        return

    # 3) Merge
    candidatos.sort()
    cartas.sort()

    caminhos_candidatos = [os.path.join(caminho_pasta_alvo, f) for f in candidatos]
    caminhos_cartas = [os.path.join(caminho_pasta_alvo, f) for f in cartas]

    saida_final = os.path.join(caminho_pasta_alvo, f"merged_{nome_pasta}.pdf")
    saida_tmp = saida_final + ".tmp"

    # remove tmp antigo
    try:
        if os.path.exists(saida_tmp):
            os.remove(saida_tmp)
    except Exception:
        pass

    pdf_final = fitz.open()
    inseridos = []  # só deletar o que realmente entrou no merged

    try:
        # opcional: inserir carta no merged, mas SEM deletar a carta
        if INCLUIR_CARTA_NO_MERGED:
            for p in caminhos_cartas:
                try:
                    with fitz.open(p) as doc:
                        pdf_final.insert_pdf(doc)
                except Exception as e:
                    log(f"[Aviso] Falha ao ler carta '{os.path.basename(p)}': {e}")

        for p in caminhos_candidatos:
            try:
                with fitz.open(p) as doc:
                    pdf_final.insert_pdf(doc)
                inseridos.append(p)
            except Exception as e:
                log(f"[Aviso] Falha ao ler '{os.path.basename(p)}': {e}")

        if pdf_final.page_count == 0:
            log("-> SKIP: Nenhum PDF pôde ser lido para merge.")
            return

        if COMPRIMIR_AO_MAXIMO:
            pdf_final.save(saida_tmp, garbage=4, deflate=True)
        else:
            pdf_final.save(saida_tmp, garbage=3)

    finally:
        pdf_final.close()

    os.replace(saida_tmp, saida_final)

    # 4) Limpeza (nunca deletar carta)
    removidos = 0
    for p in inseridos:
        try:
            os.remove(p)
            removidos += 1
        except Exception as e:
            log(f"[Aviso] Falha ao deletar '{os.path.basename(p)}': {e}")

    msg_carta = " (Carta mantida separada)" if cartas else ""
    log(f"✓ SUCESSO: {removidos} arquivos unidos em '{os.path.basename(saida_final)}'{msg_carta}")


# Execução sem sys.exit em fluxo normal
try:
    main()
except Exception as e:
    # Aqui sim é “erro de verdade” (Process Studio deve marcar como falha)
    log(f"ERRO CRÍTICO: {e}")
    raise
