import numpy as np
from flask import Flask, request, jsonify
from sklearn.tree import DecisionTreeClassifier

app = Flask(__name__)

# 1. Coleta de dados (Faixa de 0 a 4095 do ADC do STM32)
X = np.array([
    [100], [300], [500], [700], [900], [1100], [1200],       # Escuro
    [1300], [1600], [1900], [2200], [2500], [2800], [3000], # Adequado
    [3100], [3300], [3500], [3700], [3900], [4000], [4095]  # Excessivo
    ])

y = np.array([
    'Escuro', 'Escuro', 'Escuro', 'Escuro', 'Escuro', 'Escuro', 'Escuro',
    'Adequado', 'Adequado', 'Adequado', 'Adequado', 'Adequado', 'Adequado', 'Adequado',
    'Excessivo', 'Excessivo', 'Excessivo', 'Excessivo', 'Excessivo', 'Excessivo', 'Excessivo'
    ])

# 2. Treinamento do modelo
modelo = DecisionTreeClassifier(max_depth=3)
modelo.fit(X, y)

print("[Python IA] Modelo de Árvore de Decisão treinado com sucesso!")

# 3. Rota HTTP POST para fazer predições
@app.route('/predict', methods=['POST'])
def predict():
    dados = request.get_json()
    valor_adc = dados.get('valor', 0)
    
    # Realiza a predição com o valor recebido da API Node.js
    predicao = modelo.predict([[valor_adc]])[0]
    
    return jsonify({
        'valor': valor_adc,
        'classificacao': predicao
        })

if __name__ == '__main__':
    # Roda a API da IA na porta 5000
    app.run(host='0.0.0.0', port=5000)