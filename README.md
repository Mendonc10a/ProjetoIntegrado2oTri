# INTEGRANTES:
Arthur Yuzo Sáber Shida
João Gabriel Capistrano Mendonça
# Turma:
34DS
# Professores:
Ana Leticia G. Gonçalves (SEB)
Daniel Albino Mosca  (DPL e CGR)
José Andery Carneiro  (LPR)

# Explicação:
Nosso projeto consiste em uma simulação de um sensor de luz (trimpot), que conforme chega tensão no A0(pino do STM que lê os valores da tensão), o nosso código do STM converte os valores do pino A0 em binários e envia para o cabo USB, após isso o C# recebe os valores do USB procurando pelas portas COM qual dispositivo está com os valores binários do STM e envia em HTTP para nosso site HTML, assim as informações são enviadas para nossa IA que recebe os dados e classifica os dados do nosso "sensor" se estão Escuro, Adequado, Excessivo usando a Árvore de Decisões e envia para nosso site HTML para assim mostrar os valores que está sendo lidos e qual seria sua classificação.

# Link do Vídeo: 
