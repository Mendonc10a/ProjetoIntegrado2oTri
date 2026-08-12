const express = require('express');
const cors = require('cors');
const axios = require('axios');
const path = require('path');

const app = express();
app.use(express.json());
app.use(cors());

// Aponta para a pasta frontend (1 nível acima do api-luminosidade)
const FRONTEND_PATH = path.join(__dirname, '../frontend');
app.use(express.static(FRONTEND_PATH));

// Entrega o index.html ao acessar http://localhost:3000/
app.get('/', (req, res) => {
    res.sendFile(path.join(FRONTEND_PATH, 'index.html'));
});

// URL da IA em Python/Flask
const IA_URL = 'http://127.0.0.1:5000/predict';

let leituraAtual = null;
let historico = [];

// 1. Endpoint para a aplicação C# enviar a leitura do STM32
app.post('/api/medicao', async (req, res) => {
    try {
        const { valor } = req.body;

        if (valor === undefined || valor === null) {
            return res.status(400).json({ erro: 'Campo "valor" é obrigatório.' });
        }

        const valorNumerico = Number(valor);

        // Envia o valor recebido para a IA classificar
        const respostaIA = await axios.post(IA_URL, { valor: valorNumerico });
        const classificacao = respostaIA.data.classificacao;

        // Atualiza a leitura atual
        leituraAtual = {
            valor: valorNumerico,
            classificacao,
            horario: new Date().toLocaleTimeString('pt-BR')
        };

        // Salva no histórico (mantém até 10 registros para a tabela web)
        historico.unshift(leituraAtual);
        if (historico.length > 10) {
            historico.pop();
        }

        console.log(`[Node.js] Recebido: ${valorNumerico} | Classe IA: ${classificacao}`);

        return res.json({ sucesso: true, dados: leituraAtual });
    } catch (erro) {
        console.error('[Node.js Error]', erro.message);
        return res.status(500).json({ erro: 'Erro na comunicação com a IA ou servidor.' });
    }
});

// 2. Endpoint para o Frontend consumir a leitura e o histórico
app.get('/api/dados', (req, res) => {
    res.json({
        atual: leituraAtual,
        historico: historico
    });
});

const PORT = 3000;
app.listen(PORT, () => {
    console.log(`[Node.js API] Servidor rodando em http://localhost:${PORT}`);
});