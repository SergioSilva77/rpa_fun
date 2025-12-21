import fitz
import os
import sys

# --- CONFIGURAÇÕES ---

# Esta é a variável que seu outro programa deve preencher/substituir
# Exemplo: r'C:\Projeto\01 - A Extrair\Pasta_Cliente_X'
caminho_pasta_alvo = r'${caminho_da_pasta}'

# Configuração de performance
COMPRIMIR_AO_MAXIMO = False 

# --- INÍCIO DO PROCESSO ---

if not os.path.exists(caminho_pasta_alvo) or not os.path.isdir(caminho_pasta_alvo):
    print(f"Erro: O caminho informado não existe ou não é uma pasta: {caminho_pasta_alvo}")
    sys.exit(1) # Retorna erro para o programa chamador

# Pega o nome da pasta para usar no nome do arquivo final (ex: "Pasta_Cliente_X")
nome_pasta = os.path.basename(os.path.normpath(caminho_pasta_alvo))

print(f"Processando pasta: {nome_pasta}...", end=" ", flush=True)

try:
    # 1. Varredura única dos arquivos
    arquivos_na_pasta = os.listdir(caminho_pasta_alvo)
    
    tem_merged = False
    tem_carta = False
    pdfs_candidatos = []

    for f in arquivos_na_pasta:
        f_upper = f.upper()
        path_completo = os.path.join(caminho_pasta_alvo, f)
        
        # Ignora subpastas, foca só em arquivos
        if not os.path.isfile(path_completo):
            continue

        # Checagem de Merged
        if f_upper.startswith('MERGED'):
            tem_merged = True
        
        # Checagem de Carta
        if 'CARTA' in f_upper:
            tem_carta = True
            # Carta não entra na lista de união
        
        # Se for PDF e não for Merged nem Carta, é candidato
        elif f_upper.endswith('.PDF'):
            pdfs_candidatos.append(f)

    # --- 2. REGRAS DE NEGÓCIO ---

    # Se já tem merged, aborta imediatamente (independente de ter carta ou não)
    if tem_merged:
        print("-> ABORTADO: Já existe arquivo Merged nesta pasta.")
        sys.exit(0) # Sai do script com sucesso (sem erro, apenas pulou)

    # Validações de quantidade
    if not pdfs_candidatos:
        print("-> ABORTADO: Sem PDFs elegíveis para unir.")
        sys.exit(0)

    if len(pdfs_candidatos) == 1:
        print("-> ABORTADO: Apenas 1 arquivo disponível (não há o que unir).")
        sys.exit(0)

    # --- 3. EXECUÇÃO DO MERGE ---
    pdfs_candidatos.sort()
    caminhos_completos = [os.path.join(caminho_pasta_alvo, f) for f in pdfs_candidatos]
    arquivo_saida = os.path.join(caminho_pasta_alvo, f"merged_{nome_pasta}.pdf")
    
    pdf_final = fitz.open()
    
    for pdf_path in caminhos_completos:
        try:
            with fitz.open(pdf_path) as pdf:
                pdf_final.insert_pdf(pdf)
        except Exception as e:
            print(f"[Erro ao ler {os.path.basename(pdf_path)}]", end=" ")
    
    # Salva o arquivo
    if COMPRIMIR_AO_MAXIMO:
        pdf_final.save(arquivo_saida, garbage=4, deflate=True)
    else:
        pdf_final.save(arquivo_saida, garbage=3)
    
    pdf_final.close()
    
    # --- 4. LIMPEZA ---
    arquivos_removidos = 0
    for pdf_path in caminhos_completos:
        try:
            os.remove(pdf_path)
            arquivos_removidos += 1
        except Exception as e:
            print(f"[Falha ao deletar: {os.path.basename(pdf_path)}]", end=" ")

    msg_carta = " (Carta mantida separada)" if tem_carta else ""
    print(f"✓ SUCESSO: {arquivos_removidos} arquivos unidos em 'merged_{nome_pasta}.pdf'{msg_carta}")

except Exception as e:
    print(f"\nERRO CRÍTICO: {e}")
    sys.exit(1) # Retorna erro para o programa chamador