/*
    Servidor de Action que será executado pelo AmenoLink.
    NÃO execute-o manualmente. Ele é usado pelo action_example.ts.
*/

import { actions, actionContext, ensureReady, setup } from 'amenolink';
import { User, UserAstrology } from './dtos';

function parseDate(dateStr: string): Date {
    const parts = dateStr.split('/');
    if (parts.length === 3) {
        const day = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10) - 1;
        const year = parseInt(parts[2], 10);
        return new Date(year, month, day);
    }
    return new Date(dateStr);
}

function getWeekDay(dt: Date): string {
    const days = ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'];
    return days[dt.getDay()];
}

function getZodiacSign(dt: Date): string {
    const day = dt.getDate();
    const month = dt.getMonth() + 1;

    if ((month === 3 && day >= 21) || (month === 4 && day <= 19)) return 'Áries';
    if ((month === 4 && day >= 20) || (month === 5 && day <= 20)) return 'Touro';
    if ((month === 5 && day >= 21) || (month === 6 && day <= 20)) return 'Gêmeos';
    if ((month === 6 && day >= 21) || (month === 7 && day <= 22)) return 'Câncer';
    if ((month === 7 && day >= 23) || (month === 8 && day <= 22)) return 'Leão';
    if ((month === 8 && day >= 23) || (month === 9 && day <= 22)) return 'Virgem';
    if ((month === 9 && day >= 23) || (month === 10 && day <= 22)) return 'Libra';
    if ((month === 10 && day >= 23) || (month === 11 && day <= 21)) return 'Escorpião';
    if ((month === 11 && day >= 22) || (month === 12 && day <= 21)) return 'Sagitário';
    if ((month === 12 && day >= 22) || (month === 1 && day <= 19)) return 'Capricórnio';
    if ((month === 1 && day >= 20) || (month === 2 && day <= 18)) return 'Aquário';

    return 'Peixes';
}

// Assim é declarada uma ação.
async function hello(user: User): Promise<UserAstrology> {
    // Caso use recursos, declare-os antes da validação
    await ensureReady();

    const dt = parseDate(user.birthDate);
    const weekDay = getWeekDay(dt);
    const sign = getZodiacSign(dt);

    // Use actionContext() para obter contexto da ação atual.
    const context = actionContext();
    context.log(`Olá, ${user.name}!`);
    context.log(`Você é de ${sign} e nasceu em ${weekDay}!`);

    return {
        name: user.name,
        birthDate: user.birthDate,
        weekDay,
        sign
    };
}

// Configuração inicial do programa. É recomendado definir appName,
// porém, se o AmenoLink está rodando localmente, não é necessário definir originUrl.
setup({
    appName: 'Action Server (TypeScript)',
    originUrl: 'http://localhost:13545'
});

// Para registrar uma ação, a rota é igual à registrada no AmenoLink.
actions.add<User, UserAstrology>('example.action', hello);

// Aguarda por requisições.
actions.serve();
