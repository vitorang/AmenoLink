/*
    Servidor de Action que será executado pelo AmenoLink.
    NÃO execute-o manualmente. Ele é usado pelo ActionExample.
*/

using System.Globalization;
using static AmenoLink.AmenoLinkClient;

namespace AmenoLinkExamples.ActionServer;

public static class Program
{
    public static void Main()
    {
        // Configuração inicial do programa. É recomendado definir appName,
        // porém, se o AmenoLink está rodando localmente, não é necessário definir originUrl.
        Setup(
            appName: "Action Server (C#)",
            originUrl: "http://localhost:13545"
        );

        // Para registrar uma ação, a rota é igual à registrada no AmenoLink.
        Actions.Add<User, UserAstrology>("example.action", Hello);

        // Aguarda por requisições.
        Actions.Serve();
    }

    // Assim é declarada uma ação.
    private static async Task<UserAstrology> Hello(User user)
    {
        // Caso use recursos, declare-os antes da validação
        await EnsureReady();

        var date = DateTime.ParseExact(user.BirthDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        string weekDay = GetWeekDay(date);
        string sign = GetZodiacSign(date);

        // Use ActionContext para obter contexto da ação atual.
        var context = ActionContext;
        context.Log($"Olá, {user.Name}!");
        context.Log($"Você é de {sign} e nasceu em {weekDay}!");

        return new UserAstrology(user.Name, user.BirthDate, weekDay, sign);
    }

    private static string GetWeekDay(DateTime date)
    {
        string[] days = ["Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado"];
        return days[(int)date.DayOfWeek];
    }

    private static string GetZodiacSign(DateTime date)
    {
        int day = date.Day;
        int month = date.Month;

        if ((month == 3 && day >= 21) || (month == 4 && day <= 19))
            return "Áries";
        if ((month == 4 && day >= 20) || (month == 5 && day <= 20))
            return "Touro";
        if ((month == 5 && day >= 21) || (month == 6 && day <= 20))
            return "Gêmeos";
        if ((month == 6 && day >= 21) || (month == 7 && day <= 22))
            return "Câncer";
        if ((month == 7 && day >= 23) || (month == 8 && day <= 22))
            return "Leão";
        if ((month == 8 && day >= 23) || (month == 9 && day <= 22))
            return "Virgem";
        if ((month == 9 && day >= 23) || (month == 10 && day <= 22))
            return "Libra";
        if ((month == 10 && day >= 23) || (month == 11 && day <= 21))
            return "Escorpião";
        if ((month == 11 && day >= 22) || (month == 12 && day <= 21))
            return "Sagitário";
        if ((month == 12 && day >= 22) || (month == 1 && day <= 19))
            return "Capricórnio";
        if ((month == 1 && day >= 20) || (month == 2 && day <= 18))
            return "Aquário";

        return "Peixes";
    }
}
